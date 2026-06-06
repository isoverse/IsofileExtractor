// ImexpReaders.cs  —  extract all data from Qtegra .imexp.zip notebooks
//
// Navigates the BinaryFormatter object graph (parsed by ImexpFiles.cs) using
// GUID-prefixed field keys derived from the Qtegra/TFS 253 Plus DLLs:
//   MultiDetector.Serialization.dll  (AmplifierSerializable, AmplifierChannel*)
//   GasIsotopeRatioBase.Common.dll   (GasConfigurationSerializable, CupConfigurationSerializable)
//   TFS253Plus.Core.dll              (StoredSettingsSerializable, StoredInstrumentModeDataSerializable,
//                                     SettingsFolderNameContainer)
//   Util.dll                         (VersionableDictionary, VersionableKeyedCollection,
//                                     VersionableList, ImhotepArrayList)
//   TFS253Plus.Contracts.dll         (SampleListColumnStorage, AdditionalSampleDataSerializable,
//                                     LinearityCorrection*, PeakCenter*)

using System.IO.Compression;
using System.Text.Json.Nodes;

static partial class ImexpReader
{
    // ── Public entry point ───────────────────────────────────────────────────

    public static void Read(string imexpPath, JsonObject root)
    {
        var entries = GatherEntries(imexpPath);
        var arr = new JsonArray();
        foreach (var (relPath, data) in entries)
        {
            string dir      = relPath.Contains('/') ? relPath[..relPath.LastIndexOf('/')] : "";
            string settings = dir.Contains('/') ? dir[(dir.LastIndexOf('/') + 1)..] : dir;

            var obj = new JsonObject { ["source"] = relPath, ["settings_id"] = settings };
            try
            {
                var objs = ParseStream(data);
                obj["amplifiers"]         = ExtractAmplifiers(objs);
                obj["gas_configurations"] = ExtractGasConfigs(objs);
            }
            catch (Exception ex) { obj["error"] = ex.Message; }
            arr.Add(obj);
        }
        root["settings"] = arr;
        ReadSampleLists(imexpPath, root);
        ReadTraces(imexpPath, root);
    }

    // ════════════════════════════════════════════════════════════════════════
    // GUID-prefixed field key constants
    // Each key = TypeId GUID (no braces, lower-case) + field name, no separator.
    // ════════════════════════════════════════════════════════════════════════

    // StoredSettingsSerializable (TypeId: 4147e638)
    const string SS_ChannelSettings     = "4147e638-93e4-4fcb-a361-494646ba0152ChannelSettings";
    const string SS_InstrumentModesData = "4147e638-93e4-4fcb-a361-494646ba0152m_instrumentModesData";

    // StoredInstrumentModeDataSerializable (TypeId: 0e07d289)
    const string SIMD_GasConfigurations = "0e07d289-5677-4ff1-888a-33f0796778dcGasConfigurations";

    // AmplifierChannelCollectionSerializable (TypeId: afef1e4a)
    const string ACC_Version            = "afef1e4a-d8fb-4db4-9e35-ed49c623b52a_Version";
    const string ACC_Channels           = "afef1e4a-d8fb-4db4-9e35-ed49c623b52am_channels";
    const string ACC_LegacyAmplifiers   = "fee11718-4b62-4595-81dd-595d5d49cfd2m_itemsFromSerialization";

    // AmplifierChannelSerializable (TypeId: ac85586c)
    const string AC_ChannelNumber       = "ac85586c-3838-45cb-9395-8f730e1f415dChannelNumber";
    const string AC_Amplifiers          = "ac85586c-3838-45cb-9395-8f730e1f415dAmplifiers";

    // AmplifierSerializable (TypeId: 58b1614a) — v3/v4 field names
    const string AMP_Identifier         = "58b1614a-9377-459d-a7af-0d9726951f5eIdentifier";
    const string AMP_DisplayName        = "58b1614a-9377-459d-a7af-0d9726951f5eDisplayName";
    const string AMP_MaxVoltage         = "58b1614a-9377-459d-a7af-0d9726951f5eMaximumVoltage";
    const string AMP_MinVoltage         = "58b1614a-9377-459d-a7af-0d9726951f5eMinimumVoltage";
    const string AMP_ResistorValue      = "58b1614a-9377-459d-a7af-0d9726951f5eResistorValue";
    const string AMP_IsHighGain         = "58b1614a-9377-459d-a7af-0d9726951f5eIsHighGain";
    // v1/v2 legacy names
    const string AMP_LegacyIdentifier   = "58b1614a-9377-459d-a7af-0d9726951f5em_identifier";
    const string AMP_LegacyDisplayName  = "58b1614a-9377-459d-a7af-0d9726951f5em_displayName";
    const string AMP_LegacyMaxVoltage   = "58b1614a-9377-459d-a7af-0d9726951f5em_maximumVoltage";
    const string AMP_LegacyMinVoltage   = "58b1614a-9377-459d-a7af-0d9726951f5em_minimumVoltage";
    const string AMP_LegacyResistorValue = "58b1614a-9377-459d-a7af-0d9726951f5em_resistorValue";

    // VersionableDictionary (TypeId: 02ee5699)
    const string VD_Pairs               = "02ee5699-90a0-4f6a-8c06-843deec9156fpairs";

    // VersionableKeyedCollection (TypeId: e2307afa)
    const string VKC_Items              = "e2307afa-49db-4c6e-81c2-837c7e4635e4Items";

    // GasConfigurationSerializable (TypeId: fb7fe803)
    const string GC_DisplayName         = "fb7fe803-326a-4446-81ac-452af32aa079DisplayName";
    const string GC_CalibrationMass     = "fb7fe803-326a-4446-81ac-452af32aa079CalibrationMass";
    const string GC_CupCollection       = "fb7fe803-326a-4446-81ac-452af32aa079m_cupConfigurationCollection";

    // CupConfigurationSerializable (TypeId: cdcecc54)
    const string CUP_DisplayName        = "cdcecc54-442e-4469-809f-91e96b7870f1DisplayName";
    const string CUP_MoleculeOrMass     = "cdcecc54-442e-4469-809f-91e96b7870f1MoleculeOrMass";
    const string CUP_IsUsable           = "cdcecc54-442e-4469-809f-91e96b7870f1IsUsable";
    const string CUP_AmplifierIdentifier = "cdcecc54-442e-4469-809f-91e96b7870f1AmplifierIdentifier";

    // MoleculeOrMassSerializable (TypeId: 94207bd6)
    const string MOM_Mass               = "94207bd6-cb71-4f20-a1e0-2764077e5b00Mass";

    // ImhotepArrayList (TypeId: 266ce50d)
    const string IAL_Items              = "266ce50d-f983-4468-b6b7-27ddc1564287m_itemsFromSerialization";

    // VersionableList (TypeId: 23576c9d)
    const string VL_Items               = "23576c9d-2fd6-45f0-9a96-7728415a2695items";

    // SampleListColumnStorage (TypeId: dd22eb03)
    const string SLC_Name               = "dd22eb03-0c22-49a4-8a7c-79f0300ecc7bm_name";
    const string SLC_Rows               = "dd22eb03-0c22-49a4-8a7c-79f0300ecc7bm_rows";

    // SettingsFolderNameContainer (TypeId: 1c6dfa86)
    const string SFC_FolderName         = "1c6dfa86-5ce9-4939-b7a2-6b568b5c57fbm_folderName";

    // AdditionalSampleDataSerializable (TypeId: e3aa035d)
    const string ASD_Key                = "e3aa035d-7a0b-4b67-b332-3cfe6070c857Key";
    const string ASD_Data               = "e3aa035d-7a0b-4b67-b332-3cfe6070c857Data";

    // LinearityCorrectionResultSerializable (TypeId: c811c53a)
    const string LCR_State              = "c811c53a-05d9-413f-84c8-c47df853741bLinearityCorrectionResultState";
    const string LCR_RatioResults       = "c811c53a-05d9-413f-84c8-c47df853741bRatioResults";

    // LinearityCorrectionResultStateSerializable surrogate (TypeId: 820b5d94)
    const string LCRS_Value             = "820b5d94-54e1-4796-944b-7742defa08f6_Value";

    // LinearityCorrectionRatioResultSerializable (TypeId: 479d130a)
    const string LCRR_Name              = "479d130a-3bd9-45be-8b91-51923da1b25fName";
    const string LCRR_ConvFactor        = "479d130a-3bd9-45be-8b91-51923da1b25fConversionFactor";
    const string LCRR_LCFactor          = "479d130a-3bd9-45be-8b91-51923da1b25fLinearityCorrectionFactor";
    const string LCRR_IsDefault         = "479d130a-3bd9-45be-8b91-51923da1b25fIsDefault";

    // PeakCenterResultSerializable (TypeId: 5774c788)
    const string PCR_Results            = "5774c788-e3f3-4b5b-9f79-068da21bf55am_results";

    // PeakCenterResultDetailSerializable (TypeId: 642f745c)
    const string PCRD_TuneBookName      = "642f745c-4f24-44ee-adc8-8a7bcf213fb3TuneBookName";
    const string PCRD_HighVoltage       = "642f745c-4f24-44ee-adc8-8a7bcf213fb3HighVoltageDifferenceToInitialInstrumentModeHighVoltage";
    const string PCRD_State             = "642f745c-4f24-44ee-adc8-8a7bcf213fb3PeakCenterResultState";

    // PeakCenterResultStateSerializable surrogate (TypeId: 0c645a0e)
    const string PCRS_Value             = "0c645a0e-2982-4d18-b946-c1f415f3afb0_Value";

    // ════════════════════════════════════════════════════════════════════════
    // Settings: StoredSettings.bin
    // ════════════════════════════════════════════════════════════════════════

    static List<(string RelPath, byte[] Data)> GatherEntries(string path)
    {
        var list = new List<(string, byte[])>();
        if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            using var zip = ZipFile.OpenRead(path);
            foreach (var e in zip.Entries.OrderBy(e => e.FullName, StringComparer.Ordinal))
            {
                if (!string.Equals(Path.GetFileName(e.FullName), "StoredSettings.bin",
                        StringComparison.OrdinalIgnoreCase)) continue;
                using var ms = new MemoryStream((int)e.Length);
                using var s = e.Open();
                s.CopyTo(ms);
                list.Add((e.FullName, ms.ToArray()));
            }
        }
        else
        {
            foreach (var f in Directory.GetFiles(path, "StoredSettings.bin",
                         SearchOption.AllDirectories).Order())
                list.Add((Path.GetRelativePath(path, f).Replace('\\', '/'), File.ReadAllBytes(f)));
        }
        return list;
    }

    // StoredSettings → ChannelSettings → AmplifierChannelCollection
    //   → m_channels[] → AmplifierChannel → Amplifiers[] → AmplifierSerializable

    static JsonArray ExtractAmplifiers(Dictionary<int, BfObj> objs)
    {
        var result = new JsonArray();
        if (!objs.TryGetValue(1, out var root)) return result;

        var channelColl = Resolve(root.Fields.GetValueOrDefault(SS_ChannelSettings), objs);
        if (channelColl is null) return result;

        bool isV2 = channelColl.Fields.TryGetValue(ACC_Version, out var vObj) && vObj is int v && v >= 2;

        if (isV2 && channelColl.Fields.ContainsKey(ACC_Channels))
        {
            foreach (var channel in GetObjItems(channelColl.Fields.GetValueOrDefault(ACC_Channels), objs))
            {
                int channelNum = channel.Fields.TryGetValue(AC_ChannelNumber, out var cn) && cn is int c ? c : -1;
                foreach (var amp in GetObjItems(channel.Fields.GetValueOrDefault(AC_Amplifiers), objs))
                    result.Add(BuildAmplifier(amp, objs, channelNum));
            }
        }
        else if (channelColl.Fields.ContainsKey(ACC_LegacyAmplifiers))
        {
            foreach (var amp in GetObjItems(channelColl.Fields.GetValueOrDefault(ACC_LegacyAmplifiers), objs))
                result.Add(BuildAmplifier(amp, objs, null));
        }

        return result;
    }

    static JsonObject BuildAmplifier(BfObj amp, Dictionary<int, BfObj> objs, int? channelNum)
    {
        var f = amp.Fields;
        string? id  = GetString(f.GetValueOrDefault(AMP_Identifier),  objs)
                   ?? GetString(f.GetValueOrDefault(AMP_LegacyIdentifier), objs);
        string? dn  = GetString(f.GetValueOrDefault(AMP_DisplayName), objs)
                   ?? GetString(f.GetValueOrDefault(AMP_LegacyDisplayName), objs);
        double? maxV = GetQuantityDouble(f.GetValueOrDefault(AMP_MaxVoltage),  objs)
                    ?? GetQuantityDouble(f.GetValueOrDefault(AMP_LegacyMaxVoltage), objs);
        double? minV = GetQuantityDouble(f.GetValueOrDefault(AMP_MinVoltage),  objs)
                    ?? GetQuantityDouble(f.GetValueOrDefault(AMP_LegacyMinVoltage), objs);
        double? res  = GetQuantityDouble(f.GetValueOrDefault(AMP_ResistorValue), objs)
                    ?? GetQuantityDouble(f.GetValueOrDefault(AMP_LegacyResistorValue), objs);
        bool? hg = f.TryGetValue(AMP_IsHighGain, out var hgv) && hgv is bool b ? b : null;

        return new JsonObject
        {
            ["identifier"]     = id,
            ["display_name"]   = dn,
            ["channel_number"] = JsonValue.Create(channelNum),
            ["max_voltage_v"]  = maxV,
            ["min_voltage_v"]  = minV,
            ["resistor_ohm"]   = res,
            ["is_high_gain"]   = hg,
        };
    }

    // StoredSettings → m_instrumentModesData (VersionableDictionary)
    //   → pairs[] → KVP.value → StoredInstrumentModeData
    //   → GasConfigurations → Items[] → GasConfigurationSerializable
    //     → m_cupConfigurationCollection → pairs[] → CupConfigurationSerializable

    static JsonArray ExtractGasConfigs(Dictionary<int, BfObj> objs)
    {
        var result = new JsonArray();
        if (!objs.TryGetValue(1, out var root)) return result;

        var instrModesDict = Resolve(root.Fields.GetValueOrDefault(SS_InstrumentModesData), objs);
        if (instrModesDict is null) return result;

        foreach (var kvPair in GetObjItems(instrModesDict.Fields.GetValueOrDefault(VD_Pairs), objs))
        {
            // KeyValuePair system type uses plain "key"/"value" field names (no GUID prefix)
            var instrModeData = Resolve(kvPair.Fields.GetValueOrDefault("value"), objs);
            if (instrModeData is null) continue;

            var gasConfigColl = Resolve(instrModeData.Fields.GetValueOrDefault(SIMD_GasConfigurations), objs);
            if (gasConfigColl is null) continue;

            foreach (var gasConfig in GetObjItems(gasConfigColl.Fields.GetValueOrDefault(VKC_Items), objs))
                result.Add(BuildGasConfig(gasConfig, objs));
        }
        return result;
    }

    static JsonObject BuildGasConfig(BfObj gasConfig, Dictionary<int, BfObj> objs)
    {
        string? name = GetString(gasConfig.Fields.GetValueOrDefault(GC_DisplayName), objs);
        double? mass = GetQuantityDouble(gasConfig.Fields.GetValueOrDefault(GC_CalibrationMass), objs);

        var cups = new JsonArray();
        var cupDict = Resolve(gasConfig.Fields.GetValueOrDefault(GC_CupCollection), objs);
        if (cupDict is not null)
        {
            foreach (var kvPair in GetObjItems(cupDict.Fields.GetValueOrDefault(VD_Pairs), objs))
            {
                var cupObj = Resolve(kvPair.Fields.GetValueOrDefault("value"), objs);
                if (cupObj is null) continue;

                bool isUsable = cupObj.Fields.TryGetValue(CUP_IsUsable, out var u) && u is true;
                if (!isUsable) continue;

                string? cupName = GetString(cupObj.Fields.GetValueOrDefault(CUP_DisplayName), objs);
                string? ampId   = GetString(cupObj.Fields.GetValueOrDefault(CUP_AmplifierIdentifier), objs);

                double? cupMass = null;
                var momObj = Resolve(cupObj.Fields.GetValueOrDefault(CUP_MoleculeOrMass), objs);
                if (momObj is not null)
                    cupMass = GetQuantityDouble(momObj.Fields.GetValueOrDefault(MOM_Mass), objs);

                cups.Add(new JsonObject
                {
                    ["display_name"]         = cupName,
                    ["mass"]                 = cupMass,
                    ["amplifier_identifier"] = ampId,
                });
            }
        }

        return new JsonObject
        {
            ["display_name"]       = name,
            ["calibration_mass"]   = mass,
            ["cup_configurations"] = cups,
        };
    }

    // ════════════════════════════════════════════════════════════════════════
    // Sample lists: TFS253Plus.samples / VogonCalc.samples
    // ════════════════════════════════════════════════════════════════════════

    static void ReadSampleLists(string imexpPath, JsonObject root)
    {
        var files = GatherSampleListFiles(imexpPath);
        if (files.Count == 0) return;
        var arr = new JsonArray();
        foreach (var (source, data) in files)
        {
            var obj = new JsonObject { ["source"] = source };
            try   { obj["rows"] = ParseSampleList(data); }
            catch (Exception ex) { obj["error"] = ex.Message; }
            arr.Add(obj);
        }
        root["sample_lists"] = arr;
    }

    // Collect all .samples files from SampleList/ dirs, then keep only the newest
    // copy of each filename (newest = lexicographically latest top-level directory).
    static List<(string Source, byte[] Data)> GatherSampleListFiles(string path)
    {
        var candidates = new Dictionary<string, List<(string RelPath, byte[] Data)>>(
            StringComparer.OrdinalIgnoreCase);

        if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            using var zip = ZipFile.OpenRead(path);
            foreach (var e in zip.Entries)
            {
                if (!e.FullName.Contains("/SampleList/")) continue;
                string name = Path.GetFileName(e.FullName);
                if (!name.EndsWith(".samples", StringComparison.OrdinalIgnoreCase)) continue;
                if (!candidates.TryGetValue(name, out var cl)) candidates[name] = cl = new();
                cl.Add((e.FullName, LoadZipEntry(e)));
            }
        }
        else
        {
            foreach (var f in Directory.GetFiles(path, "*.samples", SearchOption.AllDirectories)
                         .Where(f => string.Equals(
                             Path.GetFileName(Path.GetDirectoryName(f)), "SampleList",
                             StringComparison.OrdinalIgnoreCase)))
            {
                string name    = Path.GetFileName(f);
                string relPath = Path.GetRelativePath(path, f).Replace('\\', '/');
                if (!candidates.TryGetValue(name, out var cl)) candidates[name] = cl = new();
                cl.Add((relPath, File.ReadAllBytes(f)));
            }
        }

        static string TopDir(string relPath)
        {
            int slash = relPath.IndexOf('/');
            return slash >= 0 ? relPath[..slash] : relPath;
        }

        var result = new List<(string, byte[])>();
        foreach (var cl in candidates.Values)
        {
            var best = cl.MaxBy(c => TopDir(c.RelPath), StringComparer.Ordinal)!;
            result.Add((best.RelPath, best.Data));
        }
        return result;
    }

    // Columns that are present in every sample list and kept as flat snake_case fields on the row.
    // All other columns go into "params" with their original PascalCase name.
    static readonly HashSet<string> StandardColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "sample_line_region", "sample_line_definition_id", "guid",
        "mark_to_pause", "identifier", "comment", "run_id",
    };

    static JsonArray ParseSampleList(byte[] d)
    {
        var objs = ParseStream(d);
        if (!objs.TryGetValue(1, out var root)) return new JsonArray();

        // Each column: (normalized snake_case key, original PascalCase label, row values)
        var columns = new List<(string Key, string Label, object?[] Rows, string? Type)>();

        foreach (var colObj in GetObjItems(root.Fields.GetValueOrDefault(VL_Items), objs))
        {
            string? rawName = GetString(colObj.Fields.GetValueOrDefault(SLC_Name), objs);
            if (rawName is null) continue;

            var rowsAL   = Resolve(colObj.Fields.GetValueOrDefault(SLC_Rows), objs);
            var rawItems = GetItems(rowsAL?.Fields.GetValueOrDefault(IAL_Items), objs)
                           ?? Array.Empty<object?>();

            string label = StripColumnPrefix(rawName);
            string key   = ToSnakeCase(label);
            string? type = GetParamType(rawItems, objs);
            columns.Add((key, label, rawItems, type));
        }

        if (columns.Count == 0) return new JsonArray();

        int nRows = columns.Max(c => c.Rows.Length);
        var result = new JsonArray();
        for (int row = 0; row < nRows; row++)
        {
            var rowObj    = new JsonObject();
            var paramsArr = new JsonArray();
            foreach (var (key, label, rows, type) in columns)
            {
                object? val = row < rows.Length ? rows[row] : null;
                if (StandardColumns.Contains(key))
                    rowObj[key] = BfValueToJson(val, objs);
                else
                {
                    var param = new JsonObject { ["label"] = label };
                    if (type is not null) param["type"] = type;
                    param["value"] = BfValueToJson(val, objs);
                    paramsArr.Add(param);
                }
            }
            if (paramsArr.Count > 0) rowObj["params"] = paramsArr;
            result.Add(rowObj);
        }
        return result;
    }

    // Returns the type name of the first non-null value in a column's row array.
    static string? GetParamType(object?[] rows, Dictionary<int, BfObj> objs)
    {
        foreach (var item in rows)
        {
            object? val = item is BfRef r && objs.TryGetValue(r.Id, out var o) ? o : item;
            if (val is null) continue;
            return val switch
            {
                bool                                   => "bool",
                int                                    => "int",
                long                                   => "int",
                double                                 => "double",
                string                                 => "string",
                BfObj b when b.ClassName == "__string" => "string",
                BfObj b when b.ClassName == "System.Guid" => "guid",
                _                                      => null,
            };
        }
        return null;
    }

    // Strip plugin prefix (e.g. "TFS253Plus.RunId" → "RunId").
    static string StripColumnPrefix(string name)
    {
        int dot = name.LastIndexOf('.');
        return dot >= 0 ? name[(dot + 1)..] : name;
    }

    // PascalCase/camelCase → snake_case; spaces become underscores.
    static string ToSnakeCase(string name)
    {
        var sb = new System.Text.StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (c == ' ')
                sb.Append('_');
            else if (char.IsUpper(c) && i > 0 && char.IsLower(name[i - 1]))
                sb.Append('_').Append(char.ToLowerInvariant(c));
            else
                sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    static string NormalizeColumnName(string name) => ToSnakeCase(StripColumnPrefix(name));

    static JsonNode? BfValueToJson(object? val, Dictionary<int, BfObj> objs)
    {
        if (val is BfRef r)
            val = objs.TryGetValue(r.Id, out var o) ? (object?)o : null;

        return val switch
        {
            null         => null,
            bool   b     => JsonValue.Create(b),
            int    i     => JsonValue.Create(i),
            long   l     => JsonValue.Create(l),
            double dbl   => JsonValue.Create(dbl),
            string s     => JsonValue.Create(StripGuidPrefix(s)),
            BfObj  bfo   => BfObjToJson(bfo, objs),
            _            => null,
        };
    }

    static JsonNode? BfObjToJson(BfObj obj, Dictionary<int, BfObj> objs)
    {
        if (obj.ClassName == "__string")
        {
            string? s = obj.Fields.GetValueOrDefault("__value") as string;
            return s is not null ? JsonValue.Create(StripGuidPrefix(s)) : null;
        }
        if (obj.ClassName == "System.Guid")
            return JsonValue.Create(BfGuidToString(obj));
        return null;
    }

    static string StripGuidPrefix(string s) =>
        s.Length == 37 && s[0] == '$' ? s[1..] : s;

    static string BfGuidToString(BfObj obj)
    {
        int  a  = GetField<int>(obj, "_a");
        int  b  = GetField<int>(obj, "_b");
        int  c  = GetField<int>(obj, "_c");
        byte dd = (byte)GetField<int>(obj, "_d");
        byte e  = (byte)GetField<int>(obj, "_e");
        byte f  = (byte)GetField<int>(obj, "_f");
        byte g  = (byte)GetField<int>(obj, "_g");
        byte h  = (byte)GetField<int>(obj, "_h");
        byte ii = (byte)GetField<int>(obj, "_i");
        byte j  = (byte)GetField<int>(obj, "_j");
        byte k  = (byte)GetField<int>(obj, "_k");
        return new Guid(a, (short)b, (short)c, dd, e, f, g, h, ii, j, k).ToString();
    }

    static T GetField<T>(BfObj obj, string key) =>
        obj.Fields.TryGetValue(key, out var v) && v is T t ? t : default!;

    // ════════════════════════════════════════════════════════════════════════
    // Traces: MeasureData.bin + MeasureDataIndexLines.bin
    // ════════════════════════════════════════════════════════════════════════

    static void ReadTraces(string imexpPath, JsonObject root)
    {
        var pairs = GatherTracePairs(imexpPath);
        var arr = new JsonArray();
        foreach (var (source, idxBytes, mdBytes, csfnBytes, adataBytes, smetaBytes) in pairs)
        {
            string? entryId = null, entryType = null, sessionTime = null;
            var parts = source.Split('/');
            if (parts.Length > 0)
                sessionTime = ParseSessionTime(parts[0]);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].StartsWith("Entry_", StringComparison.OrdinalIgnoreCase))
                {
                    entryId   = parts[i]["Entry_".Length..];
                    entryType = i + 1 < parts.Length ? parts[i + 1] : null;
                    break;
                }
            }
            var obj = new JsonObject { ["source"] = source };
            if (entryId     is not null) obj["guid"]         = entryId;
            if (entryType   is not null) obj["type"]        = entryType;
            if (sessionTime is not null) obj["session_time"] = sessionTime;
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

    static List<(string Source, byte[] IdxBytes, byte[] MdBytes, byte[] CsfnBytes, byte[] ADataBytes, byte[] SMetaBytes)>
        GatherTracePairs(string path)
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

    static string? ParseSessionTime(string dirName)
    {
        // Format: YYYYMMDD-HHMMSS-MMM  (e.g. 20250312-094429-745)
        if (System.DateTime.TryParseExact(dirName, "yyyyMMdd-HHmmss-fff",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.fff");
        return null;
    }

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
        if (segBytes < 14) return;

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
            if (hasAnalog[ch])  p += 8;
            hasCounter[ch] = d[p++] != 0;
            if (hasCounter[ch]) p += 8;
        }

        int tpBytes = 0;
        for (int ch = 0; ch < nChannels; ch++)
            tpBytes += 2 + 8 + 1 + (hasAnalog[ch] ? 8 : 0) + 1 + (hasCounter[ch] ? 8 : 0);
        int tsSize = 14 + tpBytes;

        int nPoints = (int)(segBytes / tsSize);
        if (nPoints == 0) return;

        // Per-channel intensity offset within a TraceSet (0-based from TraceSet start).
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

        var rawTicks    = new long[nPoints];
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

    // ════════════════════════════════════════════════════════════════════════
    // AdditionalData and SampleMetadata
    // ════════════════════════════════════════════════════════════════════════

    static JsonNode? ParseAdditionalData(byte[] d)
    {
        if (d.Length == 0) return null;
        var objs = ParseStream(d);
        if (!objs.TryGetValue(1, out var root)) return null;

        var result = new JsonObject();
        foreach (var item in GetObjItems(root.Fields.GetValueOrDefault(IAL_Items), objs))
        {
            string? key  = GetString(item.Fields.GetValueOrDefault(ASD_Key),  objs);
            var     data = Resolve(item.Fields.GetValueOrDefault(ASD_Data), objs);
            if (key is null) continue;
            var itemObj = new JsonObject();
            if (data is not null)
                AppendAdditionalDataPayload(itemObj, data, objs);
            result[key.ToLowerInvariant()] = itemObj;
        }
        return result.Count > 0 ? (JsonNode)result : null;
    }

    static void AppendAdditionalDataPayload(JsonObject obj, BfObj data, Dictionary<int, BfObj> objs)
    {
        if (data.Fields.ContainsKey(LCR_RatioResults))
        {
            // LinearityCorrectionResultSerializable
            string? state = null;
            var stateField = data.Fields.GetValueOrDefault(LCR_State);
            if (stateField is int si)
                state = si == 1 ? "Successful" : si == 0 ? "Failed" : si.ToString();
            else
            {
                var stateObj = Resolve(stateField, objs);
                if (stateObj is not null)
                    state = GetString(stateObj.Fields.GetValueOrDefault(LCRS_Value), objs);
            }

            var ratioArr = new JsonArray();
            foreach (var ratio in GetObjItems(data.Fields.GetValueOrDefault(LCR_RatioResults), objs))
            {
                string? name = GetString(ratio.Fields.GetValueOrDefault(LCRR_Name), objs);
                double? conv = ratio.Fields.TryGetValue(LCRR_ConvFactor, out var cfv) && cfv is double cfd ? cfd : null;
                double? lc   = ratio.Fields.TryGetValue(LCRR_LCFactor,   out var lfv) && lfv is double lfd ? lfd : null;
                bool?   isDef = ratio.Fields.TryGetValue(LCRR_IsDefault,  out var idv) && idv is bool   idb ? idb : null;
                ratioArr.Add(new JsonObject
                {
                    ["name"]                        = name,
                    ["conversion_factor"]           = conv,
                    ["linearity_correction_factor"] = lc,
                    ["is_default"]                  = isDef,
                });
            }
            obj["state"]         = state;
            obj["ratio_results"] = ratioArr;
        }
        else if (data.Fields.ContainsKey(PCR_Results))
        {
            // PeakCenterResultSerializable
            var resultsArr = new JsonArray();
            foreach (var detail in GetObjItems(data.Fields.GetValueOrDefault(PCR_Results), objs))
            {
                string? tbName = GetString(detail.Fields.GetValueOrDefault(PCRD_TuneBookName), objs);
                double? hvDiff = GetQuantityDouble(detail.Fields.GetValueOrDefault(PCRD_HighVoltage), objs);
                string? pState = null;
                var stateObj   = Resolve(detail.Fields.GetValueOrDefault(PCRD_State), objs);
                if (stateObj is not null)
                    pState = GetString(stateObj.Fields.GetValueOrDefault(PCRS_Value), objs);
                resultsArr.Add(new JsonObject
                {
                    ["tune_book_name"]      = tbName,
                    ["high_voltage_diff_v"] = hvDiff,
                    ["state"]               = pState,
                });
            }
            obj["results"] = resultsArr;
        }
    }

    static JsonNode? ParseSampleMetadata(byte[] d)
    {
        if (d.Length == 0) return null;
        var objs = ParseStream(d);
        if (!objs.TryGetValue(1, out var root)) return null;

        var result = new JsonObject();
        foreach (var kvPair in GetObjItems(root.Fields.GetValueOrDefault(VD_Pairs), objs))
        {
            string? key   = GetString(kvPair.Fields.GetValueOrDefault("key"), objs);
            if (key is null) continue;
            var    valObj = Resolve(kvPair.Fields.GetValueOrDefault("value"), objs);
            if (valObj is null) continue;

            // VersionableList<ValueTuple<Time, Fraction>>
            var tupleArr = new JsonArray();
            foreach (var tuple in GetObjItems(valObj.Fields.GetValueOrDefault(VL_Items), objs))
            {
                double? time = GetQuantityDouble(tuple.Fields.GetValueOrDefault("Item1"), objs);
                double? frac = GetQuantityDouble(tuple.Fields.GetValueOrDefault("Item2"), objs);
                tupleArr.Add(new JsonObject { ["time_s"] = time, ["fraction"] = frac });
            }
            if (tupleArr.Count > 0) result[key] = tupleArr;
        }
        return result.Count > 0 ? (JsonNode)result : null;
    }
}
