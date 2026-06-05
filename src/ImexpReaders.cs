// ImexpReader.cs  —  extract instrument settings from Qtegra .imexp.zip notebooks
//
// Navigates the BinaryFormatter object graph (parsed by ImexpFiles.cs) using
// GUID-prefixed field keys derived from the Qtegra/TFS 253 Plus DLLs:
//   MultiDetector.Serialization.dll  (AmplifierSerializable, AmplifierChannel*)
//   GasIsotopeRatioBase.Common.dll   (GasConfigurationSerializable, CupConfigurationSerializable)
//   TFS253Plus.Core.dll              (StoredSettingsSerializable, StoredInstrumentModeDataSerializable)
//   Util.dll                         (VersionableDictionary, VersionableKeyedCollection)

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

    // ── Entry gathering ──────────────────────────────────────────────────────

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

    // ── GUID-prefixed field key constants ────────────────────────────────────
    // Each key = TypeId GUID (no braces, lower-case) + field name, no separator.

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

    // ── Amplifier extraction ─────────────────────────────────────────────────
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

    // ── Gas configuration extraction ─────────────────────────────────────────
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
            ["display_name"]     = name,
            ["calibration_mass"] = mass,
            ["cup_configurations"] = cups,
        };
    }
}
