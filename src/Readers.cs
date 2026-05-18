using System.Text;
using System.Text.Json.Nodes;

namespace IsodatReader;

// ---------------------------------------------------------------------------
// All class reader functions for isodat binary files (.dxf, .scn).
//
// Naming conventions:
//   - ReadCXxx()        called directly for inline parent-class serialization
//   - Dispatch(ar)      called when object was written via WriteObject()
//                       (reads CRuntimeClass header first, then dispatches)
//   - "p_c_xxx" keys    parent class data embedded inline
//   - "c_xxx" keys      member objects written via WriteObject
// ---------------------------------------------------------------------------
static class Readers
{
    // =======================================================================
    // Registry: class name → reader function
    // =======================================================================

    static readonly Dictionary<string, Func<IsodatArchive, JsonObject>> _registry;

    static Readers()
    {
        _registry = new(StringComparer.Ordinal)
        {
            // --- CFileHeader ---
            ["CFileHeader"]                          = ReadCFileHeader,

            // --- CData chain ---
            ["CData"]                                = ReadCData,
            ["CCalibrationPoint"]                    = ReadCCalibrationPoint,
            ["CMolecule"]                            = ReadCMolecule,
            ["CTimeObject"]                          = ReadCTimeObject,
            ["CISLScriptMessageData"]                = ReadCISLScriptMessageData,
            ["CComponent"]                           = ReadCComponent,
            ["CEvalIntegrationUnitHWInfo"]           = ReadCEvalIntegrationUnitHWInfo,
            ["CTraceSettings"]                       = ReadCTraceSettings,
            ["CEvalDataItemTransferPart"]             = ReadCEvalDataItemTransferPart,
            ["CPeakDataItem"]                        = ReadCPeakDataItem,
            ["CWinColor"]                            = ReadCWinColor,
            ["CTraceLinCol"]                         = ReadCTraceLinCol,
            ["CGridColors"]                          = ReadCGridColors,
            ["CAxisPara"]                            = ReadCAxisPara,
            ["CH3FactorResult"]                      = ReadCH3FactorResult,
            ["CApplicationData"]                     = ReadCApplicationData,
            ["CResultForGas"]                        = ReadCResultForGas,
            ["CPeakFindParameter"]                   = ReadCPeakFindParameter,
            ["CMRI_DilutionList"]                    = ReadCMRI_DilutionList,

            // --- CSimple chain ---
            ["CSimple"]                              = ReadCSimple,
            ["CStr"]                                 = ReadCStr,
            ["CDword"]                               = ReadCDword,
            ["CPeakCenterOffset"]                    = ReadCDword,
            ["CBinary"]                              = ReadCBinary,

            // --- CBlockData chain ---
            ["CBlockData"]                           = ReadCBlockData,
            ["CAcquistionBaseBlockData"]             = ReadCBlockData,
            ["CPort"]                                = ReadCBlockData,
            ["CDataIndex"]                           = ReadCDataIndex,
            ["CCalibration"]                         = ReadCCalibration,
            ["CVisualisationData"]                   = ReadCVisualisationData,
            ["CGasConfiguration"]                    = ReadCGasConfiguration,
            ["CMeasurmentInfos"]                     = ReadCMeasurmentInfos,
            ["CMeasurmentErrors"]                    = ReadCMeasurmentErrors,
            ["CPlotSettings"]                        = ReadCPlotSettings,
            ["CWinSettings"]                         = ReadCWinSettings,
            ["CViewColors"]                          = ReadCViewColors,
            ["CGasSettings"]                         = ReadCGasSettings,
            ["CPkDataItemList"]                      = ReadCPkDataItemList,
            ["CAllMoleculeWeights"]                  = ReadCAllMoleculeWeights,
            ["CMethod"]                              = ReadCMethod,
            ["CConfiguration"]                       = ReadCConfiguration,
            ["CComponentList"]                       = ReadCComponentList,
            ["CParsedEvaluationStringArray"]         = ReadCParsedEvaluationStringArray,
            ["CResultArray"]                         = ReadCResultArray,
            ["CActionScript"]                        = ReadCActionScript,
            ["CGCPeakList"]                          = ReadCGCPeakList,
            ["CVisualisationDialogNamesBlockData"]   = ReadCVisualisationDialogNamesBlockData,
            ["CEvalDataItemListTransferPart"]        = ReadCEvalDataItemListTransferPart,
            ["CEvalIntegrationUnitHWInfoStore"]      = ReadCEvalDataItemListTransferPart,
            ["CEvalIntegrationUnitHWInfoList"]       = ReadCEvalDataItemListTransferPart,

            // --- CDevice chain ---
            ["CDevice"]                              = ReadCDevice,
            ["CActiveDevice"]                        = ReadCActiveDevice,
            ["CActivePort"]                          = ReadCActivePort,
            ["CMsDevice"]                            = ReadCMsDevice,
            ["CGenericGcDevice"]                     = ReadCGenericGcDevice,
            ["CFlashEA_Device"]                      = ReadCFlashEA_Device,
            ["CConFloDevice"]                        = ReadCActiveDevice,
            ["CMultiReferenceDevice"]                = ReadCActiveDevice,
            ["CUserDevice"]                          = ReadCActiveDevice,

            // --- IsoGCEvalData / CEvalDataStorage chain ---
            ["IsoGCEvalData"]                        = ReadIsoGCEvalData,
            ["CGCData"]                              = ReadCGCData,
            ["CRawData"]                             = ReadCRawData,
            ["CEvalDataStorage"]                     = ReadCEvalDataStorage,
            ["CEvalFakeData"]                        = ReadCEvalFakeData,
            ["CEvalGCData"]                          = ReadCEvalGCData,

            // --- CBasicInterface chain (= CData) ---
            ["CBasicInterface"]                      = ReadCData,
            ["CGasConfPart"]                         = ReadCData,
            ["CFinniganInterface"]                   = ReadCFinniganInterface,
            ["CGpibInterface"]                       = ReadCGpibInterface,

            // --- CTransferPart chain ---
            ["CTransferPart"]                        = ReadCTransferPart,
            ["CAdcTransferPart"]                     = ReadCAdcTransferPart,
            ["CDioTransferPart"]                     = ReadCAdcTransferPart,
            ["CDacTransferPart"]                     = ReadCAdcTransferPart,
            ["CBasicHvTransferPart"]                 = ReadCAdcTransferPart,
            ["CCalculatingDacTransferPart"]          = ReadCAdcTransferPart,
            ["CScaleHvTransferPart"]                 = ReadCAdcTransferPart,
            ["CMagnetCurrentTransferPart"]           = ReadCMagnetCurrentTransferPart,

            // --- CGasConfPart chain ---
            ["CIntegrationUnitGasConfPart"]          = ReadCIntegrationUnitGasConfPart,
            ["CChannelGasConfPart"]                  = ReadCChannelGasConfPart,

            // --- CBasicScan (CData-derived) ---
            ["CBasicScan"]                           = ReadCBasicScan,

            // --- CScanPart chain ---
            ["CScanPart"]                            = ReadCScanPart,
            ["CClockScanPart"]                       = ReadCClockScanPart,
            ["CScaleHvScanPart"]                     = ReadCScaleHvScanPart,
            ["CMagnetCurrentScanPart"]               = ReadCMagnetCurrentScanPart,
            ["CIntegrationUnitScanPart"]             = ReadCIntegrationUnitScanPart,

            // --- CHardwarePart chain ---
            ["CHardwarePart"]                        = ReadCHardwarePart,
            ["CCupHardwarePart"]                     = ReadCCupHardwarePart,
            ["CChannelHardwarePart"]                 = ReadCChannelHardwarePart,
            ["CScaleHardwarePart"]                   = ReadCScaleHardwarePart,
            ["CClockHardwarePart"]                   = ReadCClockHardwarePart,
            ["CIntegrationUnitHardwarePart"]         = ReadCIntegrationUnitHardwarePart,
            ["CDacHardwarePart"]                     = ReadCDacHardwarePart,
            ["CScaleHvHardwarePart"]                 = ReadCScaleHvHardwarePart,
            ["CMagnetCurrentHardwarePart"]           = ReadCMagnetCurrentHardwarePart,

            // --- CPlotInfo / CTraceInfo (.scn) ---
            ["CPlotInfo"]                            = ReadCPlotInfo,
            ["CTraceInfo"]                           = ReadCTraceInfo,
            ["CTraceInfoEntry"]                      = ReadCTraceInfoEntry,
            ["CPlotRange"]                           = ReadCPlotRangeObj,

            // --- CStringArray ---
            ["CStringArray"]                         = ReadCStringArray,
            ["CParsedEvaluationString"]              = ReadCParsedEvaluationString,

            // --- CAction chain ---
            ["CAction"]                              = ReadCAction,
            ["CActionPeakCenter"]                    = ReadCActionPeakCenter,
            ["CActionHwTransferContainer"]           = ReadCActionHwTransferContainer,
            ["CActionSubScript"]                     = ReadCActionSubScript,
            ["CDelay"]                               = ReadCDelay,
            ["CActionInterpreter"]                   = ReadCActionInterpreter,
            ["CMethodSwitcher"]                      = ReadCMethodSwitcher,
            ["CTimeEventList"]                       = ReadCTimeEventList,

            // --- CMethodPart / CEvaluationPart chain ---
            ["CEvaluationPart"]                      = ReadCEvaluationPart,
            ["CMethodPart"]                          = ReadCEvaluationPart,
            ["CMethodPrintoutDesc"]                  = ReadCMethodPrintoutDesc,
            ["CComponentListMethodPart"]             = ReadCComponentListMethodPart,
            ["CPartMirror"]                          = ReadCPartMirror,
            ["CTimeEventListMethodPart"]             = ReadCTimeEventListMethodPart,
            ["CContiniousFlowStandardizationMethodPart"]     = ReadCContiniousFlowStandardizationMethodPart,
            ["CContiniousFlowStandardizationListMethodPart"] = ReadCContiniousFlowStandardizationListMethodPart,
            ["CPrimaryStandardMethodPart"]           = ReadCPrimaryStandardMethodPart,
            ["CSecondaryStandardMethodPart"]         = ReadCSecondaryStandardMethodPart,
            ["CConFloMethodPart"]                    = ReadCConFloMethodPart,
            ["CICA_BasicMethodPart"]                 = ReadCICA_BasicMethodPart,
            ["CPeakFindMethodPart"]                  = ReadCPeakFindMethodPart,
            ["CSimplePeakFindMethodPart"]            = ReadCSimplePeakFindMethodPart,
            ["CSimplePeakFindParameter"]             = ReadCSimplePeakFindParameter,

            // --- CDeviceMethodPart chain ---
            ["CDeviceMethodPart"]                    = ReadCDeviceMethodPart,
            ["CConFloDeviceMethodPart"]              = ReadCConFloDeviceMethodPart,
            ["CMsDeviceMethodPart"]                  = ReadCMsDeviceMethodPart,
            ["CStandardDeviceMethodPart"]            = ReadCStandardDeviceMethodPart,
            ["CGenericGcDeviceMethodPart"]           = ReadCGenericGcDeviceMethodPart,
            ["CFlashEA_DeviceMethodPart"]            = ReadCFlashEA_DeviceMethodPart,
            ["CMultiReferenceDeviceMethodPart"]      = ReadCMultiReferenceDeviceMethodPart,
            ["CActiveDeviceMethodPart"]              = ReadCDeviceMethodPart,

            // --- CDeviceEvaluationPart chain ---
            ["CDeviceEvaluationPart"]                = ReadCDeviceEvaluationPart,
            ["CConFloDeviceEvaluationPart"]          = ReadCConFloDeviceEvaluationPart,
            ["CMsDeviceEvaluationPart"]              = ReadCMsDeviceEvaluationPart,
            ["CGenericGcDeviceEvaluationPart"]       = ReadCDeviceEvaluationPart,
            ["CFlashEA_DeviceEvaluationPart"]        = ReadCFlashEA_DeviceEvaluationPart,
            ["CMultiReferenceDeviceEvaluationPart"]  = ReadCDeviceEvaluationPart,

            // --- CEvalDataTransferPart chain ---
            ["CEvalDataTransferPart"]                = ReadCEvalDataTransferPart,
            ["CEvalDataDWORDTransferPart"]           = ReadCEvalDataDWORDTransferPart,
            ["CEvalDataSecStdTransferPart"]          = ReadCEvalDataSecStdTransferPart,
            ["CEvalDataStringTransferPart"]          = ReadCEvalDataStringTransferPart,
            ["CEvalDataIntTransferPart"]             = ReadCEvalDataTransferPart,
            ["CEvalDataDoubleTransferPart"]          = ReadCEvalDataTransferPart,

            // --- Peak stubs ---
            ["CGCPeak"]                              = ReadCGCPeak,
            ["CSPeak"]                               = ReadCSPeak,

            // --- Script / Dynamic External variable classes ---
            ["CScrHeadLine"]                         = ReadCScrHeadLine,
            ["CScrNumber"]                           = ReadCScrNumber,
            ["CDynExternal"]                         = ReadCDynExternal,
            ["CNumericValue"]                        = ReadCNumericValue,

            // --- Misc stand-alone ---
            ["CShrinkInfo"]                          = ReadCShrinkInfo,

            // --- CContiniousFlowBlockData (top-level DXF object) ---
            ["CContiniousFlowBlockData"]             = ReadCContiniousFlowBlockData,
            ["CScanStorage"]                         = ReadCScanStorage,
        };
    }

    // =======================================================================
    // Dispatch: read CRuntimeClass header → look up reader → call it
    // =======================================================================

    public static bool HasReader(string className) => _registry.ContainsKey(className);

    /// <summary>
    /// Read CRuntimeClass header from stream, then dispatch to registered reader.
    /// Returns <c>null</c> (JSON null) when the stream contains the MFC NULL
    /// WriteObject tag and <paramref name="expected"/> is <c>null</c>.
    /// </summary>
    public static JsonNode? Dispatch(IsodatArchive ar, string? expected = null)
    {
        long pos = ar.Position;
        string? className = ar.ReadCRuntimeClass(expected);
        if (className is null) return null;   // MFC NULL WriteObject
        if (!_registry.TryGetValue(className, out var reader))
            throw new InvalidDataException(
                $"No reader registered for class '{className}' (header at 0x{pos:x})");
        return reader(ar);
    }

    /// <summary>
    /// Dispatch to a reader for an already-consumed class name (no header read).
    /// </summary>
    public static JsonObject DispatchKnown(IsodatArchive ar, string className)
    {
        if (!_registry.TryGetValue(className, out var reader))
            throw new InvalidDataException(
                $"No reader registered for class '{className}'");
        return reader(ar);
    }

    /// <summary>
    /// Like Dispatch, but asserts non-null (throws on MFC NULL WriteObject).
    /// Use when the object is structurally required.
    /// </summary>
    static JsonObject DispatchObj(IsodatArchive ar, string? expected = null)
    {
        var node = Dispatch(ar, expected);
        if (node is not JsonObject jo)
            throw new InvalidDataException(
                $"Expected non-null WriteObject for '{expected ?? "(any)"}' but encountered MFC NULL tag");
        return jo;
    }

    /// <summary>
    /// Like Dispatch, but if the result is a CBlockData (has top-level "n_objects"),
    /// recursively dispatches and embeds its sub-children before returning.
    /// This is needed for CBlockData nodes that appear as children of other CBlockData
    /// nodes, because ReadCBlockData only reads the header — sub-children are left in
    /// the stream and must be consumed by the caller.
    /// </summary>
    static JsonNode? DispatchFully(IsodatArchive ar)
    {
        var node = Dispatch(ar);
        if (node is JsonObject jo)
        {
            int n = jo["n_objects"]?.GetValue<int>() ?? 0;
            if (n > 0)
            {
                var sub = new JsonArray();
                for (int i = 0; i < n; i++)
                    sub.Add(DispatchFully(ar));
                jo["children"] = sub;
            }
        }
        return node;
    }

    // Convenience: dispatch N objects and collect into JsonArray
    static JsonArray DispatchN(IsodatArchive ar, int n, string? expected = null)
    {
        var arr = new JsonArray();
        for (int i = 0; i < n; i++)
        {
            arr.Add(Dispatch(ar, expected));
        }
        return arr;
    }

    // Helper: get n_objects from a CBlockData-like JsonObject
    static int NObjects(JsonObject jo) =>
        jo["n_objects"]?.GetValue<int>() ?? 0;

    // =======================================================================
    // CFileHeader
    // =======================================================================

    static JsonObject ReadCFileHeader(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["magic"]         = ar.ReadInt32();
        int version         = ar.ReadSchemaVersion("CFileHeader", 6);
        jo["version"]       = version;
        jo["runtime_class"] = ar.ReadMfcString();
        jo["xac"]           = ar.ReadMfcString();

        if (version >= 2) jo["xb0"] = ar.ReadInt32();

        if (version >= 3)
        {
            jo["p_c_block_data"] = ReadCBlockData(ar);
            ar.ReadCRuntimeClass("CTimeObject");
            jo["c_time_object"] = ReadCTimeObject(ar);
            ar.ReadCRuntimeClass("CStr");
            jo["c_str"] = ReadCStr(ar);
        }

        if (version >= 4)
        {
            ar.ReadCRuntimeClass("CDataIndex");
            jo["c_data_index"] = ReadCDataIndex(ar);
        }

        if (version >= 5)
        {
            jo["isodat_version"] = ar.ReadMfcString();
            if (version >= 6)
                jo["isodat_minor_version"] = ar.ReadMfcString();
        }

        return jo;
    }

    // =======================================================================
    // CData chain
    // =======================================================================

    public static JsonObject ReadCData(IsodatArchive ar)
    {
        var jo = new JsonObject();
        int version   = ar.ReadSchemaVersion("CData", 3);
        jo["version"] = version;
        jo["app_id"]  = ar.ReadUInt16();
        jo["label"]   = ar.ReadMfcString();
        jo["value"]   = ar.ReadMfcString();
        if (version >= 3) jo["flags"] = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCCalibrationPoint(IsodatArchive ar)
    {
        var jo       = new JsonObject();
        jo["p_c_data"] = ReadCData(ar);
        int version  = ar.ReadSchemaVersion("CCalibrationPoint", 3);
        jo["version"] = version;
        jo["x94"]    = ar.ReadInt32();
        jo["x98"]    = ar.ReadDouble();
        if (version >= 3)
        {
            jo["x_a0"] = ar.ReadDouble();
            jo["x_a8"] = ar.ReadDouble();
        }
        return jo;
    }

    static JsonObject ReadCMolecule(IsodatArchive ar)
    {
        var jo         = new JsonObject();
        jo["p_c_data"] = ReadCData(ar);
        jo["version"]  = ar.ReadSchemaVersion("CMolecule", 1);
        jo["molecule"] = ar.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCTimeObject(IsodatArchive ar)
    {
        var jo         = new JsonObject();
        jo["p_c_data"] = ReadCData(ar);
        jo["version"]  = ar.ReadSchemaVersion("CTimeObject", 1);
        jo["datetime"] = ar.ReadTimestamp();
        return jo;
    }

    static JsonObject ReadCISLScriptMessageData(IsodatArchive ar)
    {
        var jo              = new JsonObject();
        jo["p_c_data"]      = ReadCData(ar);
        jo["version"]       = ar.ReadSchemaVersion("CISLScriptMessageData", 1);
        jo["display_text"]  = ar.ReadMfcString();
        jo["source_class"]  = ar.ReadMfcString();
        // x9c = 0xFFFFFFFF (-1) and xa0 = 0x00000000 as plain int32 fields.
        // These bytes superficially resemble an MFC new-class header (ff ff ff ff 00 00)
        // but they are raw serialized data, not WriteObject calls.
        jo["x9c"]           = ar.ReadInt32();
        jo["xa0"]           = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCComponent(IsodatArchive ar)
    {
        var jo         = new JsonObject();
        jo["p_c_data"] = ReadCData(ar);
        ar.ReadSchemaVersion("CComponent", 1); // discard
        jo["x94"] = ar.ReadInt32();
        jo["x98"] = ar.ReadInt32();
        jo["xa0"] = ar.ReadInt32();
        jo["xa4"] = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCEvalIntegrationUnitHWInfo(IsodatArchive ar)
    {
        var jo         = new JsonObject();
        jo["p_c_data"] = ReadCData(ar);
        ar.ReadSchemaVersion("CEvalIntegrationUnitHWInfo", 1); // discard
        jo["mass"]     = ar.ReadDouble();
        jo["channel"]  = ar.ReadInt32();
        jo["resistor"] = ar.ReadDouble();
        jo["cup"]      = ar.ReadInt32();
        return jo;
    }

    // CTraceSettings: Serialize does NOT call CData::Serialize
    static JsonObject ReadCTraceSettings(IsodatArchive ar)
    {
        var jo = new JsonObject();
        int version            = ar.ReadSchemaVersion("CTraceSettings", 4);
        jo["version"]          = version;
        jo["nominator_trace_idx"] = ar.ReadInt32();
        jo["divisor_trace_idx"]   = ar.ReadInt32();
        jo["source_trace_idx"]    = ar.ReadInt32();
        if (version >= 2)
        {
            jo["trace_fac_a"] = ar.ReadDouble();
            jo["trace_fac_b"] = ar.ReadDouble();
        }
        if (version >= 3) jo["enabled"]       = ar.ReadInt32();
        if (version >= 4)
        {
            jo["nominator_mass"] = ar.ReadInt32();
            jo["divisor_mass"]   = ar.ReadInt32();
            jo["eval_list"]      = ar.ReadMfcString();
            jo["eval_name"]      = ar.ReadMfcString();
            jo["xc4"]            = ar.ReadInt32();
        }
        return jo;
    }

    static JsonObject ReadCEvalDataItemTransferPart(IsodatArchive ar)
    {
        var jo            = new JsonObject();
        jo["p_c_data"]    = ReadCData(ar);
        int version       = ar.ReadSchemaVersion("CEvalDataItemTransferPart", 8);
        jo["version"]     = version;
        jo["id"]          = ar.ReadMfcString();
        jo["name"]        = ar.ReadMfcString();
        jo["format"]      = ar.ReadMfcString();
        jo["gas_name"]    = ar.ReadMfcString();
        jo["element_name"]= ar.ReadMfcString();
        if (version >= 2) jo["units"] = ar.ReadMfcString();
        if (version >= 3) jo["info"]  = ar.ReadMfcString();
        if (version >= 5) jo["xb4"]   = ar.ReadInt32();
        if (version >= 6) jo["xb0"]   = ar.ReadMfcString();
        if (version >= 7) jo["xb8"]   = ar.ReadInt32();
        if (version >= 8) jo["ampere_calculation"] = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCPeakDataItem(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_eval_data_item_transfer_part"] = ReadCEvalDataItemTransferPart(ar);
        int version = ar.ReadSchemaVersion("CPeakDataItem", 1);
        jo["version"] = version;
        ar.ReadMfcString(); // ID recomputed at runtime, discard
        jo["xc0"] = ar.ReadInt32();
        jo["xc4"] = ar.ReadInt32();
        return jo;
    }

    // CWinColor: Serialize does NOT call CData::Serialize; has embedded CBlockData via WriteObject
    static JsonObject ReadCWinColor(IsodatArchive ar)
    {
        var jo         = new JsonObject();
        var blockData  = DispatchObj(ar, "CBlockData");
        jo["p_c_block_data"] = blockData;
        int n          = NObjects(blockData);
        jo["c_trace_lin_col"] = DispatchN(ar, n, "CTraceLinCol");
        return jo;
    }

    // CTraceLinCol: Serialize does NOT call CData::Serialize
    static JsonObject ReadCTraceLinCol(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["line_color"] = ar.ReadColor();
        jo["line_type"]  = ar.ReadInt32();
        jo["line_width"] = ar.ReadInt32();
        return jo;
    }

    // CGridColors: Serialize does NOT call CData::Serialize; 9 COLORREF values
    static JsonObject ReadCGridColors(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["x94"] = ar.ReadColor();
        jo["x98"] = ar.ReadColor();
        jo["x9c"] = ar.ReadColor();
        jo["xa0"] = ar.ReadColor();
        jo["xa4"] = ar.ReadColor();
        jo["xa8"] = ar.ReadColor();
        jo["xac"] = ar.ReadColor();
        jo["xb0"] = ar.ReadColor();
        jo["xb4"] = ar.ReadColor();
        return jo;
    }

    // CAxisPara: Serialize does NOT call CData::Serialize
    static JsonObject ReadCAxisPara(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["x94"]           = ar.ReadInt32();
        jo["c_trace_lin_col"] = Dispatch(ar, "CTraceLinCol");
        return jo;
    }

    static JsonObject ReadCH3FactorResult(IsodatArchive ar)
    {
        var jo         = new JsonObject();
        jo["p_c_data"] = ReadCData(ar);
        int version    = ar.ReadSchemaVersion("CH3FactorResult", 4);
        jo["version"]  = version;
        jo["x98_x9c"]  = ar.ReadDouble();
        jo["xa0_xa4"]  = ar.ReadDouble();
        if (version >= 2) jo["xa8"] = ar.ReadUInt32();
        if (version >= 3) { jo["xac"] = ar.ReadMfcString(); jo["xb8"] = ar.ReadInt32(); }
        if (version >= 4) { jo["xb0"] = ar.ReadMfcString(); jo["xb4"] = ar.ReadMfcString(); jo["xbc"] = ar.ReadInt32(); }
        return jo;
    }

    static JsonObject ReadCApplicationData(IsodatArchive ar)
    {
        var jo         = new JsonObject();
        jo["p_c_data"] = ReadCData(ar);
        ar.ReadSchemaVersion("CApplicationData", 2); // discard, no gating
        jo["x94"] = ar.ReadUInt32();
        jo["x98"] = ar.ReadUInt32();
        jo["x9c"] = ar.ReadUInt32();
        jo["xa0"] = ar.ReadUInt16();
        jo["xa4"] = ar.ReadUInt32();
        jo["xa8"] = ar.ReadUInt32();
        jo["xac"] = ar.ReadUInt32();
        jo["xb0"] = ar.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCResultForGas(IsodatArchive ar)
    {
        var jo         = new JsonObject();
        jo["p_c_data"] = ReadCData(ar);
        ar.ReadSchemaVersion("CResultForGas", 1); // discard
        jo["x94"] = ar.ReadMfcString();
        jo["x98"] = ar.ReadMfcString();
        jo["c_data_xa4"] = Dispatch(ar, "CData");
        return jo;
    }

    static JsonObject ReadCPeakFindParameter(IsodatArchive ar)
    {
        ar.AddWarning("CPeakFindParameter: only CData parent read (stub)");
        return new JsonObject { ["p_c_data"] = ReadCData(ar) };
    }

    static JsonObject ReadCMRI_DilutionList(IsodatArchive ar)
    {
        var jo         = new JsonObject();
        jo["p_c_data"] = ReadCData(ar);
        int version    = ar.ReadSchemaVersion("CMRI_DilutionList", 1);
        jo["version"]  = version;
        int n          = ar.ReadInt32();
        if (n > 0)
        {
            var items = new JsonArray();
            for (int i = 0; i < n; i++)
                items.Add(new JsonObject { ["a"] = ar.ReadDouble(), ["b"] = ar.ReadDouble() });
            jo["items"] = items;
        }
        return jo;
    }

    // =======================================================================
    // CSimple chain
    // =======================================================================

    public static JsonObject ReadCSimple(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["version"] = ar.ReadSchemaVersion("CSimple", 2);
        jo["label"]   = ar.ReadMfcString();
        return jo;
    }

    public static JsonObject ReadCStr(IsodatArchive ar)
    {
        var jo             = new JsonObject();
        jo["p_c_simple"]   = ReadCSimple(ar);
        jo["version"]      = ar.ReadSchemaVersion("CStr", 2);
        jo["value"]        = ar.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCDword(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_simple"] = ReadCSimple(ar);
        jo["version"]    = ar.ReadSchemaVersion("CDword", 2);
        jo["value"]      = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCBinary(IsodatArchive ar)
    {
        var jo      = new JsonObject();
        jo["p_c_simple"] = ReadCSimple(ar);
        jo["version"] = ar.ReadSchemaVersion("CBinary", 2);
        int nBytes  = ar.ReadInt32();
        jo["n_bytes"] = nBytes;
        if (nBytes > 0)
            jo["data"] = Convert.ToBase64String(ar.ReadBytes(nBytes));
        return jo;
    }

    // =======================================================================
    // CBlockData chain
    // =======================================================================

    public static JsonObject ReadCBlockData(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_data"]  = ReadCData(ar);
        jo["version"]   = ar.ReadSchemaVersion("CBlockData", 2);
        jo["n_objects"] = ar.ReadInt32();
        return jo;
    }

    public static JsonObject ReadCDataIndex(IsodatArchive ar)
    {
        var jo           = new JsonObject();
        var block        = ReadCBlockData(ar);
        if (NObjects(block) != 0)
            throw new InvalidDataException($"CDataIndex: expected 0 children, got {NObjects(block)}");
        ar.ReadInt32(); // trailing sentinel (always 1)
        jo["p_c_block_data"] = block;
        return jo;
    }

    static JsonObject ReadCCalibration(IsodatArchive ar)
    {
        var jo         = new JsonObject();
        var block      = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        int nObj       = NObjects(block);
        if (nObj > 0)
            jo["c_calibration_point"] = DispatchN(ar, nObj, "CCalibrationPoint");
        int version    = ar.ReadSchemaVersion("CCalibration", 5);
        jo["version"]  = version;
        jo["x_a8"]     = ar.ReadUInt8();
        jo["x_ac"]     = ar.ReadMfcString();
        jo["x_b0"]     = ar.ReadTimestamp();
        if (version < 5) ar.ReadDouble(); // legacy
        jo["x_bc"]     = ar.ReadInt32();
        if (version >= 3) jo["x_c0"] = ar.ReadUInt8();
        if (version >= 4)
        {
            var splines = new JsonArray();
            bool cont   = true;
            while (cont)
            {
                var spline = new JsonArray();
                for (int idx = 0; idx < 8; idx++)
                {
                    int n     = ar.ReadUInt16();
                    var vals  = new JsonArray();
                    for (int j = 0; j < n; j++) vals.Add((JsonNode)ar.ReadDouble());
                    spline.Add(new JsonObject { ["n"] = n, ["values"] = vals });
                }
                cont = ar.ReadBool32();
                splines.Add(spline);
            }
            jo["splines"] = splines;
        }
        return jo;
    }

    static JsonObject ReadCVisualisationData(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        if (NObjects(block) != 0)
            throw new InvalidDataException($"CVisualisationData: expected 0 children, got {NObjects(block)}");
        int version   = ar.ReadSchemaVersion("CVisualisationData", 8);
        jo["version"] = version;

        jo["x_a8"] = ReadIntArray(ar, 4);
        jo["x_b8"] = ReadIntArray(ar, 10);
        jo["x_e0"] = ReadIntArray(ar, 10);

        if (version >= 2)
        {
            jo["font"]  = ar.ReadMfcString();
            jo["x10c"]  = ar.ReadMfcString();
            jo["x110"]  = ar.ReadMfcString();
            if (version >= 3)
            {
                jo["x120"] = ar.ReadInt32();
                if (version >= 4)
                {
                    jo["x124"] = ar.ReadInt32();
                    if (version >= 5)
                    {
                        jo["x148"] = ar.ReadMfcString();
                        if (version >= 6)
                        {
                            jo["x11c"] = ar.ReadInt32();
                            if (version >= 7)
                            {
                                jo["x128"] = ar.ReadInt32();
                                if (version >= 8)
                                    jo["x12c"] = ar.ReadInt32();
                            }
                        }
                    }
                }
            }
        }
        return jo;
    }

    static JsonObject ReadCGasConfiguration(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        jo["c_data"] = DispatchN(ar, NObjects(block));
        int version  = ar.ReadSchemaVersion("CGasConfiguration", 3);
        jo["version"] = version;
        if (version >= 3) jo["timestamp"] = ar.ReadTimestamp();
        return jo;
    }

    static JsonObject ReadCMeasurmentInfos(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        jo["c_isl_script_message_data"] = DispatchN(ar, NObjects(block));
        ar.ReadInt32(); // trailing sentinel (always 1)
        return jo;
    }

    static JsonObject ReadCMeasurmentErrors(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        jo["c_isl_script_message_data"] = DispatchN(ar, NObjects(block));
        ar.ReadInt32(); // trailing sentinel (always 1)
        return jo;
    }

    static JsonObject ReadCPlotSettings(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        jo["c_win_settings"] = DispatchN(ar, NObjects(block));
        int version = ar.ReadSchemaVersion("CPlotSettings", 5);
        jo["version"] = version;
        if (version >= 2) { jo["xb0"] = ar.ReadMfcString(); jo["configuration_name"] = ar.ReadMfcString(); }
        if (version >= 3)   jo["peak_labelling"]   = ar.ReadInt32();
        if (version >= 4)   jo["refresh_data_grid"] = ar.ReadInt32();
        if (version >= 5)   jo["ampere_calc_flag"]  = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCWinSettings(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        jo["c_gas_settings"] = DispatchN(ar, NObjects(block));
        int version = ar.ReadSchemaVersion("CWinSettings", 4);
        jo["version"]     = version;
        jo["min_val_a"]   = ar.ReadDouble();
        jo["min_val_b"]   = ar.ReadDouble();
        jo["max_val_a"]   = ar.ReadDouble();
        jo["max_val_b"]   = ar.ReadDouble();
        jo["min_perc_x"]  = ar.ReadInt32();
        jo["min_perc_y"]  = ar.ReadInt32();
        jo["max_perc_x"]  = ar.ReadInt32();
        jo["max_perc_y"]  = ar.ReadInt32();
        jo["min_perc_y_alt"] = ar.ReadInt32();
        jo["max_perc_y_alt"] = ar.ReadInt32();
        jo["trace_type"]  = ar.ReadInt32();
        jo["x10c"]        = ar.ReadInt32();
        jo["x110"]        = ar.ReadInt32();
        jo["x114"]        = ar.ReadInt32();
        jo["x118"]        = ar.ReadInt32();
        jo["c_view_colors"] = Dispatch(ar, "CViewColors");
        if (version == 2)
        {
            ar.AddWarning("CWinSettings v2: reading legacy object (untested)");
            Dispatch(ar); // discard
        }
        if (version >= 4) jo["x128"] = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCViewColors(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        if (NObjects(block) != 3)
            throw new InvalidDataException($"CViewColors: expected 3 children, got {NObjects(block)}");
        jo["c_win_color"]   = Dispatch(ar, "CWinColor");
        jo["c_grid_colors"] = Dispatch(ar, "CGridColors");
        jo["c_axis_para"]   = Dispatch(ar, "CAxisPara");
        jo["xa8"] = ar.ReadColor();
        jo["xac"] = ar.ReadColor();
        jo["xb0"] = ar.ReadColor();
        jo["xb4"] = ar.ReadColor();
        jo["xb8"] = ar.ReadColor();
        return jo;
    }

    static JsonObject ReadCGasSettings(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        jo["c_trace_settings"] = DispatchN(ar, NObjects(block));
        int version = ar.ReadSchemaVersion("CGasSettings", 5);
        jo["version"]         = version;
        jo["c_pk_data_item_list"] = Dispatch(ar, "CPkDataItemList");
        if (version >= 2) jo["gas"] = ar.ReadMfcString();
        if (version >= 3)
        {
            int hasShrink = ar.ReadInt32();
            if (hasShrink != 0) jo["c_shrink_info"] = Dispatch(ar, "CShrinkInfo");
        }
        if (version >= 4) jo["eval_list"] = ar.ReadMfcString();
        if (version >= 5) jo["ampere_calc_flag"] = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCPkDataItemList(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"]  = block;
        jo["c_peak_data_item"] = DispatchN(ar, NObjects(block));
        jo["version"] = ar.ReadSchemaVersion("CPkDataItemList", 1);
        jo["xa8"]     = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCAllMoleculeWeights(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        if (NObjects(block) != 0)
            throw new InvalidDataException($"CAllMoleculeWeights: expected 0 children, got {NObjects(block)}");
        int version = ar.ReadSchemaVersion("CAllMoleculeWeights", 2);
        jo["version"] = version;
        if (version >= 2)
        {
            ar.ReadInt32(); // heap pointer snapshots, discard
            ar.ReadInt32();
        }
        return jo;
    }

    static JsonObject ReadCMethod(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        int nChildren = NObjects(block);
        if (nChildren > 0)
        {
            var children = new JsonArray();
            for (int ci = 0; ci < nChildren; ci++)
            {
                long posBefore = ar.Position;
                string? childClass = ar.PeekClassAt(posBefore);
                var child = DispatchFully(ar);
                Console.Error.WriteLine($"  DBG CMethod child[{ci}]: 0x{posBefore:x5}→0x{ar.Position:x5} ({ar.Position - posBefore} bytes), class={childClass}");
                children.Add(child);
                if (ci == 0) { jo["children"] = children; return jo; }  // DBG: stop after first child
            }
            jo["children"] = children;
        }

        int version = ar.ReadSchemaVersion("CMethod", 10);
        jo["version"] = version;
        Console.Error.WriteLine($"  DBG CMethod version={version} pos=0x{ar.Position:x5} nChildren={nChildren}");

        long posBeforeConfig = ar.Position;
        jo["c_configuration"] = Dispatch(ar);  // CConfiguration; may be null WriteObject
        Console.Error.WriteLine($"  DBG CMethod after CConfiguration: consumed {ar.Position - posBeforeConfig} bytes, pos=0x{ar.Position:x5}");
        ar.DumpBytes(8);
        jo["x9c"] = ar.ReadMfcString();
        jo["xa0"] = ar.ReadMfcString();
        jo["xa4"] = ar.ReadMfcString();

        // N CDeviceMethodPart objects (polymorphic — concrete subclass in stream)
        int nDeviceParts = ar.ReadInt32();
        if (nDeviceParts > 0)
            jo["c_device_method_parts"] = DispatchN(ar, nDeviceParts);

        if (version >= 2)
        {
            int nEvalParts = ar.ReadInt32();
            if (nEvalParts > 0)
                jo["c_device_eval_parts"] = DispatchN(ar, nEvalParts);
        }

        if (version >= 3) jo["acq_type"]  = ar.ReadInt32();
        if (version >= 4) jo["xc4"]       = ar.ReadMfcString();

        if (version >= 5)
        {
            int nSubMethods = ar.ReadInt32();
            if (nSubMethods > 0)
                jo["sub_methods"] = DispatchN(ar, nSubMethods, "CMethod");
        }

        if (version >= 6) jo["xd0"] = ar.ReadMfcString();
        if (version >= 7) jo["xcc"] = ar.ReadInt32();
        if (version >= 9) { jo["xd4"] = ar.ReadInt32(); jo["xd8"] = ar.ReadInt32(); }
        if (version >= 10) jo["xdc"] = ar.ReadInt32();

        return jo;
    }

    static JsonObject ReadCConfiguration(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        int n = NObjects(block);
        if (n > 0) jo["children"] = DispatchN(ar, n);
        int version = ar.ReadSchemaVersion("CConfiguration", 7);
        jo["version"] = version;
        if (version >= 3) jo["xa8"] = ar.ReadInt32();
        if (version >= 4) jo["xac"] = ar.ReadInt32();
        if (version >= 5) jo["xb0"] = ar.ReadMfcString();
        if (version >= 6) jo["xb4"] = ar.ReadInt32();
        if (version >= 7) jo["xb8"] = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCComponentList(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        int n = NObjects(block);
        if (n > 0) jo["children"] = DispatchN(ar, n);
        ar.ReadSchemaVersion("CComponentList", 1); // discard
        return jo;
    }

    static JsonObject ReadCParsedEvaluationStringArray(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        // children are CParsedEvaluationString objects
        jo["children"] = DispatchN(ar, NObjects(block));
        int version = ar.ReadSchemaVersion("CParsedEvaluationStringArray", 4);
        jo["version"] = version;
        jo["xa8"] = ar.ReadMfcString();
        if (version >= 2) jo["xb0"] = ar.ReadUInt32();
        if (version >= 3) { jo["xb8"] = ar.ReadUInt32(); jo["xbc"] = ar.ReadUInt32(); }
        if (version >= 4) jo["xc0"] = ar.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCResultArray(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        jo["children"] = DispatchN(ar, NObjects(block));
        int version = ar.ReadSchemaVersion("CResultArray", 2);
        jo["version"] = version;
        jo["xa8"] = ar.ReadUInt32();
        if (version >= 2) jo["xac"] = ar.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCActionScript(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        int version = ar.ReadSchemaVersion("CActionScript", 5);
        jo["version"] = version;
        if (version >= 3) jo["c_application_data"] = Dispatch(ar, "CApplicationData");
        if (version >= 4) jo["x168"] = ar.ReadUInt32();
        if (version >= 5) { jo["x1c0"] = ar.ReadMfcString(); jo["x1c4"] = ar.ReadUInt32(); }
        return jo;
    }

    static JsonObject ReadCGCPeakList(IsodatArchive ar)
    {
        ar.AddWarning("CGCPeakList: only CBlockData parent + version read (stub)");
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        jo["version"] = ar.ReadSchemaVersion("CGCPeakList", 6);
        return jo;
    }

    static JsonObject ReadCVisualisationDialogNamesBlockData(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        ar.ReadSchemaVersion("CVisualisationDialogNamesBlockData", 1); // discard
        return jo;
    }

    static JsonObject ReadCEvalDataItemListTransferPart(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        int n = NObjects(block);
        if (n > 0) jo["children"] = DispatchN(ar, n);
        ar.ReadSchemaVersion("CEvalDataItemListTransferPart", 1); // discard
        return jo;
    }

    // =======================================================================
    // CDevice chain
    // =======================================================================

    static JsonObject ReadCDevice(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar);
        jo["p_c_block_data"] = block;
        int nChildren = NObjects(block);
        if (nChildren > 0)
        {
            var children = new JsonArray();
            for (int i = 0; i < nChildren; i++) children.Add(DispatchFully(ar));
            jo["children"] = children;
        }
        int version = ar.ReadSchemaVersion("CDevice", 5);
        jo["version"] = version;
        jo["xac"] = ar.ReadUInt32();
        jo["xb0"] = ar.ReadUInt32();
        if (version >= 3) jo["xa8"] = ar.ReadUInt32();
        if (version >= 4) jo["xb4"] = ar.ReadUInt32();
        if (version >= 5) jo["xb8"] = ar.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCActiveDevice(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_device"] = ReadCDevice(ar);
        int version = ar.ReadSchemaVersion("CActiveDevice", 2);
        jo["version"] = version;
        if (version >= 2) jo["xec"] = ar.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCActivePort(IsodatArchive ar)
    {
        var jo    = new JsonObject();
        var block = ReadCBlockData(ar); // CPort = CBlockData
        jo["p_c_port"] = block;
        int nChildren = NObjects(block);
        if (nChildren > 0)
        {
            var children = new JsonArray();
            for (int i = 0; i < nChildren; i++) children.Add(DispatchFully(ar));
            jo["children"] = children;
        }
        int version = ar.ReadSchemaVersion("CActivePort", 2);
        jo["version"] = version;
        if (version >= 2) jo["xa8"] = ar.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCMsDevice(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_active_device"] = ReadCActiveDevice(ar);
        int version = ar.ReadSchemaVersion("CMsDevice", 2);
        jo["version"] = version;
        jo["xfc"] = ar.ReadUInt32();
        if (version >= 2) jo["x100"] = ar.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCGenericGcDevice(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_active_device"] = ReadCActiveDevice(ar);
        int version = ar.ReadSchemaVersion("CGenericGcDevice", 2);
        jo["version"] = version;
        if (version >= 2) jo["xfc"] = ar.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCFlashEA_Device(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_generic_gc_device"] = ReadCGenericGcDevice(ar);
        ar.ReadSchemaVersion("CFlashEA_Device", 1); // discard
        return jo;
    }

    // =======================================================================
    // IsoGCEvalData / CEvalDataStorage chain
    // =======================================================================

    static JsonObject ReadIsoGCEvalData(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_data"] = ReadCData(ar);
        jo["version"]  = ar.ReadSchemaVersion("IsoGCEvalData", 1);
        return jo;
    }

    static JsonObject ReadCGCData(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_iso_gc_eval_data"] = ReadIsoGCEvalData(ar);
        jo["version"]            = ar.ReadSchemaVersion("CGCData", 1);
        jo["c_eval_gc_data"]     = Dispatch(ar, "CEvalGCData");
        return jo;
    }

    static JsonObject ReadCRawData(IsodatArchive ar)
    {
        var jo       = new JsonObject();
        jo["p_c_gc_data"] = ReadCGCData(ar);
        int version  = ar.ReadSchemaVersion("CRawData", 5);
        jo["version"] = version;

        if (version <= 1) return jo;

        jo["complete_formula"] = ar.ReadMfcString();
        jo["formula"]          = ar.ReadMfcString();
        int nMasses = ar.ReadInt32();
        jo["n_masses"] = nMasses;
        if (nMasses > 0) jo["masses"] = ReadIntArray(ar, nMasses);

        jo["c_all_molecule_weights"] = Dispatch(ar, "CAllMoleculeWeights");

        if (version > 2) jo["x1048"] = ar.ReadInt32();
        if (version > 3) jo["c_string_array"] = Dispatch(ar, "CStringArray");
        if (version > 4)
        {
            jo["xf88"] = ar.ReadInt32();
            int flag   = ar.ReadInt32();
            if (flag != 0)
                jo["c_integration_unit_gas_conf_part"] =
                    Dispatch(ar, "CIntegrationUnitGasConfPart");
        }
        return jo;
    }

    // CEvalDataStorage: Serialize does NOT call CData/CBlockData
    static JsonObject ReadCEvalDataStorage(IsodatArchive ar)
    {
        var jo      = new JsonObject();
        jo["version"] = ar.ReadSchemaVersion("CEvalDataStorage", 1);
        int nBytes  = ar.ReadInt32();
        jo["n_bytes"] = nBytes;
        if (nBytes > 0)
            jo["buffer"] = Convert.ToBase64String(ar.ReadBytes(nBytes));
        jo["n_bytes2"] = ar.ReadInt32();
        jo["xa0"]      = ar.ReadInt32();
        jo["xa4"]      = ar.ReadMfcString();
        jo["xb0"]      = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCEvalFakeData(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_eval_data_storage"] = ReadCEvalDataStorage(ar);
        jo["version"]  = ar.ReadSchemaVersion("CEvalFakeData", 1);
        jo["n_traces"] = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCEvalGCData(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_eval_fake_data"] = ReadCEvalFakeData(ar);
        jo["version"]            = ar.ReadSchemaVersion("CEvalGCData", 1);
        return jo;
    }

    // =======================================================================
    // CBasicInterface chain (= CData)
    // =======================================================================

    static JsonObject ReadCFinniganInterface(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_basic_interface"] = ReadCData(ar);
        int version = ar.ReadSchemaVersion("CFinniganInterface", 6);
        jo["version"] = version;
        jo["x9c"] = ar.ReadInt32();
        if (version >= 3)
        {
            jo["xa0"] = ar.ReadInt32();
            jo["xa4"] = ar.ReadBool32();
            if (version >= 5) jo["xa8"] = ar.ReadBool32();
            if (version >= 6) jo["xac"] = ar.ReadBool32();
        }
        return jo;
    }

    static JsonObject ReadCGpibInterface(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_basic_interface"] = ReadCData(ar);
        int version = ar.ReadSchemaVersion("CGpibInterface", 3);
        jo["version"] = version;
        jo["x9c"] = ar.ReadUInt8();
        jo["x9d"] = ar.ReadUInt8();
        if (version >= 3) jo["x9e"] = ar.ReadUInt8();
        return jo;
    }

    // =======================================================================
    // CTransferPart chain
    // =======================================================================

    static JsonObject ReadCTransferPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_basic_interface"] = ReadCData(ar);
        jo["version"] = ar.ReadSchemaVersion("CTransferPart", 2);
        jo["x9c"] = ar.ReadInt32();
        jo["xa0"] = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCAdcTransferPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_transfer_part"] = ReadCTransferPart(ar);
        jo["version"]    = ar.ReadSchemaVersion("CAdcTransferPart", 2);
        jo["raw_value"]  = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCMagnetCurrentTransferPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_dac_transfer_part"] = ReadCAdcTransferPart(ar);
        jo["xa8"] = ar.ReadBool32();
        return jo;
    }

    // =======================================================================
    // CGasConfPart chain
    // =======================================================================

    static JsonObject ReadCIntegrationUnitGasConfPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_gas_conf_part"] = ReadCData(ar);
        int version  = ar.ReadSchemaVersion("CIntegrationUnitGasConfPart", 2);
        jo["version"] = version;
        int n        = ar.ReadUInt8();
        jo["n_configs"] = n;
        if (n > 0) jo["c_channel_gas_conf_part"] = DispatchN(ar, n, "CChannelGasConfPart");
        return jo;
    }

    static JsonObject ReadCChannelGasConfPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_gas_conf_part"] = ReadCData(ar);
        int version = ar.ReadSchemaVersion("CChannelGasConfPart", 4);
        jo["version"] = version;
        jo["cup"]  = ar.ReadUInt8();
        jo["mass"] = ar.ReadDouble();
        jo["xa8"]  = ar.ReadDouble();
        if (version >= 3)
        {
            jo["xb0"] = ar.ReadBool32();
            if (version >= 4)
            {
                jo["xb4"] = ar.ReadBool32();
                jo["xb8"] = ar.ReadDouble();
            }
        }
        return jo;
    }

    // =======================================================================
    // CScanPart chain
    // =======================================================================

    static JsonObject ReadCBasicScan(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_data"] = ReadCData(ar);
        int version = ar.ReadSchemaVersion("CBasicScan", 4);
        jo["version"]       = version;
        jo["c_scan_part_1"] = Dispatch(ar);  // polymorphic — any ScanPart subclass
        jo["c_scan_part_2"] = Dispatch(ar);  // polymorphic — any ScanPart subclass
        var block = DispatchObj(ar, "CBlockData");
        jo["c_block_data"]  = block;
        if (NObjects(block) > 0)
            throw new InvalidDataException($"CBasicScan: unexpected {NObjects(block)} CData children");
        jo["x04"] = ar.ReadInt32();
        jo["xa4"] = ar.ReadInt32();
        if (version >= 4) jo["x94"] = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCScanPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_basic_interface"] = ReadCData(ar);
        int version = ar.ReadSchemaVersion("CScanPart", 3);
        jo["version"]        = version;
        jo["c_hardware_part"] = Dispatch(ar);
        jo["xa0"] = ar.ReadInt32();
        jo["xa4"] = ar.ReadInt32();
        jo["xb0"] = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCClockScanPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_scan_part"] = ReadCScanPart(ar);
        jo["version"]  = ar.ReadSchemaVersion("CClockScanPart", 2);
        jo["scan_time"] = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCScaleHvScanPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_scan_part"] = ReadCScanPart(ar);
        jo["version"] = ar.ReadSchemaVersion("CScaleHvScanPart", 2);
        jo["start"] = ar.ReadInt32();
        jo["stop"]  = ar.ReadInt32();
        jo["step"]  = ar.ReadInt32();
        jo["delay"] = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCMagnetCurrentScanPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_scan_part"] = ReadCScanPart(ar);
        jo["version"] = ar.ReadSchemaVersion("CMagnetCurrentScanPart", 2);
        jo["start"] = ar.ReadInt32();
        jo["stop"]  = ar.ReadInt32();
        jo["step"]  = ar.ReadInt32();
        jo["delay"] = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCIntegrationUnitScanPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_scan_part"] = ReadCScanPart(ar);
        jo["version"] = ar.ReadSchemaVersion("CIntegrationUnitScanPart", 3);
        jo["xc0"] = ar.ReadInt32();
        jo["xc4"] = ar.ReadUInt8();
        return jo;
    }

    // =======================================================================
    // CHardwarePart chain
    // =======================================================================

    static JsonObject ReadCHardwarePart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_basic_interface"] = ReadCData(ar);
        int version = ar.ReadSchemaVersion("CHardwarePart", 10);
        jo["version"]      = version;
        jo["c_interface"]  = Dispatch(ar);
        bool hasGas = ar.ReadBool32();
        jo["has_c_gas_conf_part"] = hasGas;
        if (hasGas) jo["c_gas_conf_part"] = Dispatch(ar);
        bool hasMethod = ar.ReadBool32();
        jo["has_c_method_part"] = hasMethod;
        if (hasMethod)
            throw new InvalidDataException("CHardwarePart: non-zero CMethodPart not implemented");
        bool hasExtra = ar.ReadBool32();
        jo["has_extra_c_data"] = hasExtra;
        if (hasExtra) jo["c_data_extra"] = Dispatch(ar);

        if (version >= 3)
        {
            jo["xac"] = ar.ReadBool32();
            jo["xb0"] = ar.ReadBool32();
            jo["xb4"] = ar.ReadBool32();
            jo["xb8"] = ar.ReadBool32();
            if (version >= 7)
            {
                jo["c_visualisation_data"] = Dispatch(ar, "CVisualisationData");
                jo["xc8"] = ar.ReadDouble();
                jo["xbc"] = ar.ReadInt32();
                if (version >= 9)
                {
                    int n1 = ar.ReadInt32();
                    if (n1 > 0) throw new InvalidDataException("CHardwarePart: n_strings1 > 0 not implemented");
                    int n2 = ar.ReadInt32();
                    if (n2 > 0) throw new InvalidDataException("CHardwarePart: n_strings2 > 0 not implemented");
                    if (version >= 10) jo["xa4"] = ar.ReadMfcString();
                }
            }
        }
        return jo;
    }

    static JsonObject ReadCCupHardwarePart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_hardware_part"] = ReadCHardwarePart(ar);
        int version = ar.ReadSchemaVersion("CCupHardwarePart", 5);
        jo["version"]  = version;
        jo["mode"]     = ar.ReadUInt8();
        jo["resistor"] = ar.ReadDouble();
        jo["x138"]     = ar.ReadDouble();
        if (version >= 3)
        {
            jo["x130"] = ar.ReadDouble();
            if (version == 4) ar.SkipBytes(24); // legacy
        }
        return jo;
    }

    static JsonObject ReadCChannelHardwarePart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_hardware_part"] = ReadCHardwarePart(ar);
        jo["version"] = ar.ReadSchemaVersion("CChannelHardwarePart", 2);
        jo["x120"] = ar.ReadInt32();
        jo["x124"] = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCScaleHardwarePart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_hardware_part"] = ReadCHardwarePart(ar);
        int version = ar.ReadSchemaVersion("CScaleHardwarePart", 12);
        jo["version"]  = version;
        jo["units"]    = ar.ReadMfcString();
        jo["min_step"] = ar.ReadUInt32();
        jo["max_step"] = ar.ReadUInt32();
        if (version >= 4)
        {
            jo["format_mask"] = ar.ReadUInt32();
            if (version >= 5)
            {
                jo["x124"] = ar.ReadUInt32(); jo["x128"] = ar.ReadUInt32();
                jo["x130"] = ar.ReadUInt32(); jo["x134"] = ar.ReadUInt32();
                jo["x140"] = ar.ReadUInt32();
                if (version >= 6)
                {
                    jo["x144"] = ar.ReadUInt32(); jo["x148"] = ar.ReadUInt32();
                    jo["x14c"] = ar.ReadUInt32();
                    if (version >= 7)
                    {
                        jo["x150"] = ar.ReadUInt32();
                        if (version >= 8)
                        {
                            jo["x154"] = ar.ReadMfcString(); jo["x158"] = ar.ReadUInt32();
                            if (version >= 9)
                            {
                                jo["x138"] = ar.ReadUInt32(); jo["x13c"] = ar.ReadUInt32();
                                if (version >= 10)
                                {
                                    jo["x15c"] = ar.ReadMfcString();
                                    if (version >= 11)
                                    {
                                        jo["x160"] = ar.ReadUInt32(); jo["x164"] = ar.ReadUInt32();
                                        if (version >= 12)
                                        {
                                            jo["min_value"] = ar.ReadDouble();
                                            jo["max_value"] = ar.ReadDouble();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return jo;
    }

    static JsonObject ReadCClockHardwarePart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_scale_hardware_part"] = ReadCScaleHardwarePart(ar);
        jo["version"] = ar.ReadSchemaVersion("CClockHardwarePart", 2);
        jo["x190"]    = ar.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCIntegrationUnitHardwarePart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_scale_hardware_part"] = ReadCScaleHardwarePart(ar);
        int version = ar.ReadSchemaVersion("CIntegrationUnitHardwarePart", 3);
        jo["version"] = version;
        // Serialization order from R source (offsets are struct layout, not serial order)
        jo["x194"] = ar.ReadInt32();
        jo["x198"] = ar.ReadUInt8();
        jo["x19c"] = ar.ReadInt32();
        jo["x1a0"] = ar.ReadInt32();
        jo["x199"] = ar.ReadUInt8();
        jo["x190"] = ar.ReadUInt8();
        int nTimes = ar.ReadUInt8();
        jo["n_integration_times"] = nTimes;
        if (nTimes > 0)
        {
            var times = new JsonArray();
            for (int i = 0; i < nTimes; i++) times.Add(ar.ReadUInt16());
            jo["integration_times"] = times;
        }
        int nCups = ar.ReadUInt8();
        jo["n_cups"] = nCups;
        if (nCups > 0) jo["c_cup_hardware_part"] = DispatchN(ar, nCups, "CCupHardwarePart");
        int nChan = ar.ReadUInt8();
        jo["n_channels"] = nChan;
        if (nChan > 0) jo["c_channel_hardware_part"] = DispatchN(ar, nChan, "CChannelHardwarePart");
        if (version >= 3) { jo["x1a8"] = ar.ReadBool32(); jo["x1ac"] = ar.ReadBool32(); }
        return jo;
    }

    static JsonObject ReadCDacHardwarePart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_scale_hardware_part"] = ReadCScaleHardwarePart(ar);
        int version = ar.ReadSchemaVersion("CDacHardwarePart", 3);
        jo["version"] = version;
        jo["x190"] = ar.ReadUInt8(); jo["x191"] = ar.ReadUInt8();
        jo["x192"] = ar.ReadUInt8(); jo["x193"] = ar.ReadUInt8();
        if (version >= 3) jo["format"] = ar.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCScaleHvHardwarePart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_dac_hardware_part"] = ReadCDacHardwarePart(ar);
        int version = ar.ReadSchemaVersion("CScaleHvHardwarePart", 3);
        jo["version"] = version;
        if (version >= 3) jo["x198"] = ar.ReadDouble();
        return jo;
    }

    static JsonObject ReadCMagnetCurrentHardwarePart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_dac_hardware_part"] = ReadCDacHardwarePart(ar);
        jo["version"] = ar.ReadSchemaVersion("CMagnetCurrentHardwarePart", 2);
        jo["x198"] = ar.ReadInt32();
        jo["x19c"] = ar.ReadInt32();
        return jo;
    }

    // =======================================================================
    // CPlotInfo / CTraceInfo (.scn file classes)
    // =======================================================================

    static JsonObject ReadCPlotInfo(IsodatArchive ar)
    {
        // No own schema version; MFC archive version from CRuntimeClass header is used
        int version = ar.LastArchiveVersion;
        var jo = new JsonObject();
        jo["x10"] = ar.ReadInt32(); jo["x20"] = ar.ReadInt32();
        jo["x14"] = ar.ReadInt32(); jo["x18"] = ar.ReadInt32();
        jo["x1c"] = ar.ReadInt32();
        jo["right_left_factor"] = ar.ReadFloat();
        jo["background_color"]  = ar.ReadColor();
        jo["labels_color"]      = ar.ReadColor();
        jo["x38"] = ar.ReadInt32();
        jo["x3c"] = ar.ReadUInt16();
        jo["font"]    = ar.ReadMfcString();
        jo["x_label"] = ar.ReadMfcString();
        jo["y_label"] = ar.ReadMfcString();
        jo["trace"]   = ar.ReadMfcString();
        jo["c_trace_info"]  = Dispatch(ar, "CTraceInfo");
        jo["c_plot_range"]  = Dispatch(ar, "CPlotRange");
        jo["c_plot_range_zoom"] = Dispatch(ar, "CPlotRange");
        if (version > 1) { jo["x08"] = ar.ReadInt32(); jo["x0c"] = ar.ReadInt32(); }
        jo["c_plot_range_zoom2"] = ReadCPlotRange(ar);
        jo["c_plot_range2"]      = ReadCPlotRange(ar);
        int nTraces = jo["c_trace_info"]?["n_traces"]?.GetValue<int>() ?? 0;
        if (nTraces > 0)
        {
            var labels = new JsonArray();
            for (int i = 0; i < nTraces; i++) labels.Add(ar.ReadMfcString());
            jo["trace_labels"] = labels;
        }
        return jo;
    }

    static JsonObject ReadCTraceInfo(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["x04"]     = ar.ReadInt32();
        int nTraces   = ar.ReadUInt8();
        jo["n_traces"] = nTraces;
        if (nTraces > 0) jo["c_trace_info_entry"] = DispatchN(ar, nTraces, "CTraceInfoEntry");
        jo["n_traces"] = ar.ReadUInt8();  // read again
        if (nTraces > 0)
        {
            var labels = new JsonArray();
            for (int i = 0; i < nTraces; i++) labels.Add(ar.ReadMfcString());
            jo["trace_labels"] = labels;
        }
        return jo;
    }

    static JsonObject ReadCTraceInfoEntry(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["idx"]         = ar.ReadUInt8();
        jo["x05"]         = Convert.ToBase64String(ar.ReadBytes(1));
        jo["trace_color"] = ar.ReadColor();
        jo["x0c"] = ar.ReadInt32();
        jo["x10"] = ar.ReadInt32();
        jo["x14"] = ar.ReadInt32();
        return jo;
    }

    // CPlotRange registered as dispatched object
    static JsonObject ReadCPlotRangeObj(IsodatArchive ar) => ReadCPlotRange(ar);

    // CPlotRange inline (no CRuntimeClass header)
    public static JsonObject ReadCPlotRange(IsodatArchive ar)
    {
        return new JsonObject
        {
            ["xmin"] = ar.ReadFloat(),
            ["xmax"] = ar.ReadFloat(),
            ["ymin"] = ar.ReadDouble(),
            ["ymax"] = ar.ReadDouble(),
        };
    }

    // =======================================================================
    // CStringArray
    // =======================================================================

    static JsonObject ReadCStringArray(IsodatArchive ar)
    {
        int count = ar.ReadUInt16();
        if (count == 0xFFFF) count = ar.ReadInt32(); // uint32 fallback
        var jo = new JsonObject();
        jo["n_strings"] = count;
        if (count > 0)
        {
            var arr = new JsonArray();
            for (int i = 0; i < count; i++) arr.Add(ar.ReadMfcString());
            jo["strings"] = arr;
        }
        return jo;
    }

    static JsonObject ReadCParsedEvaluationString(IsodatArchive ar)
    {
        var jo = new JsonObject();
        int version = ar.ReadSchemaVersion("CParsedEvaluationString", 2);
        jo["version"]            = version;
        jo["user_string"]        = ar.ReadMfcString();
        jo["gas_name_nominator"] = ar.ReadMfcString();
        jo["gas_name_divisor"]   = ar.ReadMfcString();
        jo["xa0"]                = ar.ReadMfcString();
        jo["mass_divisor"]       = ar.ReadMfcString();
        jo["nominator_mass"]     = ar.ReadUInt32();
        jo["divisor_mass"]       = ar.ReadUInt32();
        if (version >= 2) jo["default_visible"] = ar.ReadUInt32();
        return jo;
    }

    // =======================================================================
    // CAction chain
    // =======================================================================

    static JsonObject ReadCAction(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_data"] = ReadCData(ar);
        int version    = ar.ReadSchemaVersion("CAction", 6);
        jo["version"]  = version;
        if (version >= 3) jo["x94"] = ar.ReadInt32();
        if (version >= 4) jo["xb0"] = ar.ReadMfcString();
        if (version >= 5) jo["x9c"] = ar.ReadInt32();
        if (version >= 6) jo["xa0"] = ar.ReadInt32();
        return jo;
    }

    static JsonObject ReadCActionPeakCenter(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_action"] = ReadCAction(ar);
        ar.ReadSchemaVersion("CActionPeakCenter", 1); // discard
        jo["xbc"] = ar.ReadUInt32();
        jo["xb8"] = ar.ReadUInt8();
        return jo;
    }

    static JsonObject ReadCActionHwTransferContainer(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_action"] = ReadCAction(ar);
        int version = ar.ReadSchemaVersion("CActionHwTransferContainer", 2);
        jo["version"] = version;
        jo["xb8"]     = ar.ReadUInt32();
        jo["c_transfer_part"] = Dispatch(ar);
        if (version >= 2) { jo["xd8"] = ar.ReadUInt32(); jo["xb4"] = ar.ReadMfcString(); }
        return jo;
    }

    static JsonObject ReadCActionSubScript(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_action"] = ReadCAction(ar);
        ar.ReadSchemaVersion("CActionSubScript", 3); // discard
        string xb8 = ar.ReadMfcString();
        jo["xb8"] = xb8;
        if (xb8 == "") jo["c_action_script"] = Dispatch(ar, "CActionScript");
        return jo;
    }

    static JsonObject ReadCDelay(IsodatArchive ar)
    {
        ar.AddWarning("CDelay: only CAction parent read (stub)");
        return new JsonObject { ["p_c_action"] = ReadCAction(ar) };
    }

    static JsonObject ReadCActionInterpreter(IsodatArchive ar)
    {
        ar.AddWarning("CActionInterpreter: Serialize unknown, returning empty");
        return new JsonObject();
    }

    static JsonObject ReadCMethodSwitcher(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_action"] = ReadCAction(ar);
        int version = ar.ReadSchemaVersion("CMethodSwitcher", 5);
        jo["version"]        = version;
        jo["gas_conf_name"]  = ar.ReadMfcString();
        if (version >= 3) { jo["wait_time"] = ar.ReadUInt32(); jo["method_name"] = ar.ReadMfcString(); }
        if (version >= 4)   jo["script_path"] = ar.ReadMfcString();
        if (version >= 5)   jo["use_hysteresis"] = ar.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCTimeEventList(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_action"] = ReadCAction(ar);
        int version  = ar.ReadSchemaVersion("CTimeEventList", 3);
        jo["version"] = version;
        int n        = ar.ReadInt32();
        if (n > 0) jo["actions"] = DispatchN(ar, n);
        if (version >= 2) jo["xdc"] = ar.ReadUInt32();
        if (version >= 3) jo["xe8"] = ar.ReadUInt32();
        return jo;
    }

    // =======================================================================
    // CEvaluationPart / CMethodPart chain
    // =======================================================================

    static JsonObject ReadCEvaluationPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_data"] = ReadCData(ar);
        ar.ReadSchemaVersion("CEvaluationPart", 2); // discard
        jo["x9c"] = ar.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCMethodPrintoutDesc(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_method_part"] = ReadCEvaluationPart(ar);
        int version = ar.ReadSchemaVersion("CMethodPrintoutDesc", 2);
        jo["version"] = version;
        jo["xa0"] = ar.ReadMfcString();
        jo["xa4"] = ar.ReadMfcString();
        if (version >= 2) { jo["xa8"] = ar.ReadMfcString(); jo["xac"] = ar.ReadMfcString(); }
        return jo;
    }

    static JsonObject ReadCComponentListMethodPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_method_part"]   = ReadCEvaluationPart(ar);
        jo["c_component_list"]  = Dispatch(ar, "CComponentList");
        return jo;
    }

    static JsonObject ReadCPartMirror(IsodatArchive ar) => new JsonObject();

    static JsonObject ReadCTimeEventListMethodPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_method_part"]  = ReadCEvaluationPart(ar);
        jo["c_time_event_list"] = Dispatch(ar, "CTimeEventList");
        return jo;
    }

    static JsonObject ReadCContiniousFlowStandardizationMethodPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_method_part"] = ReadCEvaluationPart(ar);
        ar.ReadSchemaVersion("CContiniousFlowStandardizationMethodPart", 1); // discard
        jo["xa0"] = ar.ReadUInt32();
        jo["xa8"] = ar.ReadUInt32();
        jo["xac"] = ar.ReadUInt32();
        jo["xb0"] = ar.ReadMfcString();
        long flag = ar.ReadUInt32();
        if (flag != 0) jo["c_data_xb4"] = Dispatch(ar, "CData");
        return jo;
    }

    static JsonObject ReadCContiniousFlowStandardizationListMethodPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_method_part"] = ReadCEvaluationPart(ar);
        int version = ar.ReadSchemaVersion("CContiniousFlowStandardizationListMethodPart", 9);
        jo["version"] = version;
        jo["xac"] = ar.ReadUInt32();
        jo["xb8"] = ar.ReadUInt32();
        jo["xb4"] = ar.ReadUInt32();
        long flag1 = ar.ReadUInt32();
        if (flag1 != 0) jo["c_data_xa4"] = Dispatch(ar, "CData");
        long flag2 = ar.ReadUInt32();
        if (flag2 != 0) jo["c_data_xb0"] = Dispatch(ar, "CData");
        if (version > 2) jo["c_data_xdc"] = Dispatch(ar, "CData");
        if (version == 4) { Dispatch(ar); Dispatch(ar); } // discard
        if (version > 5)  jo["x100"] = ar.ReadUInt32();
        if (version > 6)  jo["x104"] = ar.ReadUInt32();
        if (version > 7)  jo["x108"] = ar.ReadMfcString();
        if (version > 8)  jo["x10c"] = ar.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCPrimaryStandardMethodPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_method_part"] = ReadCEvaluationPart(ar);
        int version = ar.ReadSchemaVersion("CPrimaryStandardMethodPart", 2);
        jo["version"] = version;
        jo["xa0"]     = ar.ReadMfcString();
        if (version == 1) ar.ReadUInt32(); // element_num, discard
        jo["c_data_xa8"] = Dispatch(ar, "CData");
        if (version > 1) jo["xb0"] = ar.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCSecondaryStandardMethodPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_method_part"] = ReadCEvaluationPart(ar);
        int version = ar.ReadSchemaVersion("CSecondaryStandardMethodPart", 3);
        jo["version"] = version;
        jo["xa0"] = ar.ReadMfcString();
        jo["xa4"] = ar.ReadMfcString();
        jo["xb0"] = ar.ReadUInt32();
        jo["c_data_xac"] = Dispatch(ar, "CData");
        if (version > 1) jo["xa8"] = ar.ReadUInt32();
        if (version > 2)
        {
            long flag = ar.ReadUInt32();
            if (flag != 0) jo["c_data_xb8"] = Dispatch(ar, "CData");
        }
        return jo;
    }

    static JsonObject ReadCConFloMethodPart(IsodatArchive ar)
    {
        ar.AddWarning("CConFloMethodPart: only CMethodPart parent + version read (stub)");
        var jo = new JsonObject();
        jo["p_c_method_part"] = ReadCEvaluationPart(ar);
        jo["version"] = ar.ReadSchemaVersion("CConFloMethodPart", 11);
        return jo;
    }

    static JsonObject ReadCICA_BasicMethodPart(IsodatArchive ar)
    {
        ar.AddWarning("CICA_BasicMethodPart: only CMethodPart parent + version read (stub)");
        var jo = new JsonObject();
        jo["p_c_method_part"] = ReadCEvaluationPart(ar);
        jo["version"] = ar.ReadSchemaVersion("CICA_BasicMethodPart", 12);
        return jo;
    }

    static JsonObject ReadCPeakFindMethodPart(IsodatArchive ar)
    {
        ar.AddWarning("CPeakFindMethodPart: only CMethodPart parent read (stub)");
        return new JsonObject { ["p_c_method_part"] = ReadCEvaluationPart(ar) };
    }

    static JsonObject ReadCSimplePeakFindMethodPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_peak_find_method_part"] = ReadCPeakFindMethodPart(ar);
        ar.ReadSchemaVersion("CSimplePeakFindMethodPart", 1); // discard
        jo["x128"] = ar.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCSimplePeakFindParameter(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_peak_find_parameter"] = ReadCPeakFindParameter(ar);
        ar.ReadSchemaVersion("CSimplePeakFindParameter", 1); // discard
        return jo;
    }

    // =======================================================================
    // CDeviceMethodPart chain
    // =======================================================================

    static JsonObject ReadCDeviceMethodPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_evaluation_part"] = ReadCEvaluationPart(ar);
        int version = ar.ReadSchemaVersion("CDeviceMethodPart", 2);
        jo["version"] = version;
        if (version >= 2)
        {
            var inner = DispatchObj(ar, "CBlockData");
            jo["p_c_block_data_inner"] = inner;
            int n = NObjects(inner);
            if (n > 0) jo["method_parts"] = DispatchN(ar, n);
        }
        else
        {
            int n = ar.ReadInt32();
            if (n > 0) jo["method_parts"] = DispatchN(ar, n);
        }
        return jo;
    }

    static JsonObject ReadCConFloDeviceMethodPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_device_method_part"] = ReadCDeviceMethodPart(ar);
        ar.ReadSchemaVersion("CConFloDeviceMethodPart", 1); // discard
        return jo;
    }

    static JsonObject ReadCMsDeviceMethodPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_device_method_part"] = ReadCDeviceMethodPart(ar);
        int version = ar.ReadSchemaVersion("CMsDeviceMethodPart", 3);
        jo["version"] = version;
        jo["xb0"] = ar.ReadUInt32();
        jo["xac"] = ar.ReadUInt8();
        jo["c_action_peak_center"] = Dispatch(ar, "CActionPeakCenter");
        if (version >= 2) jo["xb8"] = ar.ReadUInt32();
        if (version >= 3) jo["xbc"] = ar.ReadUInt8();
        return jo;
    }

    static JsonObject ReadCStandardDeviceMethodPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_device_method_part"] = ReadCDeviceMethodPart(ar);
        ar.ReadSchemaVersion("CStandardDeviceMethodPart", 1); // discard
        jo["xac"] = ar.ReadMfcString();
        jo["xb0"] = ar.ReadMfcString();
        jo["xb4"] = ar.ReadMfcString();
        jo["xb8"] = ar.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCGenericGcDeviceMethodPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_device_method_part"] = ReadCDeviceMethodPart(ar);
        ar.ReadSchemaVersion("CGenericGcDeviceMethodPart", 1); // discard
        ar.ReadUInt32(); // discarded
        jo["xb0"] = ar.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCFlashEA_DeviceMethodPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_generic_gc_device_method_part"] = ReadCGenericGcDeviceMethodPart(ar);
        ar.ReadSchemaVersion("CFlashEA_DeviceMethodPart", 2); // discard
        return jo;
    }

    static JsonObject ReadCMultiReferenceDeviceMethodPart(IsodatArchive ar)
    {
        ar.AddWarning("CMultiReferenceDeviceMethodPart: only CDeviceMethodPart parent + version read (stub)");
        var jo = new JsonObject();
        jo["p_c_device_method_part"] = ReadCDeviceMethodPart(ar);
        jo["version"] = ar.ReadSchemaVersion("CMultiReferenceDeviceMethodPart", 7);
        return jo;
    }

    // =======================================================================
    // CDeviceEvaluationPart chain
    // =======================================================================

    static JsonObject ReadCDeviceEvaluationPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_evaluation_part"] = ReadCEvaluationPart(ar);
        int version = ar.ReadSchemaVersion("CDeviceEvaluationPart", 2);
        jo["version"] = version;
        if (version >= 2)
        {
            var inner = DispatchObj(ar, "CBlockData");
            jo["p_c_block_data_inner"] = inner;
            int n = NObjects(inner);
            if (n > 0) jo["method_parts"] = DispatchN(ar, n);
        }
        else
        {
            int n = ar.ReadInt32();
            if (n > 0) jo["method_parts"] = DispatchN(ar, n);
        }
        return jo;
    }

    static JsonObject ReadCConFloDeviceEvaluationPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_device_evaluation_part"] = ReadCDeviceEvaluationPart(ar);
        ar.ReadSchemaVersion("CConFloDeviceEvaluationPart", 1); // discard
        return jo;
    }

    static JsonObject ReadCMsDeviceEvaluationPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_device_evaluation_part"] = ReadCDeviceEvaluationPart(ar);
        ar.ReadSchemaVersion("CMsDeviceEvaluationPart", 2); // discard
        return jo;
    }

    static JsonObject ReadCFlashEA_DeviceEvaluationPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_generic_gc_device_evaluation_part"] = ReadCDeviceEvaluationPart(ar);
        ar.ReadSchemaVersion("CFlashEA_DeviceEvaluationPart", 1); // discard
        return jo;
    }

    // =======================================================================
    // CEvalDataTransferPart chain
    // =======================================================================

    static JsonObject ReadCEvalDataTransferPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_eval_data_item_transfer_part"] = ReadCEvalDataItemTransferPart(ar);
        int version = ar.ReadSchemaVersion("CEvalDataTransferPart", 2);
        jo["version"] = version;
        if (version >= 1)
        {
            long n = ar.ReadUInt32();
            if (n > 0) jo["xc0_raw"] = Convert.ToBase64String(ar.ReadBytes((int)n));
        }
        if (version >= 2) jo["c_block_data_xc8"] = Dispatch(ar, "CBlockData");
        return jo;
    }

    static JsonObject ReadCEvalDataDWORDTransferPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_eval_data_transfer_part"] = ReadCEvalDataTransferPart(ar);
        ar.ReadSchemaVersion("CEvalDataDWORDTransferPart", 1); // discard
        return jo;
    }

    static JsonObject ReadCEvalDataSecStdTransferPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_eval_data_dword_transfer_part"] = ReadCEvalDataDWORDTransferPart(ar);
        int version = ar.ReadSchemaVersion("CEvalDataSecStdTransferPart", 2);
        jo["version"]       = version;
        jo["standard_name"] = ar.ReadMfcString();
        if (version >= 2)
        {
            jo["is_calculated"]    = ar.ReadUInt32();
            jo["calculated_value"] = ar.ReadDouble();
        }
        return jo;
    }

    static JsonObject ReadCEvalDataStringTransferPart(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_eval_data_transfer_part"] = ReadCEvalDataTransferPart(ar);
        ar.ReadSchemaVersion("CEvalDataStringTransferPart", 1); // discard
        long n = ar.ReadUInt32();
        jo["data_string"] = n > 0
            ? Encoding.Latin1.GetString(ar.ReadBytes((int)n))
            : "";
        return jo;
    }

    // =======================================================================
    // Peak stubs
    // =======================================================================

    static JsonObject ReadCGCPeak(IsodatArchive ar)
    {
        ar.AddWarning("CGCPeak: CGCBGDData parent Serialize unknown, returning empty");
        return new JsonObject();
    }

    static JsonObject ReadCSPeak(IsodatArchive ar)
    {
        ar.AddWarning("CSPeak: parent class and Serialize unknown, returning empty");
        return new JsonObject();
    }

    // =======================================================================
    // Script / Dynamic External variable classes
    // CScrHeadLine, CScrNumber: CData-derived; CScrBase pattern (v=2):
    //   CData + version + string + int + int + WriteObject + WriteObject +
    //   int + int + WriteObject(value)
    // CDynExternal: CData-derived; complex own fields (reverse-engineered from v=2 files)
    // CNumericValue: no parent; double + int + WriteObject(descriptor)
    // =======================================================================

    // Helper: shared header common to CScrHeadLine and CScrNumber
    static void ReadScrBase(IsodatArchive ar, JsonObject jo)
    {
        jo["p_c_data"] = ReadCData(ar);
        int version    = ar.ReadSchemaVersion("CScrBase", 2);
        jo["version"]  = version;
        jo["x9c"]      = ar.ReadMfcString();   // headline / description
        jo["xa0"]      = ar.ReadInt32();
        jo["xa4"]      = ar.ReadInt32();
        jo["xa8"]      = Dispatch(ar);          // optional WriteObject (null in observed files)
        jo["xac"]      = Dispatch(ar);          // optional WriteObject (null in observed files)
        jo["xb0"]      = ar.ReadInt32();
        jo["xb4"]      = ar.ReadInt32();
    }

    static JsonObject ReadCScrHeadLine(IsodatArchive ar)
    {
        var jo = new JsonObject();
        ReadScrBase(ar, jo);
        jo["c_dyn_external"] = Dispatch(ar);   // CDynExternal WriteObject
        return jo;
    }

    static JsonObject ReadCScrNumber(IsodatArchive ar)
    {
        var jo = new JsonObject();
        ReadScrBase(ar, jo);
        jo["c_numeric_value"] = Dispatch(ar);  // CNumericValue WriteObject
        return jo;
    }

    // CDynExternal (CData-derived) — structure reverse-engineered from v=2 binary.
    // The 130 bytes of own fields after CData + version:
    //   empty string + int(type) + 4×int(0) + int + 7×uint16(descriptors) +
    //   string(category) + string(unit) + int(-1) + string(formula) + string(name) +
    //   int + uint16 + int + string(time_category) + int(precision)
    static JsonObject ReadCDynExternal(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["p_c_data"] = ReadCData(ar);
        int version    = ar.ReadSchemaVersion("CDynExternal", 4);
        jo["version"]  = version;
        jo["x9c"]      = ar.ReadMfcString();   // empty in observed files
        jo["xa0"]      = ar.ReadInt32();        // type code
        jo["xa4"]      = ar.ReadInt32();
        jo["xa8"]      = ar.ReadInt32();
        jo["xac"]      = ar.ReadInt32();
        jo["xb0"]      = ar.ReadInt32();
        jo["xb4"]      = ar.ReadInt32();        // = 1
        // 7 descriptor uint16 values
        var desc = new JsonArray();
        for (int i = 0; i < 7; i++) desc.Add(ar.ReadUInt16());
        jo["x_desc"]   = desc;
        jo["xf0"]      = ar.ReadMfcString();   // category (e.g. "parameters")
        jo["xf4"]      = ar.ReadMfcString();   // unit (empty)
        jo["xf8"]      = ar.ReadInt32();        // limit (-1)
        jo["xfc"]      = ar.ReadMfcString();   // formula (empty)
        jo["x100"]     = ar.ReadMfcString();   // name (empty)
        int x104       = ar.ReadInt32();
        jo["x104"]     = x104;
        // Trailing time-parameter section only present when xf8 == -1 (no numeric limit)
        if (jo["xf8"]!.GetValue<int>() < 0)
        {
            jo["x108"]  = ar.ReadUInt16();
            jo["x10a"]  = ar.ReadInt32();
            jo["x110"]  = ar.ReadMfcString();
            jo["x114"]  = ar.ReadInt32();
        }
        return jo;
    }

    // CNumericValue — no CData parent; stores a numeric value with a descriptor object.
    static JsonObject ReadCNumericValue(IsodatArchive ar)
    {
        var jo = new JsonObject();
        jo["value"]    = ar.ReadDouble();       // IEEE 754 double
        jo["x08"]      = ar.ReadInt32();        // some flag/type
        jo["c_descriptor"] = Dispatch(ar);      // descriptor CData subclass
        return jo;
    }

    // =======================================================================
    // CShrinkInfo (not CData/CBlockData derived)
    // =======================================================================

    static JsonObject ReadCShrinkInfo(IsodatArchive ar)
    {
        var jo = new JsonObject();
        int version  = ar.ReadSchemaVersion("CShrinkInfo", 2);
        jo["version"] = version;
        int n        = ar.ReadInt32();
        jo["n_items"] = n;
        if (n > 0)
        {
            var items = new JsonArray();
            for (int i = 0; i < n; i++)
                items.Add(new JsonObject { ["col_idx"] = ar.ReadInt32(), ["width"] = ar.ReadInt32() });
            jo["items"] = items;
        }
        return jo;
    }

    // =======================================================================
    // CContiniousFlowBlockData (top-level DXF object)
    // =======================================================================

    public static JsonObject ReadCContiniousFlowBlockData(IsodatArchive ar)
    {
        var jo = new JsonObject();

        // Parent: CAcquistionBaseBlockData = CBlockData (inline, no CRuntimeClass header)
        jo["p_c_acquistion_base_block_data"] = ReadCBlockData(ar);

        jo["c_measurment_infos"]  = Dispatch(ar, "CMeasurmentInfos");
        jo["c_measurment_errors"] = Dispatch(ar, "CMeasurmentErrors");

        // CBlockData wrapper (n_objects=1) → CPlotSettings
        //   CPlotSettings.p_c_block_data has 5 CWinSettings children
        //   Each CWinSettings.p_c_block_data has N CGasSettings children
        //     Each CGasSettings.p_c_block_data has N CTraceSettings children
        //     + CPkDataItemList (N CPeakDataItem) + optional CShrinkInfo
        //   CWinSettings then has CViewColors → CWinColor (8 CTraceLinCol) + CGridColors + CAxisPara (CTraceLinCol)
        var plotWrapper = DispatchObj(ar, "CBlockData");
        ValidateBlockValue(plotWrapper, "Plot Settings");
        jo["c_plot_settings"] = Dispatch(ar, "CPlotSettings");

        // CBlockData wrapper (n_objects=N) → N × CRawData
        //   Each CRawData: CGCData (inline) → CEvalGCData → CAllMoleculeWeights
        //                  + CStringArray (v>3) + CIntegrationUnitGasConfPart (v>4, flag-gated)
        var rawWrapper = DispatchObj(ar, "CBlockData");
        ValidateBlockValue(rawWrapper, "RawDataBlock");
        int nRaw = NObjects(rawWrapper);
        jo["c_raw_data"] = DispatchN(ar, nRaw, "CRawData");

        // Same structure as RawDataBlock — same N, same CRawData reader
        var origWrapper = DispatchObj(ar, "CBlockData");
        ValidateBlockValue(origWrapper, "OrigDataBlock");
        jo["c_original_data"] = DispatchN(ar, nRaw, "CRawData");

        // CBlockData wrapper (n_objects=0) — "Calculated H3 Factor"
        var h3Wrapper = DispatchObj(ar, "CBlockData");
        ValidateBlockValue(h3Wrapper, "Calculated H3 Factor");
        jo["c_h3_factor_block"] = h3Wrapper;

        // CBlockData wrapper (n_objects=0) — "Prim Std"
        var primWrapper = DispatchObj(ar, "CBlockData");
        ValidateBlockValue(primWrapper, "Prim Std");
        jo["c_prim_std_block"] = primWrapper;

        // CBlockData wrapper (n_objects=1) → CMethod
        var methodWrapper = DispatchObj(ar, "CBlockData");
        ValidateBlockValue(methodWrapper, "Method");
        jo["c_method"] = Dispatch(ar, "CMethod");

        return jo;
    }

    static void ValidateBlockValue(JsonObject block, string expected)
    {
        string? actual = block["p_c_data"]?["value"]?.GetValue<string>();
        if (actual != expected)
            throw new InvalidDataException(
                $"Expected CBlockData value '{expected}', got '{actual}'");
    }

    // =======================================================================
    // CScanStorage (.scn top-level object, stub)
    // =======================================================================

    public static JsonObject ReadCScanStorage(IsodatArchive ar)
    {
        ar.AddWarning("CScanStorage: not yet implemented for this parser");
        return new JsonObject();
    }

    // =======================================================================
    // Helpers
    // =======================================================================

    static JsonArray ReadIntArray(IsodatArchive ar, int n)
    {
        var arr = new JsonArray();
        for (int i = 0; i < n; i++) arr.Add(ar.ReadInt32());
        return arr;
    }
}
