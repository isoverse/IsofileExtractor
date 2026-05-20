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
    Console.Error.WriteLine("Usage: IsodatReader [--version] [--objects] [--tree] [--unabridged] <file.dxf|file.scn> [...]");
    return 1;
}

bool dumpObjects = args.Contains("--objects");
bool dumpTree = args.Contains("--tree");
Readers.Unabridged = args.Contains("--unabridged");
string[] files = args.Where(a => !a.StartsWith("--")).ToArray();

if (files.Length == 0)
{
    Console.Error.WriteLine("Usage: IsodatReader [--version] [--objects] [--tree] [--unabridged] <file.dxf|file.scn> [...]");
    return 1;
}

var options = new JsonSerializerOptions
{
    WriteIndented = true,
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
    var nObjectsByObjIdx = archive.ObjectLog.ToDictionary(e => e.ObjIdx, e => e.NObjects);
    writer.WriteLine("start,class_idx,obj_idx,container_idx,class_name,archive_version,has_n_block_objects,block_object_idx,block_object_total,value");
    foreach (var e in archive.ObjectLog)
    {
        int? blockObjectTotal = e.ContainerObjIdx is int cid
            && nObjectsByObjIdx.TryGetValue(cid, out var pn) ? pn : null;
        writer.WriteLine($"0x{e.Start:x},{e.ClassIdx},{e.ObjIdx},{e.ContainerObjIdx?.ToString() ?? ""},\"{e.ClassName}\",{e.ArchiveVersion},{e.NObjects?.ToString() ?? ""},{e.BlockObjectIdx?.ToString() ?? ""},{blockObjectTotal?.ToString() ?? ""},\"{e.Value ?? ""}\"");
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

    // Build ObjIdx → entry lookup so WriteTreeLevel can find a parent's declared NObjects.
    var byObjIdx = archive.ObjectLog.ToDictionary(e => e.ObjIdx);

    using var writer = new StreamWriter(treePath);
    WriteTreeLevel(writer, childrenOf, byObjIdx, parentObjIdx: Root, depth: 0);
    Console.Error.WriteLine($"Tree written: {treePath}");
}

static void WriteTreeLevel(
    StreamWriter writer,
    Dictionary<int, List<ObjectLogEntry>> childrenOf,
    Dictionary<int, ObjectLogEntry> byObjIdx,
    int parentObjIdx,
    int depth)
{
    if (!childrenOf.TryGetValue(parentObjIdx, out var siblings)) return;

    string indent = new string(' ', depth * 2);

    // Use n_objects declared in the parent's binary header as the denominator for k/N: prefixes.
    // Falls back to the actual parsed sibling count when n_objects is not available.
    int? parentNObjects = byObjIdx.TryGetValue(parentObjIdx, out var parentEntry) ? parentEntry.NObjects : null;

    // Compute the declared run total for each block-object position.
    int[] runTotal = new int[siblings.Count];
    for (int j = 0; j < siblings.Count; j++)
    {
        if (!siblings[j].IsBlockObject) continue;
        if (j > 0 && siblings[j - 1].IsBlockObject) continue; // already counted
        int n = 0;
        while (j + n < siblings.Count && siblings[j + n].IsBlockObject) n++;
        int declared = parentNObjects ?? n;
        for (int k = j; k < j + n; k++) runTotal[k] = declared;
    }

    for (int i = 0; i < siblings.Count;)
    {
        var first = siblings[i];
        string? effVal = string.IsNullOrEmpty(first.Value) ? null : first.Value;

        // Collapse consecutive siblings with same class/version/blockness and same effective value.
        // Siblings with distinct non-empty values are kept on separate lines.
        int count = 1;
        while (i + count < siblings.Count)
        {
            var next = siblings[i + count];
            if (next.ClassName != first.ClassName
                || next.ArchiveVersion != first.ArchiveVersion
                || next.IsBlockObject != first.IsBlockObject
                || (string.IsNullOrEmpty(next.Value) ? null : next.Value) != effVal)
                break;
            count++;
        }

        string value = effVal is not null ? $" \"{effVal}\"" : "";
        string label = $"{first.ClassName} v{first.ArchiveVersion} 0x{first.Start:x}{value}";
        string linePrefix = first.IsBlockObject
            ? (count > 1
                ? $"{first.BlockObjectIdx}-{(first.BlockObjectIdx ?? 0) + count - 1}/{runTotal[i]}: "
                : $"{first.BlockObjectIdx}/{runTotal[i]}: ")
            : "";
        writer.WriteLine($"{indent}{linePrefix}{label}");
        if (childrenOf.ContainsKey(first.ObjIdx))
            WriteTreeLevel(writer, childrenOf, byObjIdx, first.ObjIdx, depth + 1);
        i += count;
    }
}
