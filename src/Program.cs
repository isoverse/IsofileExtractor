using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using IsofileExtractor;

if (args.Length == 1 && args[0] is "--version" or "-v")
{
    Console.WriteLine("isoextract version " + (System.Reflection.Assembly.GetExecutingAssembly()
        .GetName().Version?.ToString() ?? "unknown"));
    return 0;
}

var usage = "Usage: isoextract [--version] [--objects] [--tree] [--unabridged] [--prettyJSON] [--dry-run] [--log [<path>]] [--file-list <path>] <file|dir> [...]";

if (args.Length == 0)
{
    Console.Error.WriteLine(usage);
    return 1;
}

bool dumpObjects = args.Contains("--objects");
bool dumpTree = args.Contains("--tree");
bool prettyJson = args.Contains("--prettyJSON");
bool dryRun = args.Contains("--dry-run");
Readers.Unabridged = args.Contains("--unabridged");

bool writeLog = false;
string? logPathArg = null;
string? fileListArg = null;
var pathList = new List<string>();
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--log")
    {
        writeLog = true;
        if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            logPathArg = args[++i];
    }
    else if (args[i] == "--file-list")
    {
        if (i + 1 >= args.Length)
        {
            Console.Error.WriteLine("--file-list requires a path argument");
            return 1;
        }
        fileListArg = args[++i];
    }
    else if (!args[i].StartsWith("--"))
        pathList.Add(args[i]);
}

if (fileListArg is not null)
{
    string listPath = Path.GetFullPath(fileListArg);
    if (!File.Exists(listPath))
    {
        Console.Error.WriteLine($"File list not found: {listPath}");
        return 1;
    }
    pathList.AddRange(File.ReadAllLines(listPath)
        .Select(l => l.Trim())
        .Where(l => l.Length > 0 && !l.StartsWith('#')));
}

string[] paths = pathList.ToArray();

if (paths.Length == 0)
{
    Console.Error.WriteLine(usage);
    return 1;
}

HashSet<string> isodatExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".dxf", ".cf", ".did", ".caf", ".scn" };

int exitCode = 0;
string cwd = Directory.GetCurrentDirectory();

int folderCount = paths.Count(p => Directory.Exists(Path.GetFullPath(p)));
if (folderCount > 0)
    Console.WriteLine($"Searching {folderCount} folder{(folderCount == 1 ? "" : "s")} recursively...");

(string Full, string Display)[] files = paths
    .SelectMany(p =>
    {
        string full = Path.GetFullPath(p);
        bool wasAbsolute = Path.IsPathRooted(p);
        string Display(string f) => wasAbsolute ? f : Path.GetRelativePath(cwd, f);
        if (Directory.Exists(full))
            return Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories)
                .Where(f => isodatExtensions.Contains(Path.GetExtension(f)))
                .Select(f => (f, Display(f)));
        if (!File.Exists(full))
        {
            Console.Error.WriteLine($"Path not found: {Display(full)}");
            Interlocked.Exchange(ref exitCode, 1);
            return [];
        }
        if (!isodatExtensions.Contains(Path.GetExtension(full), StringComparer.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Skipping unsupported file extension: {Path.GetFileName(p)} ");
            Interlocked.Exchange(ref exitCode, 1);
            return [];
        }
        return [(full, Display(full))];
    })
    .ToArray();

var options = new JsonSerializerOptions
{
    WriteIndented = prettyJson,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
};

string assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly()
    .GetName().Version?.ToString() ?? "unknown";

string? logPath = writeLog
    ? (logPathArg is not null
        ? Path.GetFullPath(logPathArg)
        : Path.Combine(Directory.GetCurrentDirectory(), "isoextract.log"))
    : null;
string? logDisplayPath = writeLog ? (logPathArg ?? "isoextract.log") : null;

StreamWriter? logWriter = null;
object logLock = new();
if (logPath is not null)
{
    logWriter = new StreamWriter(logPath, append: false) { AutoFlush = true };
    logWriter.WriteLine("file,success,duration_ms,error");
}

Parallel.ForEach(files, inputArg =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var (inputPath, displayPath) = inputArg;
    if (!File.Exists(inputPath))
    {
        Console.Error.WriteLine($"File not found: {inputPath}");
        Interlocked.Exchange(ref exitCode, 1);
        return;
    }

    string outputPath = inputPath + ".json";

    using var stream = File.OpenRead(inputPath);
    {
        Span<byte> magic = stackalloc byte[2];
        if (stream.Read(magic) < 2 || magic[0] != 0xFF || magic[1] != 0xFF)
        {
            Console.Error.WriteLine($"Not an isodat file: {Path.GetFileName(inputPath)}");
            Interlocked.Exchange(ref exitCode, 1);
            return;
        }
        stream.Seek(0, SeekOrigin.Begin);
    }
    using var archive = new IsodatFile(stream);

    string ext = Path.GetExtension(inputPath).ToLowerInvariant();

    var meta = new JsonObject
    {
        ["isoextract_version"] = assemblyVersion,
        ["file_type"] = ext.TrimStart('.'),
        ["file_size_bytes"] = new FileInfo(inputPath).Length,
    };
    var root = new JsonObject();
    root["meta"] = meta;

    Exception? caughtEx = null;

    void ReadObjInto(string? expected = null, int? idx = null, int? groupTotal = null, string? expectedValue = null)
    {
        if (caughtEx is not null) return;
        try { Readers.ReadObjectInto(root, archive, expected, idx: idx, groupTotal: groupTotal, expectedValue: expectedValue); }
        catch (IsodatParseException ipe) { caughtEx = ipe; }
        catch (Exception ex) { caughtEx = ex; }
    }

    try
    {
        switch (ext)
        {
            case ".dxf":
                ReadObjInto("CFileHeader");
                ReadObjInto("CContiniousFlowBlockData");
                break;
            case ".cf":
                ReadObjInto("CFileHeader");
                ReadObjInto("CMethod");
                ReadObjInto("CPlotSettings");
                ReadObjInto("CBlockData", idx: 1, groupTotal: 4, expectedValue: "Data Block");
                ReadObjInto("CBlockData", idx: 2, groupTotal: 4, expectedValue: "Sequence Data");
                ReadObjInto("CBlockData", idx: 3, groupTotal: 4, expectedValue: "Primary Std. Data Block");
                ReadObjInto("CBlockData", idx: 4, groupTotal: 4, expectedValue: "H3 Factor");
                break;
            case ".did":
                ReadObjInto("CFileHeader");
                ReadObjInto("CDualInletBlockData");
                break;
            case ".scn":
                ReadObjInto("CScanStorage");
                break;
            case ".caf":
                ReadObjInto("CFileHeader");
                ReadObjInto("CLong");
                ReadObjInto("CBlockDataContext");
                break;
            default:
                root["error"] = $"Unsupported file extension '{ext}'";
                break;
        }
        if (caughtEx is null && archive.Position < archive.Length)
            caughtEx = new InvalidDataException(
                $"Read finished at 0x{archive.Position:x} but file ends at 0x{archive.Length:x} " +
                $"({archive.Length - archive.Position} unread bytes)");
    }
    finally
    {
        meta["complete"] = caughtEx is null;
        if (!dryRun)
        {
            string json = root.ToJsonString(options);
            if (prettyJson) json = CollapseNumberArrays(json);
            File.WriteAllText(outputPath, json);
            Console.WriteLine($"Written: {displayPath}.json{(caughtEx is not null ? " (incomplete)" : "")}");
        }
        else
        {
            Console.WriteLine($"Parsed (dry run): {displayPath}{(caughtEx is not null ? " (incomplete)" : "")}");
        }

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
        if (!dryRun) WriteIssuesLog(archive, inputPath, caughtEx);
        if (dumpObjects)
            DumpObjects(archive, inputPath, displayPath);
        if (dumpTree)
            DumpTree(archive, inputPath, displayPath);
        if (logWriter is not null)
        {
            bool success = caughtEx is null;
            string error = caughtEx?.Message ?? "";
            string line = $"{CsvField(displayPath)},{success.ToString().ToLowerInvariant()},{sw.ElapsedMilliseconds},\"{error.Replace("\"", "\"\"")}\"";
            lock (logLock) logWriter.WriteLine(line);
        }
    }
});

logWriter?.Dispose();
if (logDisplayPath is not null) Console.WriteLine($"Log: {logDisplayPath}");

return exitCode;

static string CsvField(string value) =>
    value.Contains(',') || value.Contains('"') || value.Contains('\n')
        ? $"\"{value.Replace("\"", "\"\"")}\""
        : value;

// Replaces multi-line pretty-printed number arrays with a single compact line.
static string CollapseNumberArrays(string json) =>
    Regex.Replace(json,
        @"\[(?:\s*-?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?\s*,)*\s*-?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?\s*\]",
        static m => "[" + string.Join(", ",
            Regex.Matches(m.Value, @"-?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?")
                 .Select(n => n.Value)) + "]",
        RegexOptions.Singleline);

static void DumpObjects(IsodatFile archive, string inputPath, string displayPath)
{
    string csvPath = inputPath + ".objects.csv";
    using var writer = new StreamWriter(csvPath);
    writer.WriteLine("start,class_idx,obj_idx,container_idx,class_name,archive_version,n_block_objects,object_list_idx,object_list_total,value");
    foreach (var e in archive.ObjectLog)
    {
        writer.WriteLine($"0x{e.Start:x},{e.ClassIdx},{e.ObjIdx},{e.ContainerObjIdx?.ToString() ?? ""},\"{e.ClassName}\",{e.ArchiveVersion},{e.NObjects?.ToString() ?? ""},{e.BlockObjectIdx?.ToString() ?? ""},{e.GroupTotal?.ToString() ?? ""},\"{e.Value ?? ""}\"");
    }
    Console.WriteLine($"Objects written: {displayPath}.objects.csv ({archive.ObjectLog.Count} entries)");
}

static void WriteIssuesLog(IsodatFile archive, string inputPath, Exception? error)
{
    string logPath = inputPath + ".issues.log";
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

static void DumpTree(IsodatFile archive, string inputPath, string displayPath)
{
    string treePath = inputPath + ".tree.txt";

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
    Console.WriteLine($"Tree written: {displayPath}.tree.txt");
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
