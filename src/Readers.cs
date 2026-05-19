using System.Text;
using System.Text.Json.Nodes;

namespace IsodatReader;

// ---------------------------------------------------------------------------
// All class reader functions for isodat binary files (.dxf, .scn).
//
// Naming conventions:
//   - ReadCXxx()        called directly for inline parent-class serialization
//   - Dispatch(isofile)      called when object was written via WriteObject()
//                       (reads CRuntimeClass header first, then dispatches)
//   - "parent" keys    parent class data embedded inline
//   - "c_xxx" keys      member objects written via WriteObject
// ---------------------------------------------------------------------------
static class Readers
{
    // =======================================================================
    // Partial-result tracking (thread-local stack, one slot per Dispatch frame)
    // =======================================================================

    [ThreadStatic] static Stack<JsonObject?>? _partialStack;
    static Stack<JsonObject?> PartialStack => _partialStack ??= new();

    // Called once at the top of any reader that wants partial-result capture.
    // Since jo is a reference, the stack slot always reflects the current state.
    static void TrackPartial(JsonObject jo)
    {
        if (PartialStack.Count > 0) { PartialStack.Pop(); PartialStack.Push(jo); }
    }

    // =======================================================================
    // Registry: class name → reader function
    // =======================================================================

    static readonly Dictionary<string, Func<IsodatFile, JsonObject>> _registry;

    static Readers()
    {
        _registry = new(StringComparer.Ordinal)
        {
            // --- CFileHeader ---
            ["CFileHeader"] = ReadCFileHeader,

            // --- CData chain ---
            ["CData"] = ReadCData,
            ["CCalibrationPoint"] = ReadCCalibrationPoint,
            ["CMolecule"] = ReadCMolecule,
            ["CTimeObject"] = ReadCTimeObject,
            ["CISLScriptMessageData"] = ReadCISLScriptMessageData,
            ["CComponent"] = ReadCComponent,
            ["CEvalIntegrationUnitHWInfo"] = ReadCEvalIntegrationUnitHWInfo,
            ["CTraceSettings"] = ReadCTraceSettings,
            ["CEvalDataItemTransferPart"] = ReadCEvalDataItemTransferPart,
            ["CPeakDataItem"] = ReadCPeakDataItem,
            ["CWinColor"] = ReadCWinColor,
            ["CTraceLinCol"] = ReadCTraceLinCol,
            ["CGridColors"] = ReadCGridColors,
            ["CAxisPara"] = ReadCAxisPara,
            ["CH3FactorResult"] = ReadCH3FactorResult,
            ["CApplicationData"] = ReadCApplicationData,
            ["CResultForGas"] = ReadCResultForGas,
            ["CPeakFindParameter"] = ReadCPeakFindParameter,
            ["CMRI_DilutionList"] = ReadCMRI_DilutionList,

            // --- CSimple chain ---
            ["CSimple"] = ReadCSimple,
            ["CStr"] = ReadCStr,
            ["CDword"] = ReadCDword,
            ["CPeakCenterOffset"] = ReadCDword,
            ["CBinary"] = ReadCBinary,

            // --- CBlockData chain ---
            ["CBlockData"] = ReadCBlockData,
            ["CAcquistionBaseBlockData"] = ReadCBlockData,
            ["CPort"] = ReadCBlockData,
            ["CDataIndex"] = ReadCDataIndex,
            ["CCalibration"] = ReadCCalibration,
            ["CVisualisationData"] = ReadCVisualisationData,
            ["CGasConfiguration"] = ReadCGasConfiguration,
            ["CMeasurmentInfos"] = ReadCMeasurmentInfos,
            ["CMeasurmentErrors"] = ReadCMeasurmentErrors,
            ["CPlotSettings"] = ReadCPlotSettings,
            ["CWinSettings"] = ReadCWinSettings,
            ["CViewColors"] = ReadCViewColors,
            ["CGasSettings"] = ReadCGasSettings,
            ["CPkDataItemList"] = ReadCPkDataItemList,
            ["CAllMoleculeWeights"] = ReadCAllMoleculeWeights,
            ["CMethod"] = ReadCMethod,
            ["CConfiguration"] = ReadCConfiguration,
            ["CComponentList"] = ReadCComponentList,
            ["CParsedEvaluationStringArray"] = ReadCParsedEvaluationStringArray,
            ["CResultArray"] = ReadCResultArray,
            ["CActionScript"] = ReadCActionScript,
            ["CGCPeakList"] = ReadCGCPeakList,
            ["CVisualisationDialogNamesBlockData"] = ReadCVisualisationDialogNamesBlockData,
            ["CEvalDataItemListTransferPart"] = ReadCEvalDataItemListTransferPart,
            ["CEvalIntegrationUnitHWInfoStore"] = ReadCEvalDataItemListTransferPart,
            ["CEvalIntegrationUnitHWInfoList"] = ReadCEvalDataItemListTransferPart,

            // --- CDevice chain ---
            ["CDevice"] = ReadCDevice,
            ["CActiveDevice"] = ReadCActiveDevice,
            ["CActivePort"] = ReadCActivePort,
            ["CMsDevice"] = ReadCMsDevice,
            ["CGenericGcDevice"] = ReadCGenericGcDevice,
            ["CFlashEA_Device"] = ReadCFlashEA_Device,
            ["CConFloDevice"] = ReadCActiveDevice,
            ["CMultiReferenceDevice"] = ReadCActiveDevice,
            ["CUserDevice"] = ReadCActiveDevice,

            // --- IsoGCEvalData / CEvalDataStorage chain ---
            ["IsoGCEvalData"] = ReadIsoGCEvalData,
            ["CGCData"] = ReadCGCData,
            ["CRawData"] = ReadCRawData,
            ["CEvalDataStorage"] = ReadCEvalDataStorage,
            ["CEvalFakeData"] = ReadCEvalFakeData,
            ["CEvalGCData"] = ReadCEvalGCData,

            // --- CBasicInterface chain (= CData) ---
            ["CBasicInterface"] = ReadCData,
            ["CGasConfPart"] = ReadCData,
            ["CFinniganInterface"] = ReadCFinniganInterface,
            ["CGpibInterface"] = ReadCGpibInterface,

            // --- CTransferPart chain ---
            ["CTransferPart"] = ReadCTransferPart,
            ["CAdcTransferPart"] = ReadCAdcTransferPart,
            ["CDioTransferPart"] = ReadCAdcTransferPart,
            ["CDacTransferPart"] = ReadCAdcTransferPart,
            ["CBasicHvTransferPart"] = ReadCAdcTransferPart,
            ["CCalculatingDacTransferPart"] = ReadCAdcTransferPart,
            ["CScaleHvTransferPart"] = ReadCAdcTransferPart,
            ["CMagnetCurrentTransferPart"] = ReadCMagnetCurrentTransferPart,

            // --- CGasConfPart chain ---
            ["CIntegrationUnitGasConfPart"] = ReadCIntegrationUnitGasConfPart,
            ["CChannelGasConfPart"] = ReadCChannelGasConfPart,

            // --- CBasicScan (CData-derived) ---
            ["CBasicScan"] = ReadCBasicScan,

            // --- CScanPart chain ---
            ["CScanPart"] = ReadCScanPart,
            ["CClockScanPart"] = ReadCClockScanPart,
            ["CScaleHvScanPart"] = ReadCScaleHvScanPart,
            ["CMagnetCurrentScanPart"] = ReadCMagnetCurrentScanPart,
            ["CIntegrationUnitScanPart"] = ReadCIntegrationUnitScanPart,

            // --- CHardwarePart chain ---
            ["CHardwarePart"] = ReadCHardwarePart,
            ["CCupHardwarePart"] = ReadCCupHardwarePart,
            ["CChannelHardwarePart"] = ReadCChannelHardwarePart,
            ["CScaleHardwarePart"] = ReadCScaleHardwarePart,
            ["CClockHardwarePart"] = ReadCClockHardwarePart,
            ["CIntegrationUnitHardwarePart"] = ReadCIntegrationUnitHardwarePart,
            ["CDacHardwarePart"] = ReadCDacHardwarePart,
            ["CScaleHvHardwarePart"] = ReadCScaleHvHardwarePart,
            ["CMagnetCurrentHardwarePart"] = ReadCMagnetCurrentHardwarePart,

            // --- CPlotInfo / CTraceInfo (.scn) ---
            ["CPlotInfo"] = ReadCPlotInfo,
            ["CTraceInfo"] = ReadCTraceInfo,
            ["CTraceInfoEntry"] = ReadCTraceInfoEntry,
            ["CPlotRange"] = ReadCPlotRangeObj,

            // --- CStringArray ---
            ["CStringArray"] = ReadCStringArray,
            ["CParsedEvaluationString"] = ReadCParsedEvaluationString,

            // --- CAction chain ---
            ["CAction"] = ReadCAction,
            ["CActionPeakCenter"] = ReadCActionPeakCenter,
            ["CActionHwTransferContainer"] = ReadCActionHwTransferContainer,
            ["CActionSubScript"] = ReadCActionSubScript,
            ["CDelay"] = ReadCDelay,
            ["CActionInterpreter"] = ReadCActionInterpreter,
            ["CMethodSwitcher"] = ReadCMethodSwitcher,
            ["CTimeEventList"] = ReadCTimeEventList,

            // --- CMethodPart / CEvaluationPart chain ---
            ["CEvaluationPart"] = ReadCEvaluationPart,
            ["CMethodPart"] = ReadCEvaluationPart,
            ["CMethodPrintoutDesc"] = ReadCMethodPrintoutDesc,
            ["CComponentListMethodPart"] = ReadCComponentListMethodPart,
            ["CPartMirror"] = ReadCPartMirror,
            ["CTimeEventListMethodPart"] = ReadCTimeEventListMethodPart,
            ["CContiniousFlowStandardizationMethodPart"] = ReadCContiniousFlowStandardizationMethodPart,
            ["CContiniousFlowStandardizationListMethodPart"] = ReadCContiniousFlowStandardizationListMethodPart,
            ["CPrimaryStandardMethodPart"] = ReadCPrimaryStandardMethodPart,
            ["CSecondaryStandardMethodPart"] = ReadCSecondaryStandardMethodPart,
            ["CConFloMethodPart"] = ReadCConFloMethodPart,
            ["CICA_BasicMethodPart"] = ReadCICA_BasicMethodPart,
            ["CPeakFindMethodPart"] = ReadCPeakFindMethodPart,
            ["CSimplePeakFindMethodPart"] = ReadCSimplePeakFindMethodPart,
            ["CSimplePeakFindParameter"] = ReadCSimplePeakFindParameter,

            // --- CDeviceMethodPart chain ---
            ["CDeviceMethodPart"] = ReadCDeviceMethodPart,
            ["CConFloDeviceMethodPart"] = ReadCConFloDeviceMethodPart,
            ["CMsDeviceMethodPart"] = ReadCMsDeviceMethodPart,
            ["CStandardDeviceMethodPart"] = ReadCStandardDeviceMethodPart,
            ["CGenericGcDeviceMethodPart"] = ReadCGenericGcDeviceMethodPart,
            ["CFlashEA_DeviceMethodPart"] = ReadCFlashEA_DeviceMethodPart,
            ["CMultiReferenceDeviceMethodPart"] = ReadCMultiReferenceDeviceMethodPart,
            ["CActiveDeviceMethodPart"] = ReadCDeviceMethodPart,

            // --- CDeviceEvaluationPart chain ---
            ["CDeviceEvaluationPart"] = ReadCDeviceEvaluationPart,
            ["CConFloDeviceEvaluationPart"] = ReadCConFloDeviceEvaluationPart,
            ["CMsDeviceEvaluationPart"] = ReadCMsDeviceEvaluationPart,
            ["CGenericGcDeviceEvaluationPart"] = ReadCDeviceEvaluationPart,
            ["CFlashEA_DeviceEvaluationPart"] = ReadCFlashEA_DeviceEvaluationPart,
            ["CMultiReferenceDeviceEvaluationPart"] = ReadCDeviceEvaluationPart,

            // --- CEvalDataTransferPart chain ---
            ["CEvalDataTransferPart"] = ReadCEvalDataTransferPart,
            ["CEvalDataDWORDTransferPart"] = ReadCEvalDataDWORDTransferPart,
            ["CEvalDataSecStdTransferPart"] = ReadCEvalDataSecStdTransferPart,
            ["CEvalDataStringTransferPart"] = ReadCEvalDataStringTransferPart,
            ["CEvalDataIntTransferPart"] = ReadCEvalDataTransferPart,
            ["CEvalDataDoubleTransferPart"] = ReadCEvalDataTransferPart,

            // --- Peak stubs ---
            ["CGCPeak"] = ReadCGCPeak,
            ["CSPeak"] = ReadCSPeak,

            // --- Script / Dynamic External variable classes ---
            ["CScrHeadLine"] = ReadCScrHeadLine,
            ["CScrNumber"] = ReadCScrNumber,
            ["CDynExternal"] = ReadCDynExternal,
            ["CNumericValue"] = ReadCNumericValue,

            // --- Misc stand-alone ---
            ["CShrinkInfo"] = ReadCShrinkInfo,

            // --- CContiniousFlowBlockData (top-level DXF object) ---
            ["CContiniousFlowBlockData"] = ReadCContiniousFlowBlockData,
            ["CScanStorage"] = ReadCScanStorage,
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
    public static JsonNode? Dispatch(IsodatFile isofile, string? expected = null)
    {
        long headerPos = isofile.Position;
        string? className = isofile.ReadCRuntimeClass(expected);
        if (className is null) return null;   // MFC NULL WriteObject
        if (!_registry.TryGetValue(className, out var reader))
            throw new InvalidDataException(
                $"No reader registered for class '{className}' (header at 0x{headerPos:x})");

        isofile.PushContainer(isofile.ObjectLog[^1].ObjIdx);
        PartialStack.Push(null);
        bool popped = false;
        try
        {
            return reader(isofile);
        }
        catch (IsodatParseException ipe)
        {
            popped = true;
            var partial = PartialStack.Pop();
            var enriched = ipe.PrependPath(className, headerPos);
            enriched.PartialResult = partial ?? ipe.PartialResult;  // prefer outermost partial
            throw enriched;
        }
        catch (Exception ex)
        {
            popped = true;
            var partial = PartialStack.Pop();
            var exc = new IsodatParseException(className, headerPos, isofile.Position, ex);
            exc.PartialResult = partial;
            throw exc;
        }
        finally
        {
            isofile.PopContainer();
            if (!popped) PartialStack.Pop();
        }
    }

    /// <summary>
    /// Dispatch to a reader for an already-consumed class name (no header read).
    /// </summary>
    public static JsonObject DispatchKnown(IsodatFile isofile, string className)
    {
        if (!_registry.TryGetValue(className, out var reader))
            throw new InvalidDataException(
                $"No reader registered for class '{className}'");
        long headerPos = isofile.Position;
        try
        {
            return reader(isofile);
        }
        catch (IsodatParseException ipe)
        {
            throw ipe.PrependPath(className, headerPos);
        }
        catch (Exception ex)
        {
            throw new IsodatParseException(className, headerPos, isofile.Position, ex);
        }
    }

    /// <summary>
    /// Like Dispatch, but asserts non-null (throws on MFC NULL WriteObject).
    /// Use when the object is structurally required.
    /// </summary>
    static JsonObject DispatchObj(IsodatFile isofile, string? expected = null)
    {
        var node = Dispatch(isofile, expected);
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
    static JsonNode? DispatchFully(IsodatFile isofile)
    {
        var node = Dispatch(isofile);
        if (node is JsonObject jo)
        {
            int n = jo["n_objects"]?.GetValue<int>() ?? 0;
            if (n > 0)
            {
                var sub = new JsonArray();
                for (int i = 0; i < n; i++)
                    sub.Add(DispatchFully(isofile));
                jo["children"] = sub;
            }
        }
        return node;
    }

    // Convenience: dispatch N objects and collect into JsonArray
    static JsonArray DispatchN(IsodatFile isofile, int n, string? expected = null)
    {
        var arr = new JsonArray();
        for (int i = 0; i < n; i++)
        {
            arr.Add(Dispatch(isofile, expected));
        }
        return arr;
    }

    // Helper: get n_objects from a CBlockData-like JsonObject
    static int NObjects(JsonObject jo) =>
        jo["n_objects"]?.GetValue<int>() ?? 0;

    static void ValidateBlockNObjects(JsonObject block, int expected)
    {
        int n = NObjects(block);
        if (n != expected)
            throw new InvalidDataException($"expected {expected} children, got {n}");
    }

    static void ValidateBlockValue(JsonObject block, string expected)
    {
        string? actual = block["parent"]?["value"]?.GetValue<string>();
        if (actual != expected)
            throw new InvalidDataException(
                $"Expected CBlockData value '{expected}', got '{actual}'");
    }

    // =======================================================================
    // CFileHeader
    // =======================================================================

    static JsonObject ReadCFileHeader(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        jo["magic"] = isofile.ReadInt32();
        int version = isofile.ReadSchemaVersion("CFileHeader", 6);
        jo["v"] = version;
        jo["runtime_class"] = isofile.ReadMfcString();
        jo["xac"] = isofile.ReadMfcString();

        if (version >= 2) jo["xb0"] = isofile.ReadInt32();

        if (version >= 3)
        {
            jo["parent"] = ReadCBlockData(isofile);
            jo["c_time_object"] = Dispatch(isofile, "CTimeObject");
            jo["c_str"] = Dispatch(isofile, "CStr");
        }

        if (version >= 4)
        {
            jo["c_data_index"] = Dispatch(isofile, "CDataIndex");
        }

        if (version >= 5)
        {
            jo["isodat_version"] = isofile.ReadMfcString();
            if (version >= 6)
                jo["isodat_minor_version"] = isofile.ReadMfcString();
        }

        return jo;
    }

    // =======================================================================
    // CData chain
    // =======================================================================

    public static JsonObject ReadCData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        int version = isofile.ReadSchemaVersion("CData", 3);
        jo["v"] = version;
        jo["app_id"] = isofile.ReadUInt16();
        jo["label"] = isofile.ReadMfcString();
        jo["value"] = isofile.ReadMfcString();
        if (version >= 3) jo["flags"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCCalibrationPoint(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        int version = isofile.ReadSchemaVersion("CCalibrationPoint", 3);
        jo["v"] = version;
        jo["x94"] = isofile.ReadInt32();
        jo["x98"] = isofile.ReadDouble();
        if (version >= 3)
        {
            jo["x_a0"] = isofile.ReadDouble();
            jo["x_a8"] = isofile.ReadDouble();
        }
        return jo;
    }

    static JsonObject ReadCMolecule(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CMolecule", 1);
        jo["molecule"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCTimeObject(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CTimeObject", 1);
        jo["datetime"] = isofile.ReadTimestamp();
        return jo;
    }

    static JsonObject ReadCISLScriptMessageData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CISLScriptMessageData", 1);
        jo["display_text"] = isofile.ReadMfcString();
        jo["source_class"] = isofile.ReadMfcString();
        // x9c = 0xFFFFFFFF (-1) and xa0 = 0x00000000 as plain int32 fields.
        // These bytes superficially resemble an MFC new-class header (ff ff ff ff 00 00)
        // but they are raw serialized data, not WriteObject calls.
        jo["x9c"] = isofile.ReadInt32();
        jo["xa0"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCComponent(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        isofile.ReadSchemaVersion("CComponent", 1); // discard
        jo["x94"] = isofile.ReadInt32();
        jo["x98"] = isofile.ReadInt32();
        jo["xa0"] = isofile.ReadInt32();
        jo["xa4"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCEvalIntegrationUnitHWInfo(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        isofile.ReadSchemaVersion("CEvalIntegrationUnitHWInfo", 1); // discard
        jo["mass"] = isofile.ReadDouble();
        jo["channel"] = isofile.ReadInt32();
        jo["resistor"] = isofile.ReadDouble();
        jo["cup"] = isofile.ReadInt32();
        return jo;
    }

    // CTraceSettings: Serialize does NOT call CData::Serialize
    static JsonObject ReadCTraceSettings(IsodatFile isofile)
    {
        var jo = new JsonObject();
        int version = isofile.ReadSchemaVersion("CTraceSettings", 4);
        jo["v"] = version;
        jo["nominator_trace_idx"] = isofile.ReadInt32();
        jo["divisor_trace_idx"] = isofile.ReadInt32();
        jo["source_trace_idx"] = isofile.ReadInt32();
        if (version >= 2)
        {
            jo["trace_fac_a"] = isofile.ReadDouble();
            jo["trace_fac_b"] = isofile.ReadDouble();
        }
        if (version >= 3) jo["enabled"] = isofile.ReadInt32();
        if (version >= 4)
        {
            jo["nominator_mass"] = isofile.ReadInt32();
            jo["divisor_mass"] = isofile.ReadInt32();
            jo["eval_list"] = isofile.ReadMfcString();
            jo["eval_name"] = isofile.ReadMfcString();
            jo["xc4"] = isofile.ReadInt32();
        }
        return jo;
    }

    static JsonObject ReadCEvalDataItemTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        int version = isofile.ReadSchemaVersion("CEvalDataItemTransferPart", 8);
        jo["v"] = version;
        jo["id"] = isofile.ReadMfcString();
        jo["name"] = isofile.ReadMfcString();
        jo["format"] = isofile.ReadMfcString();
        jo["gas_name"] = isofile.ReadMfcString();
        jo["element_name"] = isofile.ReadMfcString();
        if (version >= 2) jo["units"] = isofile.ReadMfcString();
        if (version >= 3) jo["info"] = isofile.ReadMfcString();
        if (version >= 5) jo["xb4"] = isofile.ReadInt32();
        if (version >= 6) jo["xb0"] = isofile.ReadMfcString();
        if (version >= 7) jo["xb8"] = isofile.ReadInt32();
        if (version >= 8) jo["ampere_calculation"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCPeakDataItem(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvalDataItemTransferPart(isofile);
        int version = isofile.ReadSchemaVersion("CPeakDataItem", 1);
        jo["v"] = version;
        isofile.ReadMfcString(); // ID recomputed at runtime, discard
        jo["xc0"] = isofile.ReadInt32();
        jo["xc4"] = isofile.ReadInt32();
        return jo;
    }

    // CWinColor: Serialize does NOT call CData::Serialize; has embedded CBlockData via WriteObject
    static JsonObject ReadCWinColor(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var blockData = DispatchObj(isofile, "CBlockData");
        jo["parent"] = blockData;
        int n = NObjects(blockData);
        jo["c_trace_lin_col"] = DispatchN(isofile, n, "CTraceLinCol");
        return jo;
    }

    // CTraceLinCol: Serialize does NOT call CData::Serialize
    static JsonObject ReadCTraceLinCol(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["line_color"] = isofile.ReadColor();
        jo["line_type"] = isofile.ReadInt32();
        jo["line_width"] = isofile.ReadInt32();
        return jo;
    }

    // CGridColors: Serialize does NOT call CData::Serialize; 9 COLORREF values
    static JsonObject ReadCGridColors(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["x94"] = isofile.ReadColor();
        jo["x98"] = isofile.ReadColor();
        jo["x9c"] = isofile.ReadColor();
        jo["xa0"] = isofile.ReadColor();
        jo["xa4"] = isofile.ReadColor();
        jo["xa8"] = isofile.ReadColor();
        jo["xac"] = isofile.ReadColor();
        jo["xb0"] = isofile.ReadColor();
        jo["xb4"] = isofile.ReadColor();
        return jo;
    }

    // CAxisPara: Serialize does NOT call CData::Serialize
    static JsonObject ReadCAxisPara(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["x94"] = isofile.ReadInt32();
        jo["c_trace_lin_col"] = Dispatch(isofile, "CTraceLinCol");
        return jo;
    }

    static JsonObject ReadCH3FactorResult(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        int version = isofile.ReadSchemaVersion("CH3FactorResult", 4);
        jo["v"] = version;
        jo["x98_x9c"] = isofile.ReadDouble();
        jo["xa0_xa4"] = isofile.ReadDouble();
        if (version >= 2) jo["xa8"] = isofile.ReadUInt32();
        if (version >= 3) { jo["xac"] = isofile.ReadMfcString(); jo["xb8"] = isofile.ReadInt32(); }
        if (version >= 4) { jo["xb0"] = isofile.ReadMfcString(); jo["xb4"] = isofile.ReadMfcString(); jo["xbc"] = isofile.ReadInt32(); }
        return jo;
    }

    static JsonObject ReadCApplicationData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        isofile.ReadSchemaVersion("CApplicationData", 2); // discard, no gating
        jo["x94"] = isofile.ReadUInt32();
        jo["x98"] = isofile.ReadUInt32();
        jo["x9c"] = isofile.ReadUInt32();
        jo["xa0"] = isofile.ReadUInt16();
        jo["xa4"] = isofile.ReadUInt32();
        jo["xa8"] = isofile.ReadUInt32();
        jo["xac"] = isofile.ReadUInt32();
        jo["xb0"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCResultForGas(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        isofile.ReadSchemaVersion("CResultForGas", 1); // discard
        jo["x94"] = isofile.ReadMfcString();
        jo["x98"] = isofile.ReadMfcString();
        jo["c_data_xa4"] = Dispatch(isofile, "CData");
        return jo;
    }

    static JsonObject ReadCPeakFindParameter(IsodatFile isofile)
    {
        isofile.AddWarning("CPeakFindParameter: only CData parent read (stub)");
        return new JsonObject { ["parent"] = ReadCData(isofile) };
    }

    static JsonObject ReadCMRI_DilutionList(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        int version = isofile.ReadSchemaVersion("CMRI_DilutionList", 1);
        jo["v"] = version;
        int n = isofile.ReadInt32();
        if (n > 0)
        {
            var items = new JsonArray();
            for (int i = 0; i < n; i++)
                items.Add(new JsonObject { ["a"] = isofile.ReadDouble(), ["b"] = isofile.ReadDouble() });
            jo["items"] = items;
        }
        return jo;
    }

    // =======================================================================
    // CSimple chain
    // =======================================================================

    public static JsonObject ReadCSimple(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["v"] = isofile.ReadSchemaVersion("CSimple", 2);
        jo["label"] = isofile.ReadMfcString();
        return jo;
    }

    public static JsonObject ReadCStr(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCSimple(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CStr", 2);
        jo["value"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCDword(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCSimple(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CDword", 2);
        jo["value"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCBinary(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCSimple(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CBinary", 2);
        int nBytes = isofile.ReadInt32();
        jo["n_bytes"] = nBytes;
        if (nBytes > 0)
            jo["data"] = Convert.ToBase64String(isofile.ReadBytes(nBytes));
        return jo;
    }

    // =======================================================================
    // CBlockData chain
    // =======================================================================

    public static JsonObject ReadCBlockData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CBlockData", 2);
        jo["n_objects"] = isofile.ReadInt32();
        return jo;
    }

    public static JsonObject ReadCDataIndex(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        ValidateBlockNObjects(block, 0);
        isofile.ReadInt32(); // trailing sentinel (always 1)
        jo["parent"] = block;
        return jo;
    }

    static JsonObject ReadCCalibration(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        int nObj = NObjects(block);
        if (nObj > 0)
            jo["c_calibration_point"] = DispatchN(isofile, nObj, "CCalibrationPoint");
        int version = isofile.ReadSchemaVersion("CCalibration", 5);
        jo["v"] = version;
        jo["x_a8"] = isofile.ReadUInt8();
        jo["x_ac"] = isofile.ReadMfcString();
        jo["x_b0"] = isofile.ReadTimestamp();
        if (version < 5) isofile.ReadDouble(); // legacy
        jo["x_bc"] = isofile.ReadInt32();
        if (version >= 3) jo["x_c0"] = isofile.ReadUInt8();
        if (version >= 4)
        {
            var splines = new JsonArray();
            bool cont = true;
            while (cont)
            {
                var spline = new JsonArray();
                for (int idx = 0; idx < 8; idx++)
                {
                    int n = isofile.ReadUInt16();
                    var vals = new JsonArray();
                    for (int j = 0; j < n; j++) vals.Add((JsonNode)isofile.ReadDouble());
                    spline.Add(new JsonObject { ["n"] = n, ["values"] = vals });
                }
                cont = isofile.ReadBool32();
                splines.Add(spline);
            }
            jo["splines"] = splines;
        }
        return jo;
    }

    static JsonObject ReadCVisualisationData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        ValidateBlockNObjects(block, 0);
        int version = isofile.ReadSchemaVersion("CVisualisationData", 8);
        jo["v"] = version;

        jo["x_a8"] = ReadIntArray(isofile, 4);
        jo["x_b8"] = ReadIntArray(isofile, 10);
        jo["x_e0"] = ReadIntArray(isofile, 10);

        if (version >= 2)
        {
            jo["font"] = isofile.ReadMfcString();
            jo["x10c"] = isofile.ReadMfcString();
            jo["x110"] = isofile.ReadMfcString();
            if (version >= 3)
            {
                jo["x120"] = isofile.ReadInt32();
                if (version >= 4)
                {
                    jo["x124"] = isofile.ReadInt32();
                    if (version >= 5)
                    {
                        jo["x148"] = isofile.ReadMfcString();
                        if (version >= 6)
                        {
                            jo["x11c"] = isofile.ReadInt32();
                            if (version >= 7)
                            {
                                jo["x128"] = isofile.ReadInt32();
                                if (version >= 8)
                                    jo["x12c"] = isofile.ReadInt32();
                            }
                        }
                    }
                }
            }
        }
        return jo;
    }

    static JsonObject ReadCGasConfiguration(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        jo["c_data"] = DispatchN(isofile, NObjects(block));
        int version = isofile.ReadSchemaVersion("CGasConfiguration", 3);
        jo["v"] = version;
        if (version >= 3) jo["timestamp"] = isofile.ReadTimestamp();
        return jo;
    }

    static JsonObject ReadCMeasurmentInfos(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        jo["c_isl_script_message_data"] = DispatchN(isofile, NObjects(block));
        isofile.ReadInt32(); // trailing sentinel (always 1)
        return jo;
    }

    static JsonObject ReadCMeasurmentErrors(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        jo["c_isl_script_message_data"] = DispatchN(isofile, NObjects(block));
        isofile.ReadInt32(); // trailing sentinel (always 1)
        return jo;
    }

    static JsonObject ReadCPlotSettings(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        jo["c_win_settings"] = DispatchN(isofile, NObjects(block));
        int version = isofile.ReadSchemaVersion("CPlotSettings", 5);
        jo["v"] = version;
        if (version >= 2) { jo["xb0"] = isofile.ReadMfcString(); jo["configuration_name"] = isofile.ReadMfcString(); }
        if (version >= 3) jo["peak_labelling"] = isofile.ReadInt32();
        if (version >= 4) jo["refresh_data_grid"] = isofile.ReadInt32();
        if (version >= 5) jo["ampere_calc_flag"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCWinSettings(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        jo["c_gas_settings"] = DispatchN(isofile, NObjects(block));
        int version = isofile.ReadSchemaVersion("CWinSettings", 4);
        jo["v"] = version;
        jo["min_val_a"] = isofile.ReadDouble();
        jo["min_val_b"] = isofile.ReadDouble();
        jo["max_val_a"] = isofile.ReadDouble();
        jo["max_val_b"] = isofile.ReadDouble();
        jo["min_perc_x"] = isofile.ReadInt32();
        jo["min_perc_y"] = isofile.ReadInt32();
        jo["max_perc_x"] = isofile.ReadInt32();
        jo["max_perc_y"] = isofile.ReadInt32();
        jo["min_perc_y_alt"] = isofile.ReadInt32();
        jo["max_perc_y_alt"] = isofile.ReadInt32();
        jo["trace_type"] = isofile.ReadInt32();
        jo["x10c"] = isofile.ReadInt32();
        jo["x110"] = isofile.ReadInt32();
        jo["x114"] = isofile.ReadInt32();
        jo["x118"] = isofile.ReadInt32();
        jo["c_view_colors"] = Dispatch(isofile, "CViewColors");
        if (version == 2)
        {
            isofile.AddWarning("CWinSettings v2: reading legacy object (untested)");
            Dispatch(isofile); // discard
        }
        if (version >= 4) jo["x128"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCViewColors(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        ValidateBlockNObjects(block, 3);
        jo["c_win_color"] = Dispatch(isofile, "CWinColor");
        jo["c_grid_colors"] = Dispatch(isofile, "CGridColors");
        jo["c_axis_para"] = Dispatch(isofile, "CAxisPara");
        jo["xa8"] = isofile.ReadColor();
        jo["xac"] = isofile.ReadColor();
        jo["xb0"] = isofile.ReadColor();
        jo["xb4"] = isofile.ReadColor();
        jo["xb8"] = isofile.ReadColor();
        return jo;
    }

    static JsonObject ReadCGasSettings(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        jo["c_trace_settings"] = DispatchN(isofile, NObjects(block));
        int version = isofile.ReadSchemaVersion("CGasSettings", 5);
        jo["v"] = version;
        jo["c_pk_data_item_list"] = Dispatch(isofile, "CPkDataItemList");
        if (version >= 2) jo["gas"] = isofile.ReadMfcString();
        if (version >= 3)
        {
            int hasShrink = isofile.ReadInt32();
            if (hasShrink != 0) jo["c_shrink_info"] = Dispatch(isofile, "CShrinkInfo");
        }
        if (version >= 4) jo["eval_list"] = isofile.ReadMfcString();
        if (version >= 5) jo["ampere_calc_flag"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCPkDataItemList(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        jo["c_peak_data_item"] = DispatchN(isofile, NObjects(block));
        jo["v"] = isofile.ReadSchemaVersion("CPkDataItemList", 1);
        jo["xa8"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCAllMoleculeWeights(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        if (NObjects(block) != 0)
            throw new InvalidDataException($"expected 0 children, got {NObjects(block)}");
        int version = isofile.ReadSchemaVersion("CAllMoleculeWeights", 2);
        jo["v"] = version;
        if (version >= 2)
        {
            isofile.ReadInt32(); // heap pointer snapshots, discard
            isofile.ReadInt32();
        }
        return jo;
    }

    static JsonObject ReadCMethod(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        int nChildren = NObjects(block);
        if (nChildren > 0)
        {
            var children = new JsonArray();
            for (int ci = 0; ci < nChildren; ci++)
            {
                children.Add(DispatchFully(isofile));
            }
            jo["children"] = children;
        }

        int version = isofile.ReadSchemaVersion("CMethod", 10);
        jo["v"] = version;
        jo["c_configuration"] = Dispatch(isofile);
        jo["x9c"] = isofile.ReadMfcString();
        jo["xa0"] = isofile.ReadMfcString();
        jo["xa4"] = isofile.ReadMfcString();

        // N CDeviceMethodPart objects (polymorphic — concrete subclass in stream)
        int nDeviceParts = isofile.ReadInt32();
        if (nDeviceParts > 0)
            jo["c_device_method_parts"] = DispatchN(isofile, nDeviceParts);

        if (version >= 2)
        {
            int nEvalParts = isofile.ReadInt32();
            if (nEvalParts > 0)
                jo["c_device_eval_parts"] = DispatchN(isofile, nEvalParts);
        }

        if (version >= 3) jo["acq_type"] = isofile.ReadInt32();
        if (version >= 4) jo["xc4"] = isofile.ReadMfcString();

        if (version >= 5)
        {
            int nSubMethods = isofile.ReadInt32();
            if (nSubMethods > 0)
                jo["sub_methods"] = DispatchN(isofile, nSubMethods, "CMethod");
        }

        if (version >= 6) jo["xd0"] = isofile.ReadMfcString();
        if (version >= 7) jo["xcc"] = isofile.ReadInt32();
        if (version >= 9) { jo["xd4"] = isofile.ReadInt32(); jo["xd8"] = isofile.ReadInt32(); }
        if (version >= 10) jo["xdc"] = isofile.ReadInt32();

        return jo;
    }

    static JsonObject ReadCConfiguration(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        int n = NObjects(block);
        if (n > 0) jo["children"] = DispatchN(isofile, n);
        int version = isofile.ReadSchemaVersion("CConfiguration", 7);
        jo["v"] = version;
        if (version >= 3) jo["xa8"] = isofile.ReadInt32();
        if (version >= 4) jo["xac"] = isofile.ReadInt32();
        if (version >= 5) jo["xb0"] = isofile.ReadMfcString();
        if (version >= 6) jo["xb4"] = isofile.ReadInt32();
        if (version >= 7) jo["xb8"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCComponentList(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        int n = NObjects(block);
        if (n > 0) jo["children"] = DispatchN(isofile, n);
        isofile.ReadSchemaVersion("CComponentList", 1); // discard
        return jo;
    }

    static JsonObject ReadCParsedEvaluationStringArray(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        // children are CParsedEvaluationString objects
        jo["children"] = DispatchN(isofile, NObjects(block));
        int version = isofile.ReadSchemaVersion("CParsedEvaluationStringArray", 4);
        jo["v"] = version;
        jo["xa8"] = isofile.ReadMfcString();
        if (version >= 2) jo["xb0"] = isofile.ReadUInt32();
        if (version >= 3) { jo["xb8"] = isofile.ReadUInt32(); jo["xbc"] = isofile.ReadUInt32(); }
        if (version >= 4) jo["xc0"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCResultArray(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        jo["children"] = DispatchN(isofile, NObjects(block));
        int version = isofile.ReadSchemaVersion("CResultArray", 2);
        jo["v"] = version;
        jo["xa8"] = isofile.ReadUInt32();
        if (version >= 2) jo["xac"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCActionScript(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        int version = isofile.ReadSchemaVersion("CActionScript", 5);
        jo["v"] = version;
        if (version >= 3) jo["c_application_data"] = Dispatch(isofile, "CApplicationData");
        if (version >= 4) jo["x168"] = isofile.ReadUInt32();
        if (version >= 5) { jo["x1c0"] = isofile.ReadMfcString(); jo["x1c4"] = isofile.ReadUInt32(); }
        return jo;
    }

    static JsonObject ReadCGCPeakList(IsodatFile isofile)
    {
        isofile.AddWarning("CGCPeakList: only CBlockData parent + version read (stub)");
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        jo["v"] = isofile.ReadSchemaVersion("CGCPeakList", 6);
        return jo;
    }

    static JsonObject ReadCVisualisationDialogNamesBlockData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        isofile.ReadSchemaVersion("CVisualisationDialogNamesBlockData", 1); // discard
        return jo;
    }

    static JsonObject ReadCEvalDataItemListTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        int n = NObjects(block);
        if (n > 0) jo["children"] = DispatchN(isofile, n);
        isofile.ReadSchemaVersion("CEvalDataItemListTransferPart", 1); // discard
        return jo;
    }

    // =======================================================================
    // CDevice chain
    // =======================================================================

    static JsonObject ReadCDevice(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile);
        jo["parent"] = block;
        int nChildren = NObjects(block);
        if (nChildren > 0)
        {
            var children = new JsonArray();
            for (int i = 0; i < nChildren; i++) children.Add(DispatchFully(isofile));
            jo["children"] = children;
        }
        int version = isofile.ReadSchemaVersion("CDevice", 5);
        jo["v"] = version;
        jo["xac"] = isofile.ReadUInt32();
        jo["xb0"] = isofile.ReadUInt32();
        if (version >= 3) jo["xa8"] = isofile.ReadUInt32();
        if (version >= 4) jo["xb4"] = isofile.ReadUInt32();
        if (version >= 5) jo["xb8"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCActiveDevice(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCDevice(isofile);
        int version = isofile.ReadSchemaVersion("CActiveDevice", 2);
        jo["v"] = version;
        if (version >= 2) jo["xec"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCActivePort(IsodatFile isofile)
    {
        var jo = new JsonObject();
        var block = ReadCBlockData(isofile); // CPort = CBlockData
        jo["parent"] = block;
        int nChildren = NObjects(block);
        if (nChildren > 0)
        {
            var children = new JsonArray();
            for (int i = 0; i < nChildren; i++) children.Add(DispatchFully(isofile));
            jo["children"] = children;
        }
        int version = isofile.ReadSchemaVersion("CActivePort", 2);
        jo["v"] = version;
        if (version >= 2) jo["xa8"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCMsDevice(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCActiveDevice(isofile);
        int version = isofile.ReadSchemaVersion("CMsDevice", 2);
        jo["v"] = version;
        jo["xfc"] = isofile.ReadUInt32();
        if (version >= 2) jo["x100"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCGenericGcDevice(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCActiveDevice(isofile);
        int version = isofile.ReadSchemaVersion("CGenericGcDevice", 2);
        jo["v"] = version;
        if (version >= 2) jo["xfc"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCFlashEA_Device(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCGenericGcDevice(isofile);
        isofile.ReadSchemaVersion("CFlashEA_Device", 1); // discard
        return jo;
    }

    // =======================================================================
    // IsoGCEvalData / CEvalDataStorage chain
    // =======================================================================

    static JsonObject ReadIsoGCEvalData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        jo["v"] = isofile.ReadSchemaVersion("IsoGCEvalData", 1);
        return jo;
    }

    static JsonObject ReadCGCData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["p_iso_gc_eval_data"] = ReadIsoGCEvalData(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CGCData", 1);
        jo["c_eval_gc_data"] = Dispatch(isofile, "CEvalGCData");
        return jo;
    }

    static JsonObject ReadCRawData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCGCData(isofile);
        int version = isofile.ReadSchemaVersion("CRawData", 5);
        jo["v"] = version;

        if (version <= 1) return jo;

        jo["complete_formula"] = isofile.ReadMfcString();
        jo["formula"] = isofile.ReadMfcString();
        int nMasses = isofile.ReadInt32();
        jo["n_masses"] = nMasses;
        if (nMasses > 0) jo["masses"] = ReadIntArray(isofile, nMasses);

        jo["c_all_molecule_weights"] = Dispatch(isofile, "CAllMoleculeWeights");

        if (version > 2) jo["x1048"] = isofile.ReadInt32();
        if (version > 3) jo["c_string_array"] = Dispatch(isofile, "CStringArray");
        if (version > 4)
        {
            jo["xf88"] = isofile.ReadInt32();
            int flag = isofile.ReadInt32();
            if (flag != 0)
                jo["c_integration_unit_gas_conf_part"] =
                    Dispatch(isofile, "CIntegrationUnitGasConfPart");
        }
        return jo;
    }

    // CEvalDataStorage: Serialize does NOT call CData/CBlockData
    static JsonObject ReadCEvalDataStorage(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["v"] = isofile.ReadSchemaVersion("CEvalDataStorage", 1);
        int nBytes = isofile.ReadInt32();
        jo["n_bytes"] = nBytes;
        if (nBytes > 0)
            jo["buffer"] = Convert.ToBase64String(isofile.ReadBytes(nBytes));
        jo["n_bytes2"] = isofile.ReadInt32();
        jo["xa0"] = isofile.ReadInt32();
        jo["xa4"] = isofile.ReadMfcString();
        jo["xb0"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCEvalFakeData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvalDataStorage(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CEvalFakeData", 1);
        jo["n_traces"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCEvalGCData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvalFakeData(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CEvalGCData", 1);
        return jo;
    }

    // =======================================================================
    // CBasicInterface chain (= CData)
    // =======================================================================

    static JsonObject ReadCFinniganInterface(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        int version = isofile.ReadSchemaVersion("CFinniganInterface", 6);
        jo["v"] = version;
        jo["x9c"] = isofile.ReadInt32();
        if (version >= 3)
        {
            jo["xa0"] = isofile.ReadInt32();
            jo["xa4"] = isofile.ReadBool32();
            if (version >= 5) jo["xa8"] = isofile.ReadBool32();
            if (version >= 6) jo["xac"] = isofile.ReadBool32();
        }
        return jo;
    }

    static JsonObject ReadCGpibInterface(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        int version = isofile.ReadSchemaVersion("CGpibInterface", 3);
        jo["v"] = version;
        jo["x9c"] = isofile.ReadUInt8();
        jo["x9d"] = isofile.ReadUInt8();
        if (version >= 3) jo["x9e"] = isofile.ReadUInt8();
        return jo;
    }

    // =======================================================================
    // CTransferPart chain
    // =======================================================================

    static JsonObject ReadCTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CTransferPart", 2);
        jo["x9c"] = isofile.ReadInt32();
        jo["xa0"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCAdcTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCTransferPart(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CAdcTransferPart", 2);
        jo["raw_value"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCMagnetCurrentTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCAdcTransferPart(isofile);
        jo["xa8"] = isofile.ReadBool32();
        return jo;
    }

    // =======================================================================
    // CGasConfPart chain
    // =======================================================================

    static JsonObject ReadCIntegrationUnitGasConfPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        int version = isofile.ReadSchemaVersion("CIntegrationUnitGasConfPart", 2);
        jo["v"] = version;
        int n = isofile.ReadUInt8();
        jo["n_configs"] = n;
        if (n > 0) jo["c_channel_gas_conf_part"] = DispatchN(isofile, n, "CChannelGasConfPart");
        return jo;
    }

    static JsonObject ReadCChannelGasConfPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        int version = isofile.ReadSchemaVersion("CChannelGasConfPart", 4);
        jo["v"] = version;
        jo["cup"] = isofile.ReadUInt8();
        jo["mass"] = isofile.ReadDouble();
        jo["xa8"] = isofile.ReadDouble();
        if (version >= 3)
        {
            jo["xb0"] = isofile.ReadBool32();
            if (version >= 4)
            {
                jo["xb4"] = isofile.ReadBool32();
                jo["xb8"] = isofile.ReadDouble();
            }
        }
        return jo;
    }

    // =======================================================================
    // CScanPart chain
    // =======================================================================

    static JsonObject ReadCBasicScan(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        int version = isofile.ReadSchemaVersion("CBasicScan", 4);
        jo["v"] = version;
        jo["c_scan_part_1"] = Dispatch(isofile);  // polymorphic — any ScanPart subclass
        jo["c_scan_part_2"] = Dispatch(isofile);  // polymorphic — any ScanPart subclass
        var block = DispatchObj(isofile, "CBlockData");
        jo["c_block_data"] = block;
        ValidateBlockNObjects(block, 0);
        jo["x04"] = isofile.ReadInt32();
        jo["xa4"] = isofile.ReadInt32();
        if (version >= 4) jo["x94"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCScanPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        int version = isofile.ReadSchemaVersion("CScanPart", 3);
        jo["v"] = version;
        jo["c_hardware_part"] = Dispatch(isofile);
        jo["xa0"] = isofile.ReadInt32();
        jo["xa4"] = isofile.ReadInt32();
        jo["xb0"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCClockScanPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCScanPart(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CClockScanPart", 2);
        jo["scan_time"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCScaleHvScanPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCScanPart(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CScaleHvScanPart", 2);
        jo["start"] = isofile.ReadInt32();
        jo["stop"] = isofile.ReadInt32();
        jo["step"] = isofile.ReadInt32();
        jo["delay"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCMagnetCurrentScanPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCScanPart(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CMagnetCurrentScanPart", 2);
        jo["start"] = isofile.ReadInt32();
        jo["stop"] = isofile.ReadInt32();
        jo["step"] = isofile.ReadInt32();
        jo["delay"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCIntegrationUnitScanPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCScanPart(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CIntegrationUnitScanPart", 3);
        jo["xc0"] = isofile.ReadInt32();
        jo["xc4"] = isofile.ReadUInt8();
        return jo;
    }

    // =======================================================================
    // CHardwarePart chain
    // =======================================================================

    static JsonObject ReadCHardwarePart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        int version = isofile.ReadSchemaVersion("CHardwarePart", 10);
        jo["v"] = version;
        jo["c_interface"] = Dispatch(isofile);
        bool hasGas = isofile.ReadBool32();
        jo["has_c_gas_conf_part"] = hasGas;
        if (hasGas) jo["c_gas_conf_part"] = Dispatch(isofile);
        bool hasMethod = isofile.ReadBool32();
        jo["has_c_method_part"] = hasMethod;
        if (hasMethod)
            throw new InvalidDataException("CHardwarePart: non-zero CMethodPart not implemented");
        bool hasExtra = isofile.ReadBool32();
        jo["has_extra_c_data"] = hasExtra;
        if (hasExtra) jo["c_data_extra"] = Dispatch(isofile);

        if (version >= 3)
        {
            jo["xac"] = isofile.ReadBool32();
            jo["xb0"] = isofile.ReadBool32();
            jo["xb4"] = isofile.ReadBool32();
            jo["xb8"] = isofile.ReadBool32();
            if (version >= 7)
            {
                jo["c_visualisation_data"] = Dispatch(isofile, "CVisualisationData");
                jo["xc8"] = isofile.ReadDouble();
                jo["xbc"] = isofile.ReadInt32();
                if (version >= 9)
                {
                    int n1 = isofile.ReadInt32();
                    if (n1 > 0) throw new InvalidDataException("CHardwarePart: n_strings1 > 0 not implemented");
                    int n2 = isofile.ReadInt32();
                    if (n2 > 0) throw new InvalidDataException("CHardwarePart: n_strings2 > 0 not implemented");
                    if (version >= 10) jo["xa4"] = isofile.ReadMfcString();
                }
            }
        }
        return jo;
    }

    static JsonObject ReadCCupHardwarePart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCHardwarePart(isofile);
        int version = isofile.ReadSchemaVersion("CCupHardwarePart", 5);
        jo["v"] = version;
        jo["mode"] = isofile.ReadUInt8();
        jo["resistor"] = isofile.ReadDouble();
        jo["x138"] = isofile.ReadDouble();
        if (version >= 3)
        {
            jo["x130"] = isofile.ReadDouble();
            if (version == 4) isofile.SkipBytes(24); // legacy
        }
        return jo;
    }

    static JsonObject ReadCChannelHardwarePart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCHardwarePart(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CChannelHardwarePart", 2);
        jo["x120"] = isofile.ReadInt32();
        jo["x124"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCScaleHardwarePart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCHardwarePart(isofile);
        int version = isofile.ReadSchemaVersion("CScaleHardwarePart", 12);
        jo["v"] = version;
        jo["units"] = isofile.ReadMfcString();
        jo["min_step"] = isofile.ReadUInt32();
        jo["max_step"] = isofile.ReadUInt32();
        if (version >= 4)
        {
            jo["format_mask"] = isofile.ReadUInt32();
            if (version >= 5)
            {
                jo["x124"] = isofile.ReadUInt32(); jo["x128"] = isofile.ReadUInt32();
                jo["x130"] = isofile.ReadUInt32(); jo["x134"] = isofile.ReadUInt32();
                jo["x140"] = isofile.ReadUInt32();
                if (version >= 6)
                {
                    jo["x144"] = isofile.ReadUInt32(); jo["x148"] = isofile.ReadUInt32();
                    jo["x14c"] = isofile.ReadUInt32();
                    if (version >= 7)
                    {
                        jo["x150"] = isofile.ReadUInt32();
                        if (version >= 8)
                        {
                            jo["x154"] = isofile.ReadMfcString(); jo["x158"] = isofile.ReadUInt32();
                            if (version >= 9)
                            {
                                jo["x138"] = isofile.ReadUInt32(); jo["x13c"] = isofile.ReadUInt32();
                                if (version >= 10)
                                {
                                    jo["x15c"] = isofile.ReadMfcString();
                                    if (version >= 11)
                                    {
                                        jo["x160"] = isofile.ReadUInt32(); jo["x164"] = isofile.ReadUInt32();
                                        if (version >= 12)
                                        {
                                            jo["min_value"] = isofile.ReadDouble();
                                            jo["max_value"] = isofile.ReadDouble();
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

    static JsonObject ReadCClockHardwarePart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCScaleHardwarePart(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CClockHardwarePart", 2);
        jo["x190"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCIntegrationUnitHardwarePart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCScaleHardwarePart(isofile);
        int version = isofile.ReadSchemaVersion("CIntegrationUnitHardwarePart", 3);
        jo["v"] = version;
        // Serialization order from R source (offsets are struct layout, not serial order)
        jo["x194"] = isofile.ReadInt32();
        jo["x198"] = isofile.ReadUInt8();
        jo["x19c"] = isofile.ReadInt32();
        jo["x1a0"] = isofile.ReadInt32();
        jo["x199"] = isofile.ReadUInt8();
        jo["x190"] = isofile.ReadUInt8();
        int nTimes = isofile.ReadUInt8();
        jo["n_integration_times"] = nTimes;
        if (nTimes > 0)
        {
            var times = new JsonArray();
            for (int i = 0; i < nTimes; i++) times.Add(isofile.ReadUInt16());
            jo["integration_times"] = times;
        }
        int nCups = isofile.ReadUInt8();
        jo["n_cups"] = nCups;
        if (nCups > 0) jo["c_cup_hardware_part"] = DispatchN(isofile, nCups, "CCupHardwarePart");
        int nChan = isofile.ReadUInt8();
        jo["n_channels"] = nChan;
        if (nChan > 0) jo["c_channel_hardware_part"] = DispatchN(isofile, nChan, "CChannelHardwarePart");
        if (version >= 3) { jo["x1a8"] = isofile.ReadBool32(); jo["x1ac"] = isofile.ReadBool32(); }
        return jo;
    }

    static JsonObject ReadCDacHardwarePart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCScaleHardwarePart(isofile);
        int version = isofile.ReadSchemaVersion("CDacHardwarePart", 3);
        jo["v"] = version;
        jo["x190"] = isofile.ReadUInt8(); jo["x191"] = isofile.ReadUInt8();
        jo["x192"] = isofile.ReadUInt8(); jo["x193"] = isofile.ReadUInt8();
        if (version >= 3) jo["format"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCScaleHvHardwarePart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCDacHardwarePart(isofile);
        int version = isofile.ReadSchemaVersion("CScaleHvHardwarePart", 3);
        jo["v"] = version;
        if (version >= 3) jo["x198"] = isofile.ReadDouble();
        return jo;
    }

    static JsonObject ReadCMagnetCurrentHardwarePart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCDacHardwarePart(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CMagnetCurrentHardwarePart", 2);
        jo["x198"] = isofile.ReadInt32();
        jo["x19c"] = isofile.ReadInt32();
        return jo;
    }

    // =======================================================================
    // CPlotInfo / CTraceInfo (.scn file classes)
    // =======================================================================

    static JsonObject ReadCPlotInfo(IsodatFile isofile)
    {
        // No own schema version; MFC archive version from CRuntimeClass header is used
        int version = isofile.ObjectLog[^1].ArchiveVersion;
        var jo = new JsonObject();
        jo["x10"] = isofile.ReadInt32(); jo["x20"] = isofile.ReadInt32();
        jo["x14"] = isofile.ReadInt32(); jo["x18"] = isofile.ReadInt32();
        jo["x1c"] = isofile.ReadInt32();
        jo["right_left_factor"] = isofile.ReadFloat();
        jo["background_color"] = isofile.ReadColor();
        jo["labels_color"] = isofile.ReadColor();
        jo["x38"] = isofile.ReadInt32();
        jo["x3c"] = isofile.ReadUInt16();
        jo["font"] = isofile.ReadMfcString();
        jo["x_label"] = isofile.ReadMfcString();
        jo["y_label"] = isofile.ReadMfcString();
        jo["trace"] = isofile.ReadMfcString();
        jo["c_trace_info"] = Dispatch(isofile, "CTraceInfo");
        jo["c_plot_range"] = Dispatch(isofile, "CPlotRange");
        jo["c_plot_range_zoom"] = Dispatch(isofile, "CPlotRange");
        if (version > 1) { jo["x08"] = isofile.ReadInt32(); jo["x0c"] = isofile.ReadInt32(); }
        jo["c_plot_range_zoom2"] = ReadCPlotRange(isofile);
        jo["c_plot_range2"] = ReadCPlotRange(isofile);
        int nTraces = jo["c_trace_info"]?["n_traces"]?.GetValue<int>() ?? 0;
        if (nTraces > 0)
        {
            var labels = new JsonArray();
            for (int i = 0; i < nTraces; i++) labels.Add(isofile.ReadMfcString());
            jo["trace_labels"] = labels;
        }
        return jo;
    }

    static JsonObject ReadCTraceInfo(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["x04"] = isofile.ReadInt32();
        int nTraces = isofile.ReadUInt8();
        jo["n_traces"] = nTraces;
        if (nTraces > 0) jo["c_trace_info_entry"] = DispatchN(isofile, nTraces, "CTraceInfoEntry");
        jo["n_traces"] = isofile.ReadUInt8();  // read again
        if (nTraces > 0)
        {
            var labels = new JsonArray();
            for (int i = 0; i < nTraces; i++) labels.Add(isofile.ReadMfcString());
            jo["trace_labels"] = labels;
        }
        return jo;
    }

    static JsonObject ReadCTraceInfoEntry(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["idx"] = isofile.ReadUInt8();
        jo["x05"] = Convert.ToBase64String(isofile.ReadBytes(1));
        jo["trace_color"] = isofile.ReadColor();
        jo["x0c"] = isofile.ReadInt32();
        jo["x10"] = isofile.ReadInt32();
        jo["x14"] = isofile.ReadInt32();
        return jo;
    }

    // CPlotRange registered as dispatched object
    static JsonObject ReadCPlotRangeObj(IsodatFile isofile) => ReadCPlotRange(isofile);

    // CPlotRange inline (no CRuntimeClass header)
    public static JsonObject ReadCPlotRange(IsodatFile isofile)
    {
        return new JsonObject
        {
            ["xmin"] = isofile.ReadFloat(),
            ["xmax"] = isofile.ReadFloat(),
            ["ymin"] = isofile.ReadDouble(),
            ["ymax"] = isofile.ReadDouble(),
        };
    }

    // =======================================================================
    // CStringArray
    // =======================================================================

    static JsonObject ReadCStringArray(IsodatFile isofile)
    {
        int count = isofile.ReadUInt16();
        if (count == 0xFFFF) count = isofile.ReadInt32(); // uint32 fallback
        var jo = new JsonObject();
        jo["n_strings"] = count;
        if (count > 0)
        {
            var arr = new JsonArray();
            for (int i = 0; i < count; i++) arr.Add(isofile.ReadMfcString());
            jo["strings"] = arr;
        }
        return jo;
    }

    static JsonObject ReadCParsedEvaluationString(IsodatFile isofile)
    {
        var jo = new JsonObject();
        int version = isofile.ReadSchemaVersion("CParsedEvaluationString", 2);
        jo["v"] = version;
        jo["user_string"] = isofile.ReadMfcString();
        jo["gas_name_nominator"] = isofile.ReadMfcString();
        jo["gas_name_divisor"] = isofile.ReadMfcString();
        jo["xa0"] = isofile.ReadMfcString();
        jo["mass_divisor"] = isofile.ReadMfcString();
        jo["nominator_mass"] = isofile.ReadUInt32();
        jo["divisor_mass"] = isofile.ReadUInt32();
        if (version >= 2) jo["default_visible"] = isofile.ReadUInt32();
        return jo;
    }

    // =======================================================================
    // CAction chain
    // =======================================================================

    static JsonObject ReadCAction(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        int version = isofile.ReadSchemaVersion("CAction", 6);
        jo["v"] = version;
        if (version >= 3) jo["x94"] = isofile.ReadInt32();
        if (version >= 4) jo["xb0"] = isofile.ReadMfcString();
        if (version >= 5) jo["x9c"] = isofile.ReadInt32();
        if (version >= 6) jo["xa0"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCActionPeakCenter(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCAction(isofile);
        isofile.ReadSchemaVersion("CActionPeakCenter", 1); // discard
        jo["xbc"] = isofile.ReadUInt32();
        jo["xb8"] = isofile.ReadUInt8();
        return jo;
    }

    static JsonObject ReadCActionHwTransferContainer(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCAction(isofile);
        int version = isofile.ReadSchemaVersion("CActionHwTransferContainer", 2);
        jo["v"] = version;
        jo["xb8"] = isofile.ReadUInt32();
        jo["c_transfer_part"] = Dispatch(isofile);
        if (version >= 2) { jo["xd8"] = isofile.ReadUInt32(); jo["xb4"] = isofile.ReadMfcString(); }
        return jo;
    }

    static JsonObject ReadCActionSubScript(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCAction(isofile);
        isofile.ReadSchemaVersion("CActionSubScript", 3); // discard
        string xb8 = isofile.ReadMfcString();
        jo["xb8"] = xb8;
        if (xb8 == "") jo["c_action_script"] = Dispatch(isofile, "CActionScript");
        return jo;
    }

    static JsonObject ReadCDelay(IsodatFile isofile)
    {
        isofile.AddWarning("CDelay: only CAction parent read (stub)");
        return new JsonObject { ["parent"] = ReadCAction(isofile) };
    }

    static JsonObject ReadCActionInterpreter(IsodatFile isofile)
    {
        isofile.AddWarning("CActionInterpreter: Serialize unknown, returning empty");
        return new JsonObject();
    }

    static JsonObject ReadCMethodSwitcher(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCAction(isofile);
        int version = isofile.ReadSchemaVersion("CMethodSwitcher", 5);
        jo["v"] = version;
        jo["gas_conf_name"] = isofile.ReadMfcString();
        if (version >= 3) { jo["wait_time"] = isofile.ReadUInt32(); jo["method_name"] = isofile.ReadMfcString(); }
        if (version >= 4) jo["script_path"] = isofile.ReadMfcString();
        if (version >= 5) jo["use_hysteresis"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCTimeEventList(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCAction(isofile);
        int version = isofile.ReadSchemaVersion("CTimeEventList", 3);
        jo["v"] = version;
        int n = isofile.ReadInt32();
        if (n > 0) jo["actions"] = DispatchN(isofile, n);
        if (version >= 2) jo["xdc"] = isofile.ReadUInt32();
        if (version >= 3) jo["xe8"] = isofile.ReadUInt32();
        return jo;
    }

    // =======================================================================
    // CEvaluationPart / CMethodPart chain
    // =======================================================================

    static JsonObject ReadCEvaluationPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        isofile.ReadSchemaVersion("CEvaluationPart", 2); // discard
        jo["x9c"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCMethodPrintoutDesc(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvaluationPart(isofile);
        int version = isofile.ReadSchemaVersion("CMethodPrintoutDesc", 2);
        jo["v"] = version;
        jo["xa0"] = isofile.ReadMfcString();
        jo["xa4"] = isofile.ReadMfcString();
        if (version >= 2) { jo["xa8"] = isofile.ReadMfcString(); jo["xac"] = isofile.ReadMfcString(); }
        return jo;
    }

    static JsonObject ReadCComponentListMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvaluationPart(isofile);
        jo["c_component_list"] = Dispatch(isofile, "CComponentList");
        return jo;
    }

    static JsonObject ReadCPartMirror(IsodatFile isofile) => new JsonObject();

    static JsonObject ReadCTimeEventListMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvaluationPart(isofile);
        jo["c_time_event_list"] = Dispatch(isofile, "CTimeEventList");
        return jo;
    }

    static JsonObject ReadCContiniousFlowStandardizationMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvaluationPart(isofile);
        isofile.ReadSchemaVersion("CContiniousFlowStandardizationMethodPart", 1); // discard
        jo["xa0"] = isofile.ReadUInt32();
        jo["xa8"] = isofile.ReadUInt32();
        jo["xac"] = isofile.ReadUInt32();
        jo["xb0"] = isofile.ReadMfcString();
        long flag = isofile.ReadUInt32();
        if (flag != 0) jo["c_data_xb4"] = Dispatch(isofile, "CData");
        return jo;
    }

    static JsonObject ReadCContiniousFlowStandardizationListMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvaluationPart(isofile);
        int version = isofile.ReadSchemaVersion("CContiniousFlowStandardizationListMethodPart", 9);
        jo["v"] = version;
        jo["xac"] = isofile.ReadUInt32();
        jo["xb8"] = isofile.ReadUInt32();
        jo["xb4"] = isofile.ReadUInt32();
        long flag1 = isofile.ReadUInt32();
        if (flag1 != 0) jo["c_data_xa4"] = Dispatch(isofile, "CData");
        long flag2 = isofile.ReadUInt32();
        if (flag2 != 0) jo["c_data_xb0"] = Dispatch(isofile, "CData");
        if (version > 2) jo["c_data_xdc"] = Dispatch(isofile, "CData");
        if (version == 4) { Dispatch(isofile); Dispatch(isofile); } // discard
        if (version > 5) jo["x100"] = isofile.ReadUInt32();
        if (version > 6) jo["x104"] = isofile.ReadUInt32();
        if (version > 7) jo["x108"] = isofile.ReadMfcString();
        if (version > 8) jo["x10c"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCPrimaryStandardMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvaluationPart(isofile);
        int version = isofile.ReadSchemaVersion("CPrimaryStandardMethodPart", 2);
        jo["v"] = version;
        jo["xa0"] = isofile.ReadMfcString();
        if (version == 1) isofile.ReadUInt32(); // element_num, discard
        jo["c_data_xa8"] = Dispatch(isofile, "CData");
        if (version > 1) jo["xb0"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCSecondaryStandardMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvaluationPart(isofile);
        int version = isofile.ReadSchemaVersion("CSecondaryStandardMethodPart", 3);
        jo["v"] = version;
        jo["xa0"] = isofile.ReadMfcString();
        jo["xa4"] = isofile.ReadMfcString();
        jo["xb0"] = isofile.ReadUInt32();
        jo["c_data_xac"] = Dispatch(isofile, "CData");
        if (version > 1) jo["xa8"] = isofile.ReadUInt32();
        if (version > 2)
        {
            long flag = isofile.ReadUInt32();
            if (flag != 0) jo["c_data_xb8"] = Dispatch(isofile, "CData");
        }
        return jo;
    }

    static JsonObject ReadCConFloMethodPart(IsodatFile isofile)
    {
        isofile.AddWarning("CConFloMethodPart: only CMethodPart parent + version read (stub)");
        var jo = new JsonObject();
        jo["parent"] = ReadCEvaluationPart(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CConFloMethodPart", 11);
        return jo;
    }

    static JsonObject ReadCICA_BasicMethodPart(IsodatFile isofile)
    {
        isofile.AddWarning("CICA_BasicMethodPart: only CMethodPart parent + version read (stub)");
        var jo = new JsonObject();
        jo["parent"] = ReadCEvaluationPart(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CICA_BasicMethodPart", 12);
        return jo;
    }

    static JsonObject ReadCPeakFindMethodPart(IsodatFile isofile)
    {
        isofile.AddWarning("CPeakFindMethodPart: only CMethodPart parent read (stub)");
        return new JsonObject { ["parent"] = ReadCEvaluationPart(isofile) };
    }

    static JsonObject ReadCSimplePeakFindMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCPeakFindMethodPart(isofile);
        isofile.ReadSchemaVersion("CSimplePeakFindMethodPart", 1); // discard
        jo["x128"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCSimplePeakFindParameter(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCPeakFindParameter(isofile);
        isofile.ReadSchemaVersion("CSimplePeakFindParameter", 1); // discard
        return jo;
    }

    // =======================================================================
    // CDeviceMethodPart chain
    // =======================================================================

    static JsonObject ReadCDeviceMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvaluationPart(isofile);
        int version = isofile.ReadSchemaVersion("CDeviceMethodPart", 2);
        jo["v"] = version;
        if (version >= 2)
        {
            var inner = DispatchObj(isofile, "CBlockData");
            jo["parent"] = inner;
            int n = NObjects(inner);
            if (n > 0) jo["method_parts"] = DispatchN(isofile, n);
        }
        else
        {
            int n = isofile.ReadInt32();
            if (n > 0) jo["method_parts"] = DispatchN(isofile, n);
        }
        return jo;
    }

    static JsonObject ReadCConFloDeviceMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCDeviceMethodPart(isofile);
        isofile.ReadSchemaVersion("CConFloDeviceMethodPart", 1); // discard
        return jo;
    }

    static JsonObject ReadCMsDeviceMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCDeviceMethodPart(isofile);
        int version = isofile.ReadSchemaVersion("CMsDeviceMethodPart", 3);
        jo["v"] = version;
        jo["xb0"] = isofile.ReadUInt32();
        jo["xac"] = isofile.ReadUInt8();
        jo["c_action_peak_center"] = Dispatch(isofile, "CActionPeakCenter");
        if (version >= 2) jo["xb8"] = isofile.ReadUInt32();
        if (version >= 3) jo["xbc"] = isofile.ReadUInt8();
        return jo;
    }

    static JsonObject ReadCStandardDeviceMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCDeviceMethodPart(isofile);
        isofile.ReadSchemaVersion("CStandardDeviceMethodPart", 1); // discard
        jo["xac"] = isofile.ReadMfcString();
        jo["xb0"] = isofile.ReadMfcString();
        jo["xb4"] = isofile.ReadMfcString();
        jo["xb8"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCGenericGcDeviceMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCDeviceMethodPart(isofile);
        isofile.ReadSchemaVersion("CGenericGcDeviceMethodPart", 1); // discard
        isofile.ReadUInt32(); // discarded
        jo["xb0"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCFlashEA_DeviceMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCGenericGcDeviceMethodPart(isofile);
        isofile.ReadSchemaVersion("CFlashEA_DeviceMethodPart", 2); // discard
        return jo;
    }

    static JsonObject ReadCMultiReferenceDeviceMethodPart(IsodatFile isofile)
    {
        isofile.AddWarning("CMultiReferenceDeviceMethodPart: only CDeviceMethodPart parent + version read (stub)");
        var jo = new JsonObject();
        jo["parent"] = ReadCDeviceMethodPart(isofile);
        jo["v"] = isofile.ReadSchemaVersion("CMultiReferenceDeviceMethodPart", 7);
        return jo;
    }

    // =======================================================================
    // CDeviceEvaluationPart chain
    // =======================================================================

    static JsonObject ReadCDeviceEvaluationPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvaluationPart(isofile);
        int version = isofile.ReadSchemaVersion("CDeviceEvaluationPart", 2);
        jo["v"] = version;
        if (version >= 2)
        {
            var inner = DispatchObj(isofile, "CBlockData");
            jo["parent"] = inner;
            int n = NObjects(inner);
            if (n > 0) jo["method_parts"] = DispatchN(isofile, n);
        }
        else
        {
            int n = isofile.ReadInt32();
            if (n > 0) jo["method_parts"] = DispatchN(isofile, n);
        }
        return jo;
    }

    static JsonObject ReadCConFloDeviceEvaluationPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCDeviceEvaluationPart(isofile);
        isofile.ReadSchemaVersion("CConFloDeviceEvaluationPart", 1); // discard
        return jo;
    }

    static JsonObject ReadCMsDeviceEvaluationPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCDeviceEvaluationPart(isofile);
        isofile.ReadSchemaVersion("CMsDeviceEvaluationPart", 2); // discard
        return jo;
    }

    static JsonObject ReadCFlashEA_DeviceEvaluationPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCDeviceEvaluationPart(isofile);
        isofile.ReadSchemaVersion("CFlashEA_DeviceEvaluationPart", 1); // discard
        return jo;
    }

    // =======================================================================
    // CEvalDataTransferPart chain
    // =======================================================================

    static JsonObject ReadCEvalDataTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvalDataItemTransferPart(isofile);
        int version = isofile.ReadSchemaVersion("CEvalDataTransferPart", 2);
        jo["v"] = version;
        if (version >= 1)
        {
            long n = isofile.ReadUInt32();
            if (n > 0) jo["xc0_raw"] = Convert.ToBase64String(isofile.ReadBytes((int)n));
        }
        if (version >= 2) jo["c_block_data_xc8"] = Dispatch(isofile, "CBlockData");
        return jo;
    }

    static JsonObject ReadCEvalDataDWORDTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvalDataTransferPart(isofile);
        isofile.ReadSchemaVersion("CEvalDataDWORDTransferPart", 1); // discard
        return jo;
    }

    static JsonObject ReadCEvalDataSecStdTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvalDataDWORDTransferPart(isofile);
        int version = isofile.ReadSchemaVersion("CEvalDataSecStdTransferPart", 2);
        jo["v"] = version;
        jo["standard_name"] = isofile.ReadMfcString();
        if (version >= 2)
        {
            jo["is_calculated"] = isofile.ReadUInt32();
            jo["calculated_value"] = isofile.ReadDouble();
        }
        return jo;
    }

    static JsonObject ReadCEvalDataStringTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCEvalDataTransferPart(isofile);
        isofile.ReadSchemaVersion("CEvalDataStringTransferPart", 1); // discard
        long n = isofile.ReadUInt32();
        jo["data_string"] = n > 0
            ? Encoding.Latin1.GetString(isofile.ReadBytes((int)n))
            : "";
        return jo;
    }

    // =======================================================================
    // Peak stubs
    // =======================================================================

    static JsonObject ReadCGCPeak(IsodatFile isofile)
    {
        isofile.AddWarning("CGCPeak: CGCBGDData parent Serialize unknown, returning empty");
        return new JsonObject();
    }

    static JsonObject ReadCSPeak(IsodatFile isofile)
    {
        isofile.AddWarning("CSPeak: parent class and Serialize unknown, returning empty");
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
    static void ReadScrBase(IsodatFile isofile, JsonObject jo)
    {
        jo["parent"] = ReadCData(isofile);
        int version = isofile.ReadSchemaVersion("CScrBase", 2);
        jo["v"] = version;
        jo["x9c"] = isofile.ReadMfcString();   // headline / description
        jo["xa0"] = isofile.ReadInt32();
        jo["xa4"] = isofile.ReadInt32();
        jo["xa8"] = Dispatch(isofile);          // optional WriteObject (null in observed files)
        jo["xac"] = Dispatch(isofile);          // optional WriteObject (null in observed files)
        jo["xb0"] = isofile.ReadInt32();
        jo["xb4"] = isofile.ReadInt32();
    }

    static JsonObject ReadCScrHeadLine(IsodatFile isofile)
    {
        var jo = new JsonObject();
        ReadScrBase(isofile, jo);
        jo["c_dyn_external"] = Dispatch(isofile);   // CDynExternal WriteObject
        return jo;
    }

    static JsonObject ReadCScrNumber(IsodatFile isofile)
    {
        var jo = new JsonObject();
        ReadScrBase(isofile, jo);
        jo["c_numeric_value"] = Dispatch(isofile);  // CNumericValue WriteObject
        return jo;
    }

    // CDynExternal (CData-derived) — structure reverse-engineered from v=2 binary.
    // The 130 bytes of own fields after CData + version:
    //   empty string + int(type) + 4×int(0) + int + 7×uint16(descriptors) +
    //   string(category) + string(unit) + int(-1) + string(formula) + string(name) +
    //   int + uint16 + int + string(time_category) + int(precision)
    static JsonObject ReadCDynExternal(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["parent"] = ReadCData(isofile);
        int version = isofile.ReadSchemaVersion("CDynExternal", 4);
        jo["v"] = version;
        jo["x9c"] = isofile.ReadMfcString();   // empty in observed files
        jo["xa0"] = isofile.ReadInt32();        // type code
        jo["xa4"] = isofile.ReadInt32();
        jo["xa8"] = isofile.ReadInt32();
        jo["xac"] = isofile.ReadInt32();
        jo["xb0"] = isofile.ReadInt32();
        jo["xb4"] = isofile.ReadInt32();        // = 1
        // 7 descriptor uint16 values
        var desc = new JsonArray();
        for (int i = 0; i < 7; i++) desc.Add(isofile.ReadUInt16());
        jo["x_desc"] = desc;
        jo["xf0"] = isofile.ReadMfcString();   // category (e.g. "parameters")
        jo["xf4"] = isofile.ReadMfcString();   // unit (empty)
        jo["xf8"] = isofile.ReadInt32();        // limit (-1)
        jo["xfc"] = isofile.ReadMfcString();   // formula (empty)
        jo["x100"] = isofile.ReadMfcString();   // name (empty)
        int x104 = isofile.ReadInt32();
        jo["x104"] = x104;
        // Trailing time-parameter section only present when xf8 == -1 (no numeric limit)
        if (jo["xf8"]!.GetValue<int>() < 0)
        {
            jo["x108"] = isofile.ReadUInt16();
            jo["x10a"] = isofile.ReadInt32();
            jo["x110"] = isofile.ReadMfcString();
            jo["x114"] = isofile.ReadInt32();
        }
        return jo;
    }

    // CNumericValue — no CData parent; stores a numeric value with a descriptor object.
    static JsonObject ReadCNumericValue(IsodatFile isofile)
    {
        var jo = new JsonObject();
        jo["value"] = isofile.ReadDouble();       // IEEE 754 double
        jo["x08"] = isofile.ReadInt32();        // some flag/type
        jo["c_descriptor"] = Dispatch(isofile);      // descriptor CData subclass
        return jo;
    }

    // =======================================================================
    // CShrinkInfo (not CData/CBlockData derived)
    // =======================================================================

    static JsonObject ReadCShrinkInfo(IsodatFile isofile)
    {
        var jo = new JsonObject();
        int version = isofile.ReadSchemaVersion("CShrinkInfo", 2);
        jo["v"] = version;
        int n = isofile.ReadInt32();
        jo["n_items"] = n;
        if (n > 0)
        {
            var items = new JsonArray();
            for (int i = 0; i < n; i++)
                items.Add(new JsonObject { ["col_idx"] = isofile.ReadInt32(), ["width"] = isofile.ReadInt32() });
            jo["items"] = items;
        }
        return jo;
    }

    // =======================================================================
    // CContiniousFlowBlockData (top-level DXF object)
    // =======================================================================

    public static JsonObject ReadCContiniousFlowBlockData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);

        // Parent: CAcquistionBaseBlockData = CBlockData (inline, no CRuntimeClass header)
        jo["parent"] = ReadCBlockData(isofile);

        jo["c_measurment_infos"] = Dispatch(isofile, "CMeasurmentInfos");
        jo["c_measurment_errors"] = Dispatch(isofile, "CMeasurmentErrors");

        // CBlockData wrapper (n_objects=1) → CPlotSettings
        //   CPlotSettings.p_c_block_data has 5 CWinSettings children
        //   Each CWinSettings.p_c_block_data has N CGasSettings children
        //     Each CGasSettings.p_c_block_data has N CTraceSettings children
        //     + CPkDataItemList (N CPeakDataItem) + optional CShrinkInfo
        //   CWinSettings then has CViewColors → CWinColor (8 CTraceLinCol) + CGridColors + CAxisPara (CTraceLinCol)
        var plotWrapper = DispatchObj(isofile, "CBlockData");
        ValidateBlockValue(plotWrapper, "Plot Settings");
        jo["c_plot_settings"] = Dispatch(isofile, "CPlotSettings");

        // CBlockData wrapper (n_objects=N) → N × CRawData
        //   Each CRawData: CGCData (inline) → CEvalGCData → CAllMoleculeWeights
        //                  + CStringArray (v>3) + CIntegrationUnitGasConfPart (v>4, flag-gated)
        var rawWrapper = DispatchObj(isofile, "CBlockData");
        ValidateBlockValue(rawWrapper, "RawDataBlock");
        int nRaw = NObjects(rawWrapper);
        jo["c_raw_data"] = DispatchN(isofile, nRaw, "CRawData");

        // Same structure as RawDataBlock — same N, same CRawData reader
        var origWrapper = DispatchObj(isofile, "CBlockData");
        ValidateBlockValue(origWrapper, "OrigDataBlock");
        jo["c_original_data"] = DispatchN(isofile, nRaw, "CRawData");

        // CBlockData wrapper (n_objects=0) — "Calculated H3 Factor"
        var h3Wrapper = DispatchObj(isofile, "CBlockData");
        ValidateBlockValue(h3Wrapper, "Calculated H3 Factor");
        jo["c_h3_factor_block"] = h3Wrapper;

        // CBlockData wrapper (n_objects=0) — "Prim Std"
        var primWrapper = DispatchObj(isofile, "CBlockData");
        ValidateBlockValue(primWrapper, "Prim Std");
        jo["c_prim_std_block"] = primWrapper;

        // CBlockData wrapper (n_objects=1) → CMethod
        var methodWrapper = DispatchObj(isofile, "CBlockData");
        ValidateBlockValue(methodWrapper, "Method");
        jo["c_method"] = Dispatch(isofile, "CMethod");

        return jo;
    }

    // =======================================================================
    // CScanStorage (.scn top-level object, stub)
    // =======================================================================

    public static JsonObject ReadCScanStorage(IsodatFile isofile)
    {
        isofile.AddWarning("CScanStorage: not yet implemented for this parser");
        return new JsonObject();
    }

    // =======================================================================
    // Helpers
    // =======================================================================

    static JsonArray ReadIntArray(IsodatFile isofile, int n)
    {
        var arr = new JsonArray();
        for (int i = 0; i < n; i++) arr.Add(isofile.ReadInt32());
        return arr;
    }
}
