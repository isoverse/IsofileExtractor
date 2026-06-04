using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace IsofileExtractor;

static class BchReader
{
    static readonly Regex DatasetIdRx = new(@"_P_([0-9]+)_G_", RegexOptions.Compiled);
    static readonly HashSet<string> BlockMarkers =
        new(StringComparer.Ordinal) { "S", "R", "B" };

    public static void Read(string bchDir, JsonObject root)
    {
        string folderName = Path.GetFileName(bchDir);
        string resultsDir = Path.Combine(bchDir, "Results");

        if (!Directory.Exists(resultsDir))
            throw new InvalidDataException($"No 'Results' directory found in: {bchDir}");

        string recPath = Path.Combine(resultsDir, folderName + ".rec");
        if (!File.Exists(recPath))
        {
            string[] recFiles = Directory.GetFiles(resultsDir, "*.rec");
            recPath = recFiles.Length > 0 ? recFiles[0]
                : throw new InvalidDataException($"No .rec file found in: {resultsDir}");
        }

        string prnPath = Path.Combine(resultsDir, "ReprocessedData.prn");
        if (!File.Exists(prnPath))
            throw new InvalidDataException($"Missing ReprocessedData.prn in: {resultsDir}");

        var (header, prnSections) = ParsePrn(prnPath);
        var (recVersion, recBlocks) = ParseRec(recPath);

        root["header"] = new JsonObject
        {
            ["source"]             = Path.GetRelativePath(bchDir, prnPath),
            ["system_description"] = header["system_description"]?.GetValue<string>(),
            ["timestamp"]          = header["timestamp"]?.GetValue<string>(),
        };

        string mcpPath = Path.Combine(bchDir, "Method", "Setups", "MultiCollector_A.mcp");
        List<McpBeam> mcpBeams = [];
        if (File.Exists(mcpPath))
        {
            var (mcpFormat, parsedBeams) = ParseMcp(mcpPath);
            mcpBeams = parsedBeams;
            if (mcpBeams.Count > 0)
            {
                var collectorsObj = new JsonObject
                {
                    ["source"] = Path.GetRelativePath(bchDir, mcpPath),
                    ["format"] = mcpFormat,
                };
                var beamsArr = new JsonArray();
                foreach (McpBeam b in mcpBeams)
                {
                    long res1 = (long)b.ResVal1 * 1_000_000L;
                    long res2 = (long)b.ResVal2 * 1_000_000L;
                    var bo = new JsonObject
                    {
                        ["beam_num"]              = b.BeamNum,
                        ["enabled"]               = b.Enabled,
                        ["slot"]                  = b.Slot,
                        ["res_type"]              = b.ResType,
                        ["resistance_1_ohm"]      = res1,
                        ["resistance_2_ohm"]      = res2,
                        ["active_resistance_ohm"] = b.Flag2 ? res2 : res1,
                    };
                    // b.Mass omitted: only populated for some MCP formats and reflects the
                    // calibration-gas mass, not a stable per-beam property.
                    if (b.UsageType is not null)   bo["usage_type"]            = b.UsageType;
                    if (b.DenominatorBeamNum != 0) bo["denominator_beam_num"]  = b.DenominatorBeamNum;
                    beamsArr.Add(bo);
                }
                collectorsObj["beams"] = beamsArr;
                root["collectors"] = collectorsObj;
            }
        }

        // Parse .set files for methods used by at least one rec block
        string methodDir  = Path.Combine(bchDir, "Method");
        string setupsDir  = Path.Combine(methodDir, "Setups");
        var methodsList = new List<JsonObject>();
        foreach (string methodName in recBlocks
            .Select(b => b.Method)
            .Where(m => !string.IsNullOrEmpty(m))
            .Distinct())
        {
            string methodKey = methodName.EndsWith(".set", StringComparison.OrdinalIgnoreCase)
                ? methodName : methodName + ".set";
            string methodBase = methodKey[..^4]; // strip .set
            string setPath = Path.Combine(setupsDir, methodKey);
            JsonObject? setData = ParseSet(setPath);
            if (setData is not null)
            {
                var methodObj = new JsonObject
                {
                    ["method"] = methodBase,
                    ["source"] = Path.GetRelativePath(bchDir, setPath),
                };
                foreach (var kv in setData) methodObj[kv.Key] = kv.Value?.DeepClone();
                methodsList.Add(methodObj);
            }
        }
        if (methodsList.Count > 0)
        {
            var methodsArr = new JsonArray();
            foreach (var m in methodsList) methodsArr.Add(m);
            root["methods"] = methodsArr;
        }

        // Parse .par timing files referenced by methods
        string paramsDir = Path.Combine(methodDir, "Parameters");
        var timingsArr = new JsonArray();
        var seenPar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in methodsList)
        {
            string? atFile = m["analysis_timing_file"]?.GetValue<string>();
            if (string.IsNullOrEmpty(atFile)) continue;
            string parBase = atFile.EndsWith(".par", StringComparison.OrdinalIgnoreCase)
                ? atFile[..^4] : atFile;
            string parKey = parBase + ".par";
            if (!seenPar.Add(parKey)) continue;
            string parPath = Path.Combine(paramsDir, parKey);
            JsonObject? parData = ParsePar(parPath);
            if (parData is null) continue;
            var entry = new JsonObject
            {
                ["timing"] = parBase,
                ["source"] = Path.GetRelativePath(bchDir, parPath),
            };
            foreach (var pkv in parData) entry[pkv.Key] = pkv.Value?.DeepClone();
            timingsArr.Add(entry);
        }
        if (timingsArr.Count > 0) root["timings"] = timingsArr;

        // Parse .evt event sequence files referenced by methods
        string eventsDir = Path.Combine(methodDir, "Events");
        var eventsArr = new JsonArray();
        var seenEvt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in methodsList)
        {
            string? evtFile = m["event_sequence_file"]?.GetValue<string>();
            if (string.IsNullOrEmpty(evtFile) ||
                evtFile.Equals("NONE.evt", StringComparison.OrdinalIgnoreCase) ||
                evtFile.Equals("NONE", StringComparison.OrdinalIgnoreCase)) continue;
            string evtBase = evtFile.EndsWith(".evt", StringComparison.OrdinalIgnoreCase)
                ? evtFile[..^4] : evtFile;
            string evtKey = evtBase + ".evt";
            if (!seenEvt.Add(evtKey)) continue;
            string evtPath = Path.Combine(eventsDir, evtKey);
            JsonObject? evtData = ParseEvt(evtPath);
            if (evtData is null) continue;
            var entry = new JsonObject
            {
                ["event"] = evtBase,
                ["source"] = Path.GetRelativePath(bchDir, evtPath),
            };
            foreach (var ekv in evtData) entry[ekv.Key] = ekv.Value?.DeepClone();
            eventsArr.Add(entry);
        }
        if (eventsArr.Count > 0) root["events"] = eventsArr;

        if (prnSections.Count == 0)
            throw new InvalidDataException($"No data sections found in: {prnPath}");

        // Primary section (un-drift-corrected) is joined with rec blocks
        PrnSection primary = prnSections[0];

        // Build data section: raw recorded data from the .rec file
        var recObj = new JsonObject
        {
            ["source"]  = Path.GetRelativePath(bchDir, recPath),
            ["version"] = recVersion,
        };
        var recBlocksArr = new JsonArray();
        foreach (RecBlock b in recBlocks)
        {
            var rb = new JsonObject
            {
                ["type"]    = b.Type,
                ["name"]    = b.Name,
                ["method"]  = b.Method,
                ["n_scans"] = b.NScans,
            };
            if (b.Weight.HasValue)   rb["weight"]  = b.Weight.Value;
            if (b.ScanId is not null) rb["scan_id"] = b.ScanId;

            var traceObj = new JsonObject();
            if (b.TimeS is not null)
            {
                if (b.NScans > 0)
                    rb["acquisition_duration_s"] = b.TimeS.Max() - b.TimeS.Min();
                traceObj["time_s"] = BuildDoubleArray(b.TimeS);
            }
            if (b.TimeCounterRaw is not null)
            {
                var tcArr = new JsonArray();
                foreach (long v in b.TimeCounterRaw) tcArr.Add(v);
                traceObj["time_counter_raw"] = tcArr;
            }
            for (int beam = 0; beam < b.NBeams; beam++)
                traceObj[$"beam{beam + 1}_a"] = BuildDoubleArray(b.Beams[beam]);
            rb["traces"] = traceObj;

            recBlocksArr.Add(rb);
        }
        recObj["blocks"] = recBlocksArr;
        root["data"] = recObj;

        // Build results: columnar format from .prn, one entry per field across all analyses
        var resultsObj = new JsonObject { ["source"] = Path.GetRelativePath(bchDir, prnPath) };

        // Build secondary lookup by N for drift-corrected values
        int idxN2 = ColIdx(prnSections.Count > 1 ? prnSections[1].ColHeaders : primary.ColHeaders, "N");
        Dictionary<int, Dictionary<int, string>>? secondary = null;
        if (prnSections.Count > 1 && idxN2 >= 0)
        {
            secondary = new Dictionary<int, Dictionary<int, string>>();
            foreach (var row in prnSections[1].Rows)
                if (row.TryGetValue(idxN2, out string? nStr) && int.TryParse(nStr, out int nv))
                    secondary[nv] = row;
        }

        int nRows = primary.Rows.Count;
        var columnsArr = new JsonArray();

        void AddCol(string label, string? units, JsonNode?[] vals, JsonNode?[]? driftVals = null)
        {
            var col = new JsonObject { ["label"] = label };
            if (units is not null) col["units"] = units;
            var arr = new JsonArray();
            foreach (var v in vals) arr.Add(v);
            col["values"] = arr;
            if (driftVals is not null)
            {
                var darr = new JsonArray();
                foreach (var v in driftVals) darr.Add(v);
                col["values_drift_corrected"] = darr;
            }
            columnsArr.Add(col);
        }

        // Precompute structural column indices
        int idxN      = ColIdx(primary.ColHeaders, "N");
        int idxName   = ColIdx(primary.ColHeaders, "Name");
        int idxType   = ColIdx(primary.ColHeaders, "Type");
        int idxWeight = ColIdx(primary.ColHeaders, "Weight/Vol");
        int idxStatus = ColIdx(primary.ColHeaders, "Status");

        // Structural columns
        var ids        = new JsonNode?[nRows];
        var names      = new JsonNode?[nRows];
        var types      = new JsonNode?[nRows];
        var datasetIds = new JsonNode?[nRows];
        var weights    = new JsonNode?[nRows];
        var statuses   = new JsonNode?[nRows];
        bool anyStatus = false;

        for (int i = 0; i < nRows; i++)
        {
            var row = primary.Rows[i];
            double? recWeight = i < recBlocks.Count ? recBlocks[i].Weight : null;

            ids[i] = idxN >= 0 && row.TryGetValue(idxN, out string? nStr) && int.TryParse(nStr, out int nv)
                ? JsonValue.Create(nv) : null;

            string name = idxName >= 0 && row.TryGetValue(idxName, out string? nameStr) ? nameStr : "";
            names[i] = JsonValue.Create(name);

            string? typeVal = idxType >= 0 && row.TryGetValue(idxType, out string? t) && !string.IsNullOrWhiteSpace(t) ? t : null;
            types[i] = typeVal is not null ? JsonValue.Create(typeVal) : null;

            datasetIds[i] = ExtractDatasetId(name) is string did ? JsonValue.Create(did) : null;

            double? weight = null;
            if (idxWeight >= 0 && row.TryGetValue(idxWeight, out string? wstr) &&
                double.TryParse(wstr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double wv))
                weight = wv;
            else if (recWeight is double bw)
                weight = bw;
            weights[i] = weight.HasValue ? JsonValue.Create(weight.Value) : null;

            string? statusVal = null;
            if (idxStatus >= 0 && row.TryGetValue(idxStatus, out string? sv) && !string.IsNullOrWhiteSpace(sv))
            {
                statusVal = sv.TrimStart('#').Trim();
                anyStatus = true;
            }
            statuses[i] = statusVal is not null ? JsonValue.Create(statusVal) : null;
        }

        AddCol("id",         null, ids);
        AddCol("name",       null, names);
        AddCol("type",       null, types);
        AddCol("dataset_id", null, datasetIds);
        AddCol("weight",     null, weights);
        if (anyStatus) AddCol("status", null, statuses);

        // Data columns from .prn measurement section — iterate by index to handle
        // duplicate column names (e.g. "Ratio 1" repeated per gas species section).
        for (int colIdx = 0; colIdx < primary.ColHeaders.Count; colIdx++)
        {
            string col = primary.ColHeaders[colIdx];
            if (!IsDataColumn(col)) continue;
            string? units = primary.ColUnits.TryGetValue(colIdx, out string? u) && !string.IsNullOrWhiteSpace(u) ? u : null;

            var vals      = new JsonNode?[nRows];
            var driftVals = secondary is not null ? new JsonNode?[nRows] : null;
            bool anyDrift = false;

            for (int i = 0; i < nRows; i++)
            {
                var row = primary.Rows[i];
                if (row.TryGetValue(colIdx, out string? valStr))
                {
                    if (double.TryParse(valStr, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out double dv))
                        vals[i] = JsonValue.Create(dv);
                    else if (!string.IsNullOrWhiteSpace(valStr))
                        vals[i] = JsonValue.Create(valStr);
                }

                if (secondary is not null && driftVals is not null &&
                    idxN >= 0 && row.TryGetValue(idxN, out string? nStr) && int.TryParse(nStr, out int nv) &&
                    secondary.TryGetValue(nv, out var secRow) &&
                    secRow.TryGetValue(colIdx, out string? secValStr))
                {
                    if (double.TryParse(secValStr, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out double sdv))
                    { driftVals[i] = JsonValue.Create(sdv); anyDrift = true; }
                    else if (!string.IsNullOrWhiteSpace(secValStr))
                    { driftVals[i] = JsonValue.Create(secValStr); anyDrift = true; }
                }
            }

            AddCol(col, units, vals, anyDrift ? driftVals : null);
        }

        resultsObj["columns"] = columnsArr;
        root["results"] = resultsObj;
    }

    // ── .prn ─────────────────────────────────────────────────────────────

    // Rows and ColUnits are keyed by column index, not name, so duplicate
    // column names (e.g. "Ratio 1" repeating per gas section) are preserved.
    record PrnSection(List<string> ColHeaders,
        Dictionary<int, string> ColUnits,
        List<Dictionary<int, string>> Rows);

    static int ColIdx(List<string> headers, string name) =>
        headers.FindIndex(h => h.Equals(name, StringComparison.OrdinalIgnoreCase));

    static (JsonObject Header, List<PrnSection> Sections) ParsePrn(string path)
    {
        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 7)
            throw new InvalidDataException($"Unexpected .prn file (too few lines): {path}");

        var header = new JsonObject
        {
            ["system_description"] = lines[0].Trim(),
            ["timestamp"] = lines[1].Trim(),
        };

        // Parse all sections; each section starts with a non-data label line
        // followed by a column-header line, a units line, then data rows.
        var sections = new List<PrnSection>();

        // Line 4 (index 4) is the first section label (e.g. "Un-Drift Corrected")
        // Lines 5/6 (index 5/6) are col headers + units for the first section.
        int li = 4;
        while (li < lines.Length)
        {
            // Expect section label
            string sectionLabel = lines[li].Trim();
            if (string.IsNullOrWhiteSpace(sectionLabel)) { li++; continue; }
            li++;
            if (li >= lines.Length) break;

            // Expect column header line (contains N as first tab field)
            string headerLine = lines[li].Trim();
            if (string.IsNullOrEmpty(headerLine)) { li++; continue; }
            li++;
            if (li >= lines.Length) break;

            // Expect units line — split WITHOUT trimming the whole line first,
            // because leading whitespace/tabs encode empty fields for the first columns.
            string[] unitTokens = lines[li].Split('\t');
            li++;

            // Parse column names and per-index units
            List<string> colHeaders = headerLine.Split('\t')
                .Select(s => s.Trim()).ToList();
            var colUnits = new Dictionary<int, string>();
            for (int c = 0; c < colHeaders.Count; c++)
                colUnits[c] = c < unitTokens.Length ? unitTokens[c].Trim() : "";

            // Read data rows until a non-data line (next section label or EOF)
            var rows = new List<Dictionary<int, string>>();
            while (li < lines.Length)
            {
                string line = lines[li];
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) { li++; continue; }
                // Data rows start with numeric N; non-numeric lines start a new section
                if (!int.TryParse(line.Split('\t')[0].Trim(), out _)) break;
                // Skip END row (terminator; END appears in the Name column)
                if (line.Split('\t').Any(f => f.Trim().Equals("END", StringComparison.OrdinalIgnoreCase)))
                { li++; continue; }

                string[] vals = line.Split('\t');
                var row = new Dictionary<int, string>();
                for (int c = 0; c < colHeaders.Count && c < vals.Length; c++)
                    row[c] = vals[c].Trim();
                rows.Add(row);
                li++;
            }

            sections.Add(new PrnSection(colHeaders, colUnits, rows));
        }

        return (header, sections);
    }

    // ── .rec ─────────────────────────────────────────────────────────────

    // TimeS: seconds from last float column (v3.0 only); null for v5.0
    // TimeCounterRaw: integer time counter at extra[4] (v5.0 only); null for v3.0
    record RecBlock(
        string Type, string Name, double? Weight, string Method,
        int NBeams, int NScans, string? ScanId,
        double[]? TimeS, long[]? TimeCounterRaw, double[][] Beams);

    static (string Version, List<RecBlock> Blocks) ParseRec(string path)
    {
        string[] lines = File.ReadAllLines(path);
        // Strip the ">N~" suffix the Callisto software appends after the version tag
        // (e.g. "v5.0>0~" → "v5.0"). The software's own reader uses InStr() substring match.
        string rawVersion = lines.Length > 0 ? lines[0].Trim() : "";
        int versionEnd = rawVersion.IndexOf('>');
        string version = versionEnd > 0 ? rawVersion[..versionEnd] : rawVersion;
        bool isV5 = version.StartsWith("v5", StringComparison.OrdinalIgnoreCase);

        var blocks = new List<RecBlock>();
        int n = lines.Length;
        int i = 0;

        while (i < n)
        {
            string tok = lines[i].Trim();
            if (!BlockMarkers.Contains(tok) || i + 5 >= n)
            {
                i++;
                continue;
            }

            string bType = tok;
            string bName = lines[i + 1].Trim();

            double? weight = double.TryParse(lines[i + 2].Trim(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double bw) ? bw : null;

            string method = lines[i + 3].Trim();
            if (method.EndsWith(".set", StringComparison.OrdinalIgnoreCase))
                method = method[..^4];

            if (!int.TryParse(lines[i + 4].Trim(), out int rowWidth) || rowWidth < 2)
            { i++; continue; }
            if (!int.TryParse(lines[i + 5].Trim(), out int scanCount) || scanCount <= 0)
            { i++; continue; }

            // v3.0: last column is time_s; v5.0: all columns are beams
            int nBeams = isV5 ? rowWidth : rowWidth - 1;
            int dataStart;
            string? scanId = null;

            if (isV5 && i + 6 < n && lines[i + 6].Trim().StartsWith("~", StringComparison.Ordinal))
            {
                scanId = lines[i + 6].Trim();
                dataStart = i + 7;
            }
            else
            {
                dataStart = i + 6;
            }

            RecBlock? block = isV5
                ? ParseV5Block(lines, dataStart, scanCount, nBeams, bType, bName, weight, method, scanId)
                : ParseV3Block(lines, dataStart, nBeams, bType, bName, weight, method);

            if (block is not null)
            {
                blocks.Add(block);
                i = isV5
                    ? dataStart + block.NScans
                    : dataStart + block.NScans * rowWidth;
            }
            else
            {
                i++;
            }
        }

        return (version, blocks);
    }

    // v3.0: one value per line; last column is time_s
    static RecBlock? ParseV3Block(string[] lines, int dataStart,
        int nBeams, string bType, string bName, double? weight, string method)
    {
        // Scan forward to count available data lines (stop at next block marker or non-numeric)
        int rowWidth = nBeams + 1;
        int maxLines = 0;
        while (dataStart + maxLines < lines.Length)
        {
            string t = lines[dataStart + maxLines].Trim();
            if (string.IsNullOrEmpty(t) || BlockMarkers.Contains(t)) break;
            maxLines++;
        }
        int scanCount = maxLines / rowWidth;
        if (scanCount == 0) return null;

        var timeS = new double[scanCount];
        var beams = new double[nBeams][];
        for (int b = 0; b < nBeams; b++) beams[b] = new double[scanCount];

        for (int s = 0; s < scanCount; s++)
        {
            for (int c = 0; c < rowWidth; c++)
            {
                double.TryParse(lines[dataStart + s * rowWidth + c].Trim(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double val);
                if (c < nBeams) beams[c][s] = val;
                else timeS[s] = val;
            }
        }

        return new RecBlock(bType, bName, weight, method, nBeams, scanCount, null, timeS, null, beams);
    }

    // v5.0: tab-separated per line; all float columns are beams; extra[4] is time counter
    static RecBlock? ParseV5Block(string[] lines, int dataStart, int scanCount,
        int nBeams, string bType, string bName, double? weight, string method, string? scanId)
    {
        if (dataStart >= lines.Length) return null;

        var beams = new double[nBeams][];
        for (int b = 0; b < nBeams; b++) beams[b] = new double[scanCount];
        var timeCounter = new long[scanCount];

        int actual = 0;
        for (int s = 0; s < scanCount && dataStart + s < lines.Length; s++)
        {
            string line = lines[dataStart + s];
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || BlockMarkers.Contains(trimmed)) break;

            string[] parts = line.Split('\t');
            if (parts.Length < nBeams) break;

            bool ok = true;
            for (int c = 0; c < nBeams; c++)
            {
                if (!double.TryParse(parts[c].Trim(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double val))
                { ok = false; break; }
                beams[c][s] = val;
            }
            if (!ok) break;

            // extra integers start at index nBeams; extra[4] = time counter
            int tcIdx = nBeams + 4;
            if (tcIdx < parts.Length &&
                long.TryParse(parts[tcIdx].Trim(), out long tc))
                timeCounter[s] = tc;

            actual++;
        }

        if (actual == 0) return null;

        if (actual < scanCount)
        {
            timeCounter = timeCounter[..actual];
            for (int b = 0; b < nBeams; b++) beams[b] = beams[b][..actual];
        }

        // Unwrap the uint16 hardware counter (wraps at 65536) before computing time.
        // Raw values are kept in TimeCounterRaw; unwrapped values are used only for time_s.
        var unwrapped = new long[actual];
        unwrapped[0] = timeCounter[0];
        long overflow = 0;
        for (int s = 1; s < actual; s++)
        {
            if (timeCounter[s] < timeCounter[s - 1]) overflow += 65536L;
            unwrapped[s] = timeCounter[s] + overflow;
        }

        // Convert hardware counter to block-relative elapsed seconds.
        // The counter is a 10 Hz absolute clock; hardware discards the first 2 scans
        // (stabilization delay) before writing to file, so counter[0] is already 2×dt_ticks
        // past block start. To produce block-relative time starting at 2×dt (matching v3.0's
        // stored time_s convention), add that 2-scan offset back:
        //   time_s[i] = (unwrapped[i] - unwrapped[0] + 2 × dt_ticks) / 10.0
        long dt_ticks = actual > 1 ? (unwrapped[1] - unwrapped[0]) : 10L;
        long t0 = unwrapped[0];
        double[] timeS = new double[actual];
        for (int s = 0; s < actual; s++)
            timeS[s] = (unwrapped[s] - t0 + 2 * dt_ticks) / 10.0;

        return new RecBlock(bType, bName, weight, method, nBeams, actual, scanId, timeS, timeCounter[..actual], beams);
    }

    // ── .mcp ─────────────────────────────────────────────────────────────

    record McpBeam(int BeamNum, bool Enabled, int? Mass, int Slot,
        int ResType, bool Flag2, int ResVal1, int ResVal2, string? UsageType, int DenominatorBeamNum);

    static (int Format, List<McpBeam> Beams) ParseMcp(string path)
    {
        // Tokenize: one value per line, skip blanks
        List<string> tokens = File.ReadAllLines(path)
            .Select(l => l.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        // Header: token[0..4] = "1", "False", "Default", "1", "<format>"
        if (tokens.Count < 5 || !int.TryParse(tokens[4], out int format))
            return (0, []);

        var beams = new List<McpBeam>();
        int idx = 5;

        if (format == 3)
        {
            // Format 3: each beam = mass (numeric) + 15 tokens
            // Layout: [mass, enabled, beam_num, slot, res_type, field4, res_val1, res_val2,
            //          color_fg, color_bg, flag2, usage_type, label1, label2, label3, label4]
            while (idx + 16 <= tokens.Count)
            {
                if (!int.TryParse(tokens[idx], out int mass)) break;
                bool enabled = tokens[idx + 1].Equals("True", StringComparison.OrdinalIgnoreCase);
                int.TryParse(tokens[idx + 2], out int beamNum);
                int.TryParse(tokens[idx + 3], out int slot);
                int.TryParse(tokens[idx + 4], out int resType);
                int.TryParse(tokens[idx + 5], out int denomBeamNum);  // denominator beam_num (0 = self = reference)
                int.TryParse(tokens[idx + 6], out int resVal1);
                int.TryParse(tokens[idx + 7], out int resVal2);
                bool flag2 = tokens[idx + 10].Equals("True", StringComparison.OrdinalIgnoreCase);
                string? usageType = tokens[idx + 11] is { } u3 && u3 != "Spare" ? u3 : null;
                beams.Add(new McpBeam(beamNum, enabled, mass, slot, resType, flag2, resVal1, resVal2, usageType, denomBeamNum));
                idx += 16;
            }
        }
        else if (format == 4)
        {
            // Format 4: each beam = 15 tokens; inactive beams may have an optional
            // mass/label token after the 15 tokens (before the next True/False or EOF).
            // Layout: [enabled, beam_num, slot, res_type, field4, res_val1, res_val2,
            //          color_fg, color_bg, flag2, usage_type, label1, label2, label3, label4]
            while (idx < tokens.Count)
            {
                // Skip to next True/False (beam start marker)
                while (idx < tokens.Count &&
                    !tokens[idx].Equals("True", StringComparison.OrdinalIgnoreCase) &&
                    !tokens[idx].Equals("False", StringComparison.OrdinalIgnoreCase))
                    idx++;

                if (idx + 15 > tokens.Count) break;

                bool enabled = tokens[idx].Equals("True", StringComparison.OrdinalIgnoreCase);
                int.TryParse(tokens[idx + 1], out int beamNum);
                int.TryParse(tokens[idx + 2], out int slot);
                int.TryParse(tokens[idx + 3], out int resType);
                int.TryParse(tokens[idx + 4], out int denomBeamNum);  // denominator beam_num (0 = self = reference)
                int.TryParse(tokens[idx + 5], out int resVal1);
                int.TryParse(tokens[idx + 6], out int resVal2);
                bool flag2 = tokens[idx + 9].Equals("True", StringComparison.OrdinalIgnoreCase);
                string? usageType = tokens[idx + 10] is { } u4 && u4 != "Spare" ? u4 : null;
                idx += 15;

                // Optional mass token: present if next token is not a beam start (True/False)
                int? mass = null;
                if (idx < tokens.Count &&
                    !tokens[idx].Equals("True", StringComparison.OrdinalIgnoreCase) &&
                    !tokens[idx].Equals("False", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(tokens[idx], out int labelMass))
                        mass = labelMass;
                    idx++;
                }

                beams.Add(new McpBeam(beamNum, enabled, mass, slot, resType, flag2, resVal1, resVal2, usageType, denomBeamNum));
            }
        }

        return (format, beams);
    }

    // ── .set ─────────────────────────────────────────────────────────────

    static JsonObject? ParseSet(string path)
    {
        if (!File.Exists(path)) return null;

        string[] lines = File.ReadAllLines(path)
            .Select(l => l.Trim())
            .ToArray();

        if (lines.Length < 11) return null;

        var jo = new JsonObject
        {
            ["description"]               = lines[0],
            ["reference_file"]            = lines[1],
            ["analysis_timing_file"]      = lines[2],
            ["run_mode"]                  = lines[3],
            ["event_sequence_file"]       = lines[4],
            ["auto_sampler_sequence_file"] = lines[5],
            ["multicollector_file"]       = lines[6],
            ["peak_centre_file"]          = lines[7],
            ["line_9"]                    = lines[8],
            ["line_11"]                   = lines[10],
        };

        // data_rate_hz: "0" → 1 Hz, "1" → 10 Hz, otherwise raw string
        if (lines[9] == "0")      jo["data_rate_hz"] = 1;
        else if (lines[9] == "1") jo["data_rate_hz"] = 10;
        else                      jo["data_rate_hz"] = lines[9];

        if (lines.Length >= 15)
        {
            jo["line_12"]        = lines[11];
            jo["line_13"]        = lines[12];
            jo["element_by_tcd"] = lines[13].Equals("True", StringComparison.OrdinalIgnoreCase);
            jo["line_15"]        = lines[14];
        }

        return jo;
    }


    // ── .evt ─────────────────────────────────────────────────────────────

    static JsonObject? ParseEvt(string path)
    {
        if (!File.Exists(path)) return null;

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 3) return null;

        bool isV2 = lines[0].Trim().Equals("Version 2", StringComparison.OrdinalIgnoreCase);

        string description;
        int totalTimeS, nEvents, headerLines, linesPerEvent;

        if (isV2)
        {
            if (lines.Length < 4) return null;
            description  = lines[1].Trim();
            if (!int.TryParse(lines[2].Trim(), out totalTimeS)) return null;
            if (!int.TryParse(lines[3].Trim(), out nEvents) || nEvents <= 0) return null;
            headerLines  = 4;
            linesPerEvent = 4; // time, event, L/R, comment
        }
        else
        {
            description  = lines[0].Trim();
            if (!int.TryParse(lines[1].Trim(), out totalTimeS)) return null;
            if (!int.TryParse(lines[2].Trim(), out nEvents) || nEvents <= 0) return null;
            headerLines  = 3;
            linesPerEvent = 3; // time, event, L/R (no comment)
        }

        var jo = new JsonObject { ["total_run_time_s"] = totalTimeS };
        if (!string.IsNullOrEmpty(description)) jo["description"] = description;

        var eventsArr = new JsonArray();
        for (int e = 0; e < nEvents; e++)
        {
            int pos = headerLines + e * linesPerEvent;
            if (pos + linesPerEvent - 1 >= lines.Length) break;

            int.TryParse(lines[pos].Trim(), out int timeS);
            string eventStr = lines[pos + 1].Trim();
            string side     = lines[pos + 2].Trim();

            var ev = new JsonObject
            {
                ["time_s"] = timeS,
                ["event"]  = eventStr,
                ["side"]   = side,
            };

            if (isV2)
            {
                string comment = lines[pos + 3].Trim();
                if (!string.IsNullOrEmpty(comment)) ev["comment"] = comment;
            }

            eventsArr.Add(ev);
        }

        jo["events"] = eventsArr;
        return jo;
    }

    // ── .par ─────────────────────────────────────────────────────────────

    static string? IsotopeName(int idx) => idx switch
    {
        0 => "15N",
        1 => "13C",
        2 => "18O",
        7 => "34S",
        8 => "2H",
        _ => null
    };

    static JsonObject? ParsePar(string path)
    {
        if (!File.Exists(path)) return null;

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 20) return null;

        if (!int.TryParse(lines[14].Trim(), out int totalTimeS)) return null;
        if (!int.TryParse(lines[18].Trim(), out int nPeaks) || nPeaks <= 0) return null;
        // line[19] is n_peaks_total (usually == nPeaks; use nPeaks for iteration)

        var jo = new JsonObject
        {
            ["description"]  = lines[0].Trim(),
            ["total_time_s"] = totalTimeS,
        };

        int pos = 20; // first peak starts here (0-indexed line)
        var peaksArr = new JsonArray();

        for (int p = 0; p < nPeaks; p++)
        {
            if (pos + 19 >= lines.Length) break; // need at least 6 timing + 14 data

            // 6 timing lines
            int.TryParse(lines[pos++].Trim(), out int base1Start);
            int.TryParse(lines[pos++].Trim(), out int base1End);
            int.TryParse(lines[pos++].Trim(), out int intStart);
            int.TryParse(lines[pos++].Trim(), out int intEnd);
            int.TryParse(lines[pos++].Trim(), out int base2Start);
            int.TryParse(lines[pos++].Trim(), out int base2End);

            // 14 base per-peak data lines
            bool   active    = lines[pos++].Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
            string peakType  = lines[pos++].Trim();       // Sample / Reference
            int.TryParse(lines[pos++].Trim(), out int iso1Idx);  // isotope1_idx
            pos++;                                                 // gas source name (empty, skip)
            pos++;                                                 // unknown int
            pos++;                                                 // unknown int
            pos++;                                                 // gas_idx (skip)
            int.TryParse(lines[pos++].Trim(), out int iso2Idx);  // isotope2_idx
            string mode1     = lines[pos++].Trim();       // DeltaAir / DeltaPDB / DeltaCDT / …
            string mode2raw  = lines[pos++].Trim();       // same set, or "0" if no Isotope 2
            pos++;                                         // unknown int
            pos++;                                         // unknown int
            bool   linReg    = lines[pos++].Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
            int.TryParse(lines[pos++].Trim(), out int group);

            // Optional 15th data line: AutoDilute (True/False)
            bool? autoDilute = null;
            if (pos < lines.Length)
            {
                string next = lines[pos].Trim();
                if (next.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                    next.Equals("False", StringComparison.OrdinalIgnoreCase))
                {
                    autoDilute = next.Equals("True", StringComparison.OrdinalIgnoreCase);
                    pos++;
                }
            }

            string? iso1Name = IsotopeName(iso1Idx);
            string? iso2Name = IsotopeName(iso2Idx);

            var peak = new JsonObject
            {
                ["active"]            = active,
                ["type"]              = peakType,
                ["base1_start_s"]     = base1Start,
                ["base1_end_s"]       = base1End,
                ["integrate_start_s"] = intStart,
                ["integrate_end_s"]   = intEnd,
                ["base2_start_s"]     = base2Start,
                ["base2_end_s"]       = base2End,
                ["mode1"]             = mode1,
            };
            if (iso1Name is not null) peak["isotope1"] = iso1Name;
            if (iso2Name is not null) peak["isotope2"] = iso2Name;
            if (!string.IsNullOrEmpty(mode2raw) && mode2raw != "0") peak["mode2"] = mode2raw;
            peak["linear_regression"] = linReg;
            peak["group"]             = group;
            if (autoDilute.HasValue) peak["auto_dilute"] = autoDilute.Value;
            peaksArr.Add(peak);
        }

        // Skip 48 fixed global lines, then read n_peaks × 2 source/time pairs
        pos += 48;
        for (int p = 0; p < nPeaks && p < peaksArr.Count; p++)
        {
            int srcIdx = pos + p * 2;
            if (srcIdx + 1 >= lines.Length) break;
            string srcFile = lines[srcIdx].Trim();
            int.TryParse(lines[srcIdx + 1].Trim(), out int atTime);
            var old = (JsonObject)peaksArr[p]!;
            var peak = new JsonObject();
            if (!string.IsNullOrEmpty(srcFile)) peak["gas_species"] = srcFile;
            foreach (var kv in old) peak[kv.Key] = kv.Value?.DeepClone();
            peak["at_time_s"] = atTime;
            peaksArr[p] = peak;
        }

        jo["peaks"] = peaksArr;
        return jo;
    }

    // ── Results helpers ──────────────────────────────────────────────────

    static readonly HashSet<string> StructuralCols =
        new(StringComparer.OrdinalIgnoreCase) { "N", "Name", "Type", "Weight/Vol", "Status" };

    static bool IsDataColumn(string col) =>
        !string.IsNullOrEmpty(col) &&
        !col.All(c => c == '-') &&  // skip "-------" separator columns
        !StructuralCols.Contains(col);

    static JsonArray BuildDoubleArray(double[] values)
    {
        var arr = new JsonArray();
        foreach (double v in values) arr.Add(v);
        return arr;
    }

    static string? ExtractDatasetId(string name)
    {
        Match m = DatasetIdRx.Match(name);
        return m.Success ? m.Groups[1].Value : null;
    }
}
