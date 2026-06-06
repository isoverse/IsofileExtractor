using System.Runtime.InteropServices;
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
    { ".dxf", ".cf", ".did", ".caf", ".scn", ".iarc", ".larc" };

static bool IsBchDir(string path) =>
    Directory.Exists(path) &&
    string.Equals(Path.GetExtension(path), ".bch", StringComparison.OrdinalIgnoreCase);

static bool IsImexpZip(string path) =>
    File.Exists(path) &&
    path.EndsWith(".imexp.zip", StringComparison.OrdinalIgnoreCase);

static bool IsImexpFile(string path) =>
    File.Exists(path) &&
    Path.GetExtension(path).Equals(".imexp", StringComparison.OrdinalIgnoreCase);

int exitCode = 0;
string cwd = Directory.GetCurrentDirectory();

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

int folderCount = paths.Count(p => Directory.Exists(Path.GetFullPath(p)));
if (folderCount > 0)
    Console.WriteLine($"Searching {folderCount} folder{(folderCount == 1 ? "" : "s")} recursively...");

(string Full, string Display)[] files = paths
    .SelectMany(p =>
    {
        string full = Path.GetFullPath(p);
        bool wasAbsolute = Path.IsPathRooted(p);
        string Display(string f) => wasAbsolute ? f : Path.GetRelativePath(cwd, f);
        if (IsBchDir(full))
            return [(full, Display(full))];
        if (Directory.Exists(full))
            return Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories)
                .Where(f => isodatExtensions.Contains(Path.GetExtension(f)))
                .Select(f => (f, Display(f)))
                .Concat(Directory.EnumerateDirectories(full, "*", SearchOption.AllDirectories)
                    .Where(d => Path.GetExtension(d).Equals(".bch", StringComparison.OrdinalIgnoreCase))
                    .Select(d => (d, Display(d))))
                .Concat(Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".imexp.zip", StringComparison.OrdinalIgnoreCase))
                    .Select(f => (f, Display(f))))
                .Concat(Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories)
                    .Where(f => Path.GetExtension(f).Equals(".imexp", StringComparison.OrdinalIgnoreCase)
                             && !File.Exists(f + ".zip"))
                    .Select(f => (f, Display(f))));
        if (!File.Exists(full))
        {
            // .imexp missing but its .zip sidecar exists → process the zip transparently
            if (Path.GetExtension(full).Equals(".imexp", StringComparison.OrdinalIgnoreCase)
                && File.Exists(full + ".zip"))
                return [(full + ".zip", Display(full))];

            Console.Error.WriteLine($"Path does not exist: {Display(full)}");
            Interlocked.Exchange(ref exitCode, 1);
            if (!dryRun)
                try
                {
                    string issueDir = Path.GetDirectoryName(full + ".issues.log") ?? ".";
                    Directory.CreateDirectory(issueDir);
                    File.WriteAllText(full + ".issues.log", "error: path does not exist\n");
                    File.Delete(full + ".json");
                } catch { }
            if (logWriter is not null)
            {
                string line = $"{CsvField(Display(full))},false,0,\"path does not exist\"";
                lock (logLock) logWriter.WriteLine(line);
            }
            return [];
        }
        if (full.EndsWith(".imexp.zip", StringComparison.OrdinalIgnoreCase))
            return [(full, Display(full))];
        if (Path.GetExtension(full).Equals(".imexp", StringComparison.OrdinalIgnoreCase))
            return [(full, Display(full))];
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

// Lazily resolve isosolfs.exe: prefer a file next to the executable, fall back to the
// embedded resource (bundled in win-x64 publish builds).
var isosolfsLazy = new Lazy<string?>(() =>
{
    string exeDir = Path.GetDirectoryName(
        System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
        ?? AppContext.BaseDirectory) ?? AppContext.BaseDirectory;
    string sideBySide = Path.Combine(exeDir, "isosolfs.exe");
    if (File.Exists(sideBySide)) return sideBySide;

    using var stream = System.Reflection.Assembly.GetExecutingAssembly()
        .GetManifestResourceStream("isosolfs.exe");
    if (stream is null) return null;

    string tempPath = Path.Combine(Path.GetTempPath(), $"isoextract_isosolfs_{Environment.ProcessId}.exe");
    using var outFile = File.Create(tempPath);
    stream.CopyTo(outFile);
    return tempPath;
}, LazyThreadSafetyMode.ExecutionAndPublication);

AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    if (isosolfsLazy.IsValueCreated && isosolfsLazy.Value is string p
            && Path.GetFileName(p).StartsWith("isoextract_isosolfs_"))
        try { File.Delete(p); } catch { }
};

Parallel.ForEach(files, inputArg =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var (inputPath, displayPath) = inputArg;

    if (IsBchDir(inputPath))
    {
        string outputPath = inputPath + ".json";
        long bchSize = Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories)
            .Sum(f => new FileInfo(f).Length);
        var bchMeta = new JsonObject
        {
            ["isoextract_version"] = assemblyVersion,
            ["file_type"] = "bch",
            ["file_size_bytes"] = bchSize,
        };
        var bchRoot = new JsonObject();
        bchRoot["meta"] = bchMeta;
        Exception? bchEx = null;
        try
        {
            BchReader.Read(inputPath, bchRoot);
        }
        catch (Exception ex) { bchEx = ex; }
        finally
        {
            bchMeta["complete"] = bchEx is null;
            if (!dryRun)
            {
                string json = bchRoot.ToJsonString(options);
                if (prettyJson) json = CollapseNumberArrays(json);
                File.WriteAllText(outputPath, json);
                Console.WriteLine($"Written: {displayPath}.json{(bchEx is not null ? " (incomplete)" : "")}");
            }
            else
            {
                Console.WriteLine($"Parsed (dry run): {displayPath}{(bchEx is not null ? " (incomplete)" : "")}");
            }
            if (bchEx is not null)
            {
                Console.Error.WriteLine($"Error processing {Path.GetFileName(inputPath)}: {bchEx.Message}");
                Interlocked.Exchange(ref exitCode, 1);
            }
            if (!dryRun)
            {
                string issuesLogPath = inputPath + ".issues.log";
                if (bchEx is not null)
                    File.WriteAllText(issuesLogPath, $"error: {bchEx.Message}\n");
                else
                    File.Delete(issuesLogPath);
            }
            if (logWriter is not null)
            {
                bool success = bchEx is null;
                string error = bchEx?.Message ?? "";
                string line = $"{CsvField(displayPath)},{success.ToString().ToLowerInvariant()},{sw.ElapsedMilliseconds},\"{error.Replace("\"", "\"\"")}\"";
                lock (logLock) logWriter.WriteLine(line);
            }
        }
        return;
    }

    if (IsImexpFile(inputPath))
    {
        string zipPath = inputPath + ".zip";
        if (!File.Exists(zipPath))
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string osName = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS"
                              : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux"
                              : RuntimeInformation.OSDescription;
                string msg = $"Cannot extract .imexp notebooks on {osName} (yet), please run this on Windows and port the resulting .json files to your operating system";
                Console.Error.WriteLine(msg);
                Interlocked.Exchange(ref exitCode, 1);
                if (!dryRun)
                {
                    File.WriteAllText(inputPath + ".issues.log", $"error: {msg}\n");
                    File.Delete(inputPath + ".json");
                }
                if (logWriter is not null)
                {
                    string line = $"{CsvField(displayPath)},false,{sw.ElapsedMilliseconds},\"{msg.Replace("\"", "\"\"")}\"";
                    lock (logLock) logWriter.WriteLine(line);
                }
                return;
            }

            string? isosolfsPath = isosolfsLazy.Value;
            if (isosolfsPath is null)
            {
                string msg = "isosolfs.exe not bundled in this build and not found next to isoextract.exe";
                Console.Error.WriteLine(msg);
                Interlocked.Exchange(ref exitCode, 1);
                if (logWriter is not null)
                {
                    string line = $"{CsvField(displayPath)},false,{sw.ElapsedMilliseconds},\"{msg}\"";
                    lock (logLock) logWriter.WriteLine(line);
                }
                if (!dryRun) File.Delete(inputPath + ".json");
                return;
            }

            string extractedFolder = Path.Combine(
                Path.GetDirectoryName(inputPath) ?? "",
                Path.GetFileNameWithoutExtension(inputPath));

            bool conversionOk = false;
            try
            {
                Console.WriteLine($"Unpacking SolFS: {displayPath}...");
                var psi = new System.Diagnostics.ProcessStartInfo(isosolfsPath)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                psi.ArgumentList.Add(inputPath);
                psi.ArgumentList.Add("--extract");
                using var proc = System.Diagnostics.Process.Start(psi)!;
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                    throw new Exception($"isosolfs.exe exited with code {proc.ExitCode}: {proc.StandardError.ReadToEnd().Trim()}");
                System.IO.Compression.ZipFile.CreateFromDirectory(extractedFolder, zipPath);
                conversionOk = true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error converting {Path.GetFileName(inputPath)} to .zip: {ex.Message}");
                Interlocked.Exchange(ref exitCode, 1);
                if (logWriter is not null)
                {
                    string err = ex.Message.Replace("\"", "\"\"");
                    string line = $"{CsvField(displayPath)},false,{sw.ElapsedMilliseconds},\"{err}\"";
                    lock (logLock) logWriter.WriteLine(line);
                }
            }
            finally
            {
                if (Directory.Exists(extractedFolder))
                    Directory.Delete(extractedFolder, true);
            }
            if (!conversionOk)
            {
                if (!dryRun) File.Delete(inputPath + ".json");
                return;
            }
        }
        inputPath = zipPath;
    }

    if (IsImexpZip(inputPath))
    {
        string imexpBase = inputPath[..^".zip".Length];           // foo.imexp
        string displayBase = displayPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
            ? displayPath[..^".zip".Length] : displayPath;        // handles both foo.imexp and foo.imexp.zip inputs
        string outputPath = imexpBase + ".json";
        var imexpMeta = new JsonObject
        {
            ["isoextract_version"] = assemblyVersion,
            ["file_type"] = "imexp",
            ["file_size_bytes"] = File.Exists(imexpBase) ? new FileInfo(imexpBase).Length : new FileInfo(inputPath).Length,
        };
        var imexpRoot = new JsonObject();
        imexpRoot["meta"] = imexpMeta;
        Exception? imexpEx = null;
        try
        {
            ImexpReader.Read(inputPath, imexpRoot);
        }
        catch (Exception ex) { imexpEx = ex; }
        finally
        {
            imexpMeta["complete"] = imexpEx is null;
            if (!dryRun)
            {
                string json = imexpRoot.ToJsonString(options);
                if (prettyJson) json = CollapseNumberArrays(json);
                File.WriteAllText(outputPath, json);
                Console.WriteLine($"Written: {displayBase}.json{(imexpEx is not null ? " (incomplete)" : "")}");
            }
            else
            {
                Console.WriteLine($"Parsed (dry run): {displayBase}{(imexpEx is not null ? " (incomplete)" : "")}");
            }
            if (imexpEx is not null)
            {
                Console.Error.WriteLine($"Error processing {Path.GetFileName(inputPath)}: {imexpEx.Message}");
                Interlocked.Exchange(ref exitCode, 1);
            }
            if (!dryRun)
            {
                string issuesLogPath = imexpBase + ".issues.log";
                if (imexpEx is not null)
                    File.WriteAllText(issuesLogPath, $"error: {imexpEx.Message}\n");
                else
                    File.Delete(issuesLogPath);
            }
            if (logWriter is not null)
            {
                bool success = imexpEx is null;
                string error = imexpEx?.Message ?? "";
                string line = $"{CsvField(displayPath)},{success.ToString().ToLowerInvariant()},{sw.ElapsedMilliseconds},\"{error.Replace("\"", "\"\"")}\"";
                lock (logLock) logWriter.WriteLine(line);
            }
        }
        return;
    }

    if (!File.Exists(inputPath))
    {
        Console.Error.WriteLine($"Path does not exist: {inputPath}");
        Interlocked.Exchange(ref exitCode, 1);
        if (!dryRun)
        {
            File.WriteAllText(inputPath + ".issues.log", "error: path does not exist\n");
            File.Delete(inputPath + ".json");
        }
        if (logWriter is not null)
        {
            string line = $"{CsvField(displayPath)},false,{sw.ElapsedMilliseconds},\"path does not exist\"";
            lock (logLock) logWriter.WriteLine(line);
        }
        return;
    }

    string outputPath2 = inputPath + ".json";
    string ext = Path.GetExtension(inputPath).ToLowerInvariant();

    if (ext == ".iarc" || ext == ".larc")
    {
        var liarcMeta = new JsonObject
        {
            ["isoextract_version"] = assemblyVersion,
            ["file_type"] = ext.TrimStart('.'),
            ["file_size_bytes"] = new FileInfo(inputPath).Length,
        };
        var liarcRoot = new JsonObject();
        liarcRoot["meta"] = liarcMeta;
        Exception? liarcEx = null;
        try
        {
            using var zip = new System.IO.Compression.ZipArchive(
                File.OpenRead(inputPath), System.IO.Compression.ZipArchiveMode.Read);
            LiarcReader.Read(zip, liarcRoot);
        }
        catch (Exception ex) { liarcEx = ex; }
        finally
        {
            liarcMeta["complete"] = liarcEx is null;
            if (!dryRun)
            {
                string json = liarcRoot.ToJsonString(options);
                if (prettyJson) json = CollapseNumberArrays(json);
                File.WriteAllText(outputPath2, json);
                Console.WriteLine($"Written: {displayPath}.json{(liarcEx is not null ? " (incomplete)" : "")}");
            }
            else
            {
                Console.WriteLine($"Parsed (dry run): {displayPath}{(liarcEx is not null ? " (incomplete)" : "")}");
            }
            if (liarcEx is not null)
            {
                Console.Error.WriteLine($"Error processing {Path.GetFileName(inputPath)}: {liarcEx.Message}");
                Interlocked.Exchange(ref exitCode, 1);
            }
            if (!dryRun)
            {
                string issuesLogPath = inputPath + ".issues.log";
                if (liarcEx is not null)
                    File.WriteAllText(issuesLogPath, $"error: {liarcEx.Message}\n");
                else
                    File.Delete(issuesLogPath);
            }
            if (logWriter is not null)
            {
                bool success = liarcEx is null;
                string error = liarcEx?.Message ?? "";
                string line = $"{CsvField(displayPath)},{success.ToString().ToLowerInvariant()},{sw.ElapsedMilliseconds},\"{error.Replace("\"", "\"\"")}\"";
                lock (logLock) logWriter.WriteLine(line);
            }
        }
        return;
    }

    using var stream = File.OpenRead(inputPath);
    {
        Span<byte> magic = stackalloc byte[2];
        if (stream.Read(magic) < 2 || magic[0] != 0xFF || magic[1] != 0xFF)
        {
            Console.Error.WriteLine($"Not an isodat file: {Path.GetFileName(inputPath)}");
            Interlocked.Exchange(ref exitCode, 1);
            if (!dryRun) File.WriteAllText(inputPath + ".issues.log", "error: not an isodat file\n");
            return;
        }
        stream.Seek(0, SeekOrigin.Begin);
    }
    using var archive = new IsodatFile(stream);

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
            File.WriteAllText(outputPath2, json);
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
    $"\"{value.Replace("\"", "\"\"")}\"";

// Replaces multi-line pretty-printed number arrays with a single compact line.
static string CollapseNumberArrays(string json)
{
    // primitive = null | true | false | number | "string"
    const string prim = @"(?:null|true|false|-?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?|""(?:[^""\\]|\\.)*"")";
    return Regex.Replace(json,
        @"\[(?:\s*" + prim + @"\s*,)*\s*" + prim + @"\s*\]",
        static m => "[" + string.Join(", ",
            Regex.Matches(m.Value,
                @"null|true|false|-?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?|""(?:[^""\\]|\\.)*""")
                 .Select(n => n.Value)) + "]",
        RegexOptions.Singleline);
}

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
