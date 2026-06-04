// ImexpTraceReader.cs — parse MeasureDataIndexLines.bin + MeasureData.bin
// from TFS 253 Plus (Qtegra) .imexp.zip notebooks.
//
// Binary formats (all little-endian):
//
// MeasureDataIndexLines.bin
//   header:  int32 version(=1), int32 blockSize(=21)
//   records: byte version(=1), int32 setIndex, int32 lineIndex,
//            int32 integrationIndex, int64 position
//   last record is a sentinel (all indices = -1, position = MeasureData.bin size)
//
// MeasureData.bin
//   header:  int32 version(=1)
//   segments begin at each non-sentinel record's position offset
//   each segment is a sequence of fixed-size TraceSets:
//     uint16 version
//     int64  timestamp  (DateTime.ToBinary(): bits 62-63 = kind, bits 0-61 = .NET ticks)
//     int32  channelCount
//     channelCount × TracePoint:
//       uint16 version
//       double mass
//       bool   hasAnalog
//       [double analogIntensity]
//       bool   hasCounter
//       [double counterIntensity]
//   TraceSet size is fixed within a segment (channel layout constant).

using System.IO.Compression;
using System.Text.Json.Nodes;

static partial class ImexpReader
{
    // ── Public entry point ───────────────────────────────────────────────────

    static void ReadTraces(string imexpPath, JsonObject root)
    {
        var pairs = GatherTracePairs(imexpPath);
        var arr = new JsonArray();
        foreach (var (source, idxBytes, mdBytes, csfnBytes, adataBytes, smetaBytes) in pairs)
        {
            string? entryId = source.Split('/')
                .FirstOrDefault(p => p.StartsWith("Entry_", StringComparison.OrdinalIgnoreCase));
            var obj = new JsonObject { ["source"] = source };
            if (entryId is not null) obj["entry_id"] = entryId;
            if (csfnBytes.Length > 0)
                try { obj["settings_id"] = ParseSettingsFolderName(csfnBytes); }
                catch { /* omit on parse failure */ }
            try
            {
                var indexLines = ParseIndexLines(idxBytes);
                obj["segments"] = ParseMeasureData(mdBytes, indexLines);
            }
            catch (Exception ex) { obj["error"] = ex.Message; }
            if (adataBytes.Length > 0)
                try { var ad = ParseAdditionalData(adataBytes); if (ad is not null) obj["additional_data"] = ad; }
                catch (Exception ex) { obj["additional_data_error"] = ex.Message; }
            // SampleMetadata only exists for conflo-based dilution experiments; not read by default.
            // if (smetaBytes.Length > 0)
            //     try { var sm = ParseSampleMetadata(smetaBytes); if (sm is not null) obj["sample_metadata"] = sm; }
            //     catch (Exception ex) { obj["sample_metadata_error"] = ex.Message; }
            arr.Add(obj);
        }
        root["entries"] = arr;
    }

    // ── File gathering ───────────────────────────────────────────────────────

    static List<(string Source, byte[] IdxBytes, byte[] MdBytes, byte[] CsfnBytes, byte[] ADataBytes, byte[] SMetaBytes)> GatherTracePairs(string path)
    {
        var list = new List<(string, byte[], byte[], byte[], byte[], byte[])>();
        if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            using var zip = ZipFile.OpenRead(path);
            var byDir = new Dictionary<string, Dictionary<string, ZipArchiveEntry>>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in zip.Entries)
            {
                string dir  = e.FullName.Contains('/') ? e.FullName[..e.FullName.LastIndexOf('/')] : "";
                string name = Path.GetFileName(e.FullName);
                if (!byDir.TryGetValue(dir, out var files)) byDir[dir] = files = new();
                files[name] = e;
            }
            foreach (var (dir, files) in byDir.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                if (!files.TryGetValue("MeasureData.bin", out var mdEntry)) continue;
                files.TryGetValue("MeasureDataIndexLines.bin",      out var idxEntry);
                files.TryGetValue("CapturedSettingsFolderName.bin", out var csfnEntry);
                files.TryGetValue("AdditionalData",                 out var adataEntry);
                files.TryGetValue("SampleMetadata",                 out var smetaEntry);
                byte[] idxBytes   = idxEntry   is not null ? LoadZipEntry(idxEntry)   : Array.Empty<byte>();
                byte[] csfnBytes  = csfnEntry  is not null ? LoadZipEntry(csfnEntry)  : Array.Empty<byte>();
                byte[] adataBytes = adataEntry is not null ? LoadZipEntry(adataEntry) : Array.Empty<byte>();
                byte[] smetaBytes = smetaEntry is not null ? LoadZipEntry(smetaEntry) : Array.Empty<byte>();
                list.Add((dir, idxBytes, LoadZipEntry(mdEntry), csfnBytes, adataBytes, smetaBytes));
            }
        }
        else
        {
            foreach (var mdFile in Directory.GetFiles(path, "MeasureData.bin", SearchOption.AllDirectories).Order())
            {
                string dir       = Path.GetDirectoryName(mdFile)!;
                string idxFile   = Path.Combine(dir, "MeasureDataIndexLines.bin");
                string csfnFile  = Path.Combine(dir, "CapturedSettingsFolderName.bin");
                string adataFile = Path.Combine(dir, "AdditionalData");
                string smetaFile = Path.Combine(dir, "SampleMetadata");
                byte[] idxBytes   = File.Exists(idxFile)   ? File.ReadAllBytes(idxFile)   : Array.Empty<byte>();
                byte[] csfnBytes  = File.Exists(csfnFile)  ? File.ReadAllBytes(csfnFile)  : Array.Empty<byte>();
                byte[] adataBytes = File.Exists(adataFile) ? File.ReadAllBytes(adataFile) : Array.Empty<byte>();
                byte[] smetaBytes = File.Exists(smetaFile) ? File.ReadAllBytes(smetaFile) : Array.Empty<byte>();
                list.Add((Path.GetRelativePath(path, dir).Replace('\\', '/'), idxBytes, File.ReadAllBytes(mdFile), csfnBytes, adataBytes, smetaBytes));
            }
        }
        return list;
    }

    // SettingsFolderNameContainer (TypeId: 1c6dfa86-5ce9-4939-b7a2-6b568b5c57fb)
    const string SFC_FolderName = "1c6dfa86-5ce9-4939-b7a2-6b568b5c57fbm_folderName";

    static string? ParseSettingsFolderName(byte[] d)
    {
        var objs = ParseStream(d);
        if (!objs.TryGetValue(1, out var root)) return null;
        return GetString(root.Fields.GetValueOrDefault(SFC_FolderName), objs);
    }

    static byte[] LoadZipEntry(ZipArchiveEntry e)
    {
        using var ms = new MemoryStream((int)e.Length);
        using var s  = e.Open(); s.CopyTo(ms);
        return ms.ToArray();
    }

    // ── IndexLines ───────────────────────────────────────────────────────────

    readonly record struct TraceIndexLine(int SetIndex, int LineIndex, int IntegrationIndex, long Position);

    static List<TraceIndexLine> ParseIndexLines(byte[] d)
    {
        if (d.Length == 0) return new();
        if (d.Length < 8)  throw new InvalidDataException("MeasureDataIndexLines.bin too short");

        int ver = BitConverter.ToInt32(d, 0);
        int bsz = BitConverter.ToInt32(d, 4);
        if (ver != 1)  throw new InvalidDataException($"MeasureDataIndexLines: unexpected version {ver}");
        if (bsz != 21) throw new InvalidDataException($"MeasureDataIndexLines: unexpected block size {bsz}");

        int n = (d.Length - 8) / 21;
        var list = new List<TraceIndexLine>(n);
        for (int i = 0, p = 8; i < n; i++, p += 21)
        {
            // p+0: record version byte (skip)
            int  setIdx  = BitConverter.ToInt32(d, p + 1);
            int  lineIdx = BitConverter.ToInt32(d, p + 5);
            int  intIdx  = BitConverter.ToInt32(d, p + 9);
            long pos     = BitConverter.ToInt64(d, p + 13);
            list.Add(new TraceIndexLine(setIdx, lineIdx, intIdx, pos));
        }
        return list;
    }

    // ── MeasureData ──────────────────────────────────────────────────────────

    static JsonArray ParseMeasureData(byte[] d, List<TraceIndexLine> indexLines)
    {
        if (d.Length < 4) throw new InvalidDataException("MeasureData.bin too short");
        int ver = BitConverter.ToInt32(d, 0);
        if (ver != 1) throw new InvalidDataException($"MeasureData: unexpected version {ver}");

        List<TraceIndexLine> real;
        long fileEnd;
        if (indexLines.Count == 0)
        {
            real    = new List<TraceIndexLine> { new(0, 0, 0, 4) };
            fileEnd = d.Length;
        }
        else
        {
            real    = indexLines.Where(il => il.SetIndex != -1).ToList();
            var sentinel = indexLines.FirstOrDefault(il => il.SetIndex == -1);
            fileEnd = sentinel.Position > 0 ? sentinel.Position : d.Length;
        }

        var result = new JsonArray();
        for (int i = 0; i < real.Count; i++)
        {
            var il      = real[i];
            long segEnd = i + 1 < real.Count ? real[i + 1].Position : fileEnd;

            var segObj = new JsonObject
            {
                ["integration_line_set_index"] = il.SetIndex,
                ["line_index"]                 = il.LineIndex,
                ["integration_index"]          = il.IntegrationIndex,
            };
            try   { AppendSegmentData(segObj, d, il.Position, segEnd); }
            catch (Exception ex) { segObj["error"] = ex.Message; }
            result.Add(segObj);
        }
        return result;
    }

    static void AppendSegmentData(JsonObject obj, byte[] d, long start, long end)
    {
        long segBytes = end - start;
        if (segBytes < 14) return; // smaller than a TraceSet header

        // Peek at the first TraceSet to determine the fixed record layout.
        //   TraceSet header: uint16 version (2) + int64 timestamp (8) + int32 count (4) = 14 bytes
        int p = (int)start + 14;
        int nChannels = BitConverter.ToInt32(d, (int)start + 10);
        if (nChannels <= 0) return;

        double[] masses     = new double[nChannels];
        bool[]   hasAnalog  = new bool[nChannels];
        bool[]   hasCounter = new bool[nChannels];
        for (int ch = 0; ch < nChannels; ch++)
        {
            p += 2;                                   // TracePoint uint16 version
            masses[ch]     = BitConverter.ToDouble(d, p); p += 8;
            hasAnalog[ch]  = d[p++] != 0;
            if (hasAnalog[ch])  p += 8;              // skip intensity during peek
            hasCounter[ch] = d[p++] != 0;
            if (hasCounter[ch]) p += 8;
        }

        // Fixed TraceSet size
        int tpBytes = 0;
        for (int ch = 0; ch < nChannels; ch++)
            tpBytes += 2 + 8 + 1 + (hasAnalog[ch] ? 8 : 0) + 1 + (hasCounter[ch] ? 8 : 0);
        int tsSize = 14 + tpBytes;

        int nPoints = (int)(segBytes / tsSize);
        if (nPoints == 0) return;

        // Per-channel intensity offset within a TraceSet (0-based from TraceSet start).
        // TracePoint layout:  version(2) + mass(8) + hasAnalog(1) = 11 bytes to analog intensity
        //                     version(2) + mass(8) + hasAnalog(1) + hasCounter(1) = 12 to counter intensity
        int[]    intensOffset = new int[nChannels];
        string[] detectors    = new string[nChannels];
        {
            int off = 14;
            for (int ch = 0; ch < nChannels; ch++)
            {
                if (hasAnalog[ch])
                {
                    intensOffset[ch] = off + 11;
                    detectors[ch]    = "analog";
                }
                else if (hasCounter[ch])
                {
                    intensOffset[ch] = off + 12;
                    detectors[ch]    = "counter";
                }
                else
                {
                    intensOffset[ch] = -1;
                    detectors[ch]    = "none";
                }
                off += 2 + 8 + 1 + (hasAnalog[ch] ? 8 : 0) + 1 + (hasCounter[ch] ? 8 : 0);
            }
        }

        // Read all time points
        var rawTicks   = new long[nPoints];
        var intensities = new double[nChannels][];
        for (int ch = 0; ch < nChannels; ch++) intensities[ch] = new double[nPoints];

        for (int t = 0; t < nPoints; t++)
        {
            int tsBase = (int)start + t * tsSize;

            // Timestamp: int64 at TraceSet offset 2 (after uint16 version).
            // DateTime.ToBinary() format: bits 62-63 = DateTimeKind, bits 0-61 = .NET ticks.
            long binary  = BitConverter.ToInt64(d, tsBase + 2);
            rawTicks[t]  = binary & 0x3FFFFFFFFFFFFFFF;

            for (int ch = 0; ch < nChannels; ch++)
                if (intensOffset[ch] >= 0)
                    intensities[ch][t] = BitConverter.ToDouble(d, tsBase + intensOffset[ch]);
        }

        // Store time as seconds elapsed since the first time point
        var timeArr = new JsonArray();
        double t0s = rawTicks[0] / 1e7;
        for (int t = 0; t < nPoints; t++)
            timeArr.Add((JsonNode)Math.Round(rawTicks[t] / 1e7 - t0s, 7));

        obj["n_timepoints"] = nPoints;
        obj["time_s"]       = timeArr;

        var channelsArr = new JsonArray();
        for (int ch = 0; ch < nChannels; ch++)
        {
            var intensArr = new JsonArray();
            foreach (var v in intensities[ch]) intensArr.Add(v);
            channelsArr.Add(new JsonObject
            {
                ["mass"]      = masses[ch],
                ["detector"]  = detectors[ch],
                ["intensity"] = intensArr,
            });
        }
        obj["channels"] = channelsArr;
    }
}
