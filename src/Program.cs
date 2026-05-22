using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using IsodatReader;

if (args.Length == 1 && args[0] is "--version" or "-v")
{
    Console.WriteLine(System.Reflection.Assembly.GetExecutingAssembly()
        .GetName().Version?.ToString() ?? "unknown");
    return 0;
}

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: IsodatReader [--version] [--objects] [--tree] [--unabridged] [--prettyJSON] <file.dxf|file.scn> [...]");
    return 1;
}

bool dumpObjects = args.Contains("--objects");
bool dumpTree = args.Contains("--tree");
bool prettyJson = args.Contains("--prettyJSON");
Readers.Unabridged = args.Contains("--unabridged");
string[] files = args.Where(a => !a.StartsWith("--")).ToArray();

if (files.Length == 0)
{
    Console.Error.WriteLine("Usage: IsodatReader [--version] [--objects] [--tree] [--unabridged] [--prettyJSON] <file.dxf|file.scn> [...]");
    return 1;
}

var options = new JsonSerializerOptions
{
    WriteIndented = prettyJson,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
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

    string outputPath = inputPath + ".json";

    using var stream = File.OpenRead(inputPath);
    using var archive = new IsodatFile(stream);

    string ext = Path.GetExtension(inputPath).ToLowerInvariant();

    var meta = new JsonObject
    {
        ["reader_version"] = assemblyVersion,
        ["file_type"] = ext.TrimStart('.'),
        ["file_size_bytes"] = new FileInfo(inputPath).Length,
    };
    var root = new JsonObject();
    root["meta"] = meta;

    Exception? caughtEx = null;

    void ReadInto(string key, string className)
    {
        if (caughtEx is not null) return;
        try { root[key] = Readers.ReadObject(archive, className); }
        catch (IsodatParseException ipe)
        {
            if (ipe.PartialResult is not null) root[key] = ipe.PartialResult;
            caughtEx = ipe;
        }
        catch (Exception ex) { caughtEx = ex; }
    }

    try
    {
        switch (ext)
        {
            case ".dxf":
                ReadInto("CFileHeader", "CFileHeader");
                ReadInto("CContiniousFlowBlockData", "CContiniousFlowBlockData");
                break;
            case ".scn":
                ReadInto("CScanStorage", "CScanStorage");
                break;
            default:
                root["error"] = $"Unsupported file extension '{ext}'";
                break;
        }
    }
    finally
    {
        meta["complete"] = caughtEx is null;
        string json = root.ToJsonString(options);
        if (prettyJson) json = CollapseNumberArrays(json);
        File.WriteAllText(outputPath, json);
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

// Replaces multi-line pretty-printed number arrays with a single compact line.
static string CollapseNumberArrays(string json) =>
    Regex.Replace(json,
        @"\[(?:\s*-?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?\s*,)*\s*-?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?\s*\]",
        static m => "[" + string.Join(", ",
            Regex.Matches(m.Value, @"-?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?")
                 .Select(n => n.Value)) + "]",
        RegexOptions.Singleline);

static void DumpObjects(IsodatFile archive, string inputPath)
{
    string csvPath = Path.ChangeExtension(inputPath, ".objects.csv");
    using var writer = new StreamWriter(csvPath);
    writer.WriteLine("start,class_idx,obj_idx,container_idx,class_name,archive_version,n_block_objects,object_list_idx,object_list_total,value,plot_info_class_idx,plot_info_obj_idx");
    foreach (var e in archive.ObjectLog)
    {
        writer.WriteLine($"0x{e.Start:x},{e.ClassIdx},{e.ObjIdx},{e.ContainerObjIdx?.ToString() ?? ""},\"{e.ClassName}\",{e.ArchiveVersion},{e.NObjects?.ToString() ?? ""},{e.BlockObjectIdx?.ToString() ?? ""},{e.GroupTotal?.ToString() ?? ""},\"{e.Value ?? ""}\",{e.SecondaryClassIdx?.ToString() ?? ""},{e.SecondaryObjIdx?.ToString() ?? ""}");
    }
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

    for (int i = 0; i < siblings.Count;)
    {
        var first = siblings[i];
        string? effVal = string.IsNullOrEmpty(first.Value) ? null : first.Value;

        // Collapse consecutive siblings with same class/version/blockness and same effective value.
        // Never collapse items that have children — each must appear separately so its subtree is printed.
        int count = 1;
        if (!childrenOf.ContainsKey(first.ObjIdx))
        {
            while (i + count < siblings.Count)
            {
                var next = siblings[i + count];
                if (next.ClassName != first.ClassName
                    || next.ArchiveVersion != first.ArchiveVersion
                    || next.IsBlockObject != first.IsBlockObject
                    || (string.IsNullOrEmpty(next.Value) ? null : next.Value) != effVal
                    || childrenOf.ContainsKey(next.ObjIdx))
                    break;
                count++;
            }
        }

        string value = effVal is not null ? $" \"{effVal}\"" : "";
        string label = $"{first.ClassName} v{first.ArchiveVersion} 0x{first.Start:x}{value}";
        string linePrefix = first.IsBlockObject
            ? (count > 1
                ? $"{first.BlockObjectIdx}-{(first.BlockObjectIdx ?? 0) + count - 1}/{first.GroupTotal}: "
                : $"{first.BlockObjectIdx}/{first.GroupTotal}: ")
            : "";
        writer.WriteLine($"{indent}{linePrefix}{label}");
        if (childrenOf.ContainsKey(first.ObjIdx))
            WriteTreeLevel(writer, childrenOf, first.ObjIdx, depth + 1);
        i += count;
    }
}
