using System.Text.Json;
using System.Text.Json.Nodes;
using IsodatReader;

if (args.Length == 1 && args[0] is "--version" or "-v")
{
    Console.WriteLine(System.Reflection.Assembly.GetExecutingAssembly()
        .GetName().Version?.ToString() ?? "unknown");
    return 0;
}

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: IsodatReader [--version] [--objects] <file.dxf|file.scn> [...]");
    return 1;
}

bool dumpObjects = args.Contains("--objects");
bool dumpTree    = args.Contains("--tree");
string[] files   = args.Where(a => !a.StartsWith("--")).ToArray();

if (files.Length == 0)
{
    Console.Error.WriteLine("Usage: IsodatReader [--version] [--objects] [--tree] <file.dxf|file.scn> [...]");
    return 1;
}

var options = new JsonSerializerOptions
{
    WriteIndented          = true,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy   = JsonNamingPolicy.SnakeCaseLower,
    TypeInfoResolver       = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
};

string assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly()
    .GetName().Version?.ToString() ?? "unknown";

int exitCode = 0;

Parallel.ForEach(files, inputArg =>
{
    string inputPath = Path.GetFullPath(inputArg);
    if (!File.Exists(inputPath))
    {
        Console.Error.WriteLine($"File not found: {inputPath}");
        Interlocked.Exchange(ref exitCode, 1);
        return;
    }

    string outputPath = Path.ChangeExtension(inputPath, ".json");

    using var stream  = File.OpenRead(inputPath);
    using var archive = new IsodatFile(stream);

    string ext = Path.GetExtension(inputPath).ToLowerInvariant();
    string? dataClass = ext switch
    {
        ".dxf" => "CContiniousFlowBlockData",
        ".scn" => "CScanStorage",
        _      => null,
    };

    var meta = new JsonObject
    {
        ["reader_version"] = assemblyVersion,
        ["file_type"]      = ext.TrimStart('.'),
        ["file_size_bytes"] = new FileInfo(inputPath).Length,
    };
    var root = new JsonObject();
    root["meta"] = meta;

    Exception? caughtEx = null;
    try
    {
        // DXF files have a CFileHeader; SCN files start directly with the data class
        if (ext == ".dxf")
            root["file_header"] = Readers.Dispatch(archive, "CFileHeader");

        try
        {
            if (dataClass is not null)
                root["data"] = Readers.Dispatch(archive, dataClass);
            else
                root["data"] = new JsonObject { ["error"] = $"Unsupported file extension '{ext}'" };
        }
        catch (EndOfStreamException)
        {
            // Some files contain only a file header
        }
    }
    catch (Exception ex) { caughtEx = ex; }
    finally
    {
        meta["complete"] = caughtEx is null;
        if (caughtEx is not null && caughtEx is IsodatParseException ipe && ipe.PartialResult is not null)
            root["data"] = ipe.PartialResult;
        File.WriteAllText(outputPath, root.ToJsonString(options));
        Console.WriteLine($"Written: {outputPath}{(caughtEx is not null ? " (incomplete)" : "")}");

        if (archive.Warnings.Count > 0)
        {
            Console.Error.WriteLine($"\n{archive.Warnings.Count} warning(s) in {Path.GetFileName(inputPath)}:");
            foreach (string w in archive.Warnings)
                Console.Error.WriteLine($"  {w}");
        }
        if (caughtEx is not null)
        {
            Console.Error.WriteLine($"Error processing {Path.GetFileName(inputPath)}: {caughtEx.Message}");
            Interlocked.Exchange(ref exitCode, 1);
        }
        WriteIssuesLog(archive, inputPath, caughtEx);
        if (dumpObjects)
            DumpObjects(archive, inputPath);
        if (dumpTree)
            DumpTree(archive, inputPath);
    }
});

return exitCode;

static void DumpObjects(IsodatFile archive, string inputPath)
{
    string csvPath = Path.ChangeExtension(inputPath, ".objects.csv");
    using var writer = new StreamWriter(csvPath);
    writer.WriteLine("start,class_idx,obj_idx,container_idx,class_name,archive_version");
    foreach (var e in archive.ObjectLog)
        writer.WriteLine($"0x{e.Start:x},{e.ClassIdx},{e.ObjIdx},{e.ContainerObjIdx?.ToString() ?? ""},\"{e.ClassName}\",{e.ArchiveVersion}");
    Console.Error.WriteLine($"Objects written: {csvPath} ({archive.ObjectLog.Count} entries)");
}

static void WriteIssuesLog(IsodatFile archive, string inputPath, Exception? error)
{
    string logPath = Path.ChangeExtension(inputPath, ".issues.log");
    if (archive.Warnings.Count == 0 && error is null)
    {
        File.Delete(logPath);
        return;
    }
    using var writer = new StreamWriter(logPath);
    foreach (string w in archive.Warnings)
        writer.WriteLine($"warning: {w}");
    if (error is not null)
        writer.WriteLine($"error: {error.Message}");
}

static void DumpTree(IsodatFile archive, string inputPath)
{
    string treePath = Path.ChangeExtension(inputPath, ".tree.txt");

    // Group entries by parent obj-index (-1 = root sentinel)
    const int Root = -1;
    var childrenOf = new Dictionary<int, List<ObjectLogEntry>>();
    foreach (var e in archive.ObjectLog)
    {
        int key = e.ContainerObjIdx ?? Root;
        if (!childrenOf.TryGetValue(key, out var list))
            childrenOf[key] = list = new List<ObjectLogEntry>();
        list.Add(e);
    }

    using var writer = new StreamWriter(treePath);
    WriteTreeLevel(writer, childrenOf, parentObjIdx: Root, depth: 0);
    Console.Error.WriteLine($"Tree written: {treePath}");
}

static void WriteTreeLevel(
    StreamWriter writer,
    Dictionary<int, List<ObjectLogEntry>> childrenOf,
    int parentObjIdx,
    int depth)
{
    if (!childrenOf.TryGetValue(parentObjIdx, out var siblings)) return;

    string indent = new string(' ', depth * 2);
    int i = 0;
    while (i < siblings.Count)
    {
        var first = siblings[i];

        // Count consecutive siblings with the same class name and archive version
        int count = 1;
        while (i + count < siblings.Count
               && siblings[i + count].ClassName       == first.ClassName
               && siblings[i + count].ArchiveVersion  == first.ArchiveVersion)
            count++;

        string prefix = count > 1 ? $"{count}x " : "";
        string label  = $"{prefix}{first.ClassName} v{first.ArchiveVersion} @0x{first.Start:x}";

        writer.WriteLine($"{indent}{label}");

        // Recurse into the first entry's children (representative for collapsed groups)
        if (childrenOf.ContainsKey(first.ObjIdx))
            WriteTreeLevel(writer, childrenOf, first.ObjIdx, depth + 1);

        i += count;
    }
}
