// ImexpEntryDataReader.cs — parse AdditionalData and SampleMetadata
// from per-entry TFS253Plus/ directories in .imexp.zip notebooks.
//
// AdditionalData: ImhotepArrayList of AdditionalSampleDataSerializable items.
//   Each item has a string Key and a typed Data object:
//     "Linearity_Correction" → LinearityCorrectionResultSerializable
//     "Peak_Center"          → PeakCenterResultSerializable
//
// SampleMetadata: VersionableDictionary<string, object>.
//   Keys map to VersionableList<ValueTuple<Time, Fraction>> values,
//   e.g. "SampleDilution" → list of (time_s, fraction) pairs.

using System.Text.Json.Nodes;

static partial class ImexpReader
{
    // ── GUID-prefixed field key constants ────────────────────────────────────

    // ImhotepArrayList (TypeId: 266ce50d-f983-4468-b6b7-27ddc1564287)
    const string IAL_Items = "266ce50d-f983-4468-b6b7-27ddc1564287m_itemsFromSerialization";

    // AdditionalSampleDataSerializable (TypeId: e3aa035d-7a0b-4b67-b332-3cfe6070c857)
    const string ASD_Key  = "e3aa035d-7a0b-4b67-b332-3cfe6070c857Key";
    const string ASD_Data = "e3aa035d-7a0b-4b67-b332-3cfe6070c857Data";

    // LinearityCorrectionResultSerializable (TypeId: c811c53a-05d9-413f-84c8-c47df853741b)
    const string LCR_State        = "c811c53a-05d9-413f-84c8-c47df853741bLinearityCorrectionResultState";
    const string LCR_RatioResults = "c811c53a-05d9-413f-84c8-c47df853741bRatioResults";

    // LinearityCorrectionResultStateSerializable surrogate (TypeId: 820b5d94-54e1-4796-944b-7742defa08f6)
    const string LCRS_Value = "820b5d94-54e1-4796-944b-7742defa08f6_Value";

    // LinearityCorrectionRatioResultSerializable (TypeId: 479d130a-3bd9-45be-8b91-51923da1b25f)
    const string LCRR_Name     = "479d130a-3bd9-45be-8b91-51923da1b25fName";
    const string LCRR_ConvFactor = "479d130a-3bd9-45be-8b91-51923da1b25fConversionFactor";
    const string LCRR_LCFactor  = "479d130a-3bd9-45be-8b91-51923da1b25fLinearityCorrectionFactor";
    const string LCRR_IsDefault = "479d130a-3bd9-45be-8b91-51923da1b25fIsDefault";

    // PeakCenterResultSerializable (TypeId: 5774c788-e3f3-4b5b-9f79-068da21bf55a)
    const string PCR_Results = "5774c788-e3f3-4b5b-9f79-068da21bf55am_results";

    // PeakCenterResultDetailSerializable (TypeId: 642f745c-4f24-44ee-adc8-8a7bcf213fb3)
    const string PCRD_TuneBookName = "642f745c-4f24-44ee-adc8-8a7bcf213fb3TuneBookName";
    const string PCRD_HighVoltage  = "642f745c-4f24-44ee-adc8-8a7bcf213fb3HighVoltageDifferenceToInitialInstrumentModeHighVoltage";
    const string PCRD_State        = "642f745c-4f24-44ee-adc8-8a7bcf213fb3PeakCenterResultState";

    // PeakCenterResultStateSerializable surrogate (TypeId: 0c645a0e-2982-4d18-b946-c1f415f3afb0)
    const string PCRS_Value = "0c645a0e-2982-4d18-b946-c1f415f3afb0_Value";

    // VersionableList (TypeId: 23576c9d-2fd6-45f0-9a96-7728415a2695)
    const string VL_Items = "23576c9d-2fd6-45f0-9a96-7728415a2695items";

    // ── AdditionalData ───────────────────────────────────────────────────────

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
            {
                state = si == 1 ? "Successful" : si == 0 ? "Failed" : si.ToString();
            }
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
            obj["state"]        = state;
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

    // ── SampleMetadata ───────────────────────────────────────────────────────

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
