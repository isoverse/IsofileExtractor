using System.Text;
using System.Text.Json.Nodes;

namespace IsodatReader;

// ---------------------------------------------------------------------------
// All class reader functions for isodat binary files (.dxf, .scn).
//
// Naming conventions:
//   - ReadCXxx()        called directly for inline parent-class serialization
//   - ReadObject(isofile)      called when object was written via WriteObject()
//                       (reads CRuntimeClass header first, then calls reader)
//   - "parent" keys    parent class data embedded inline
//   - "c_xxx" keys      member objects written via WriteObject
// ---------------------------------------------------------------------------
static class Readers
{
    // =======================================================================
    // Partial-result tracking (thread-local stack, one slot per ReadObject frame)
    // =======================================================================

    public static bool Unabridged { get; set; }

    static string ParentKey => Unabridged ? "parent" : "p";
    static string ValueKey => Unabridged ? "value" : "v";

    [ThreadStatic] static Stack<JsonObject?>? _partialStack;
    static Stack<JsonObject?> PartialStack => _partialStack ??= new();

    // Called once at the top of any reader that wants partial-result capture.
    // Since jo is a reference, the stack slot always reflects the current state.
    static void TrackPartial(JsonObject jo)
    {
        if (PartialStack.Count > 0 && PartialStack.Peek() is null)
        {
            PartialStack.Pop();
            PartialStack.Push(jo);
        }
    }

    static string ClassToJsonKey(string className) => className;

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
            ["CPeakDetectionParameter"] = ReadCPeakDetectionParameter,

            // --- CSimple chain ---
            ["CSimple"] = ReadCSimple,
            ["CStr"] = ReadCStr,
            ["CDword"] = ReadCDword,
            ["CInt"] = ReadCInt,
            ["CDouble"] = ReadCDouble,
            ["CPeakCenterOffset"] = ReadCDword,
            ["CBinary"] = ReadCBinary,

            // --- CBlockData chain ---
            ["CBlockData"] = ReadCBlockData,
            ["CAcquistionBaseBlockData"] = ReadCBlockData,
            ["CPort"] = ReadCBlockData,
            ["CDataIndex"] = ReadCDataIndex,
            ["CCalibration"] = ReadCCalibration,
            ["CCalibrationParameter"] = ReadCCalibrationParameter,
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
            ["CConFloDevice"] = ReadCConFloDevice,
            ["CMultiReferenceDevice"] = ReadCBufferedRefillDevice,
            ["CUserDevice"] = ReadCBufferedRefillDevice,
            ["CGCExtendedInterfaceDevice"] = ReadCBufferedRefillDevice,
            ["CXCaliburDevice"] = ReadCXCaliburDevice,
            ["CTraceBasicDevice"] = ReadCTraceBasicDevice,
            ["CTrace_II_Device"] = ReadCTrace_II_Device,
            ["CXcalRSH2Device"] = ReadCXcalRSH2Device,
            ["CXcalRSHDevice"] = ReadCXcalRSH2Device,

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
            ["CValveTransferPart"] = ReadCAdcTransferPart,
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
            ["CGCExtendedInterfaceDeviceMethodPart"] = ReadCGCExtendedInterfaceDeviceMethodPart,
            ["CFlashEA_DeviceMethodPart"] = ReadCFlashEA_DeviceMethodPart,
            ["CMultiReferenceDeviceMethodPart"] = ReadCMultiReferenceDeviceMethodPart,
            ["CActiveDeviceMethodPart"] = ReadCDeviceMethodPart,

            // --- CDeviceEvaluationPart chain ---
            ["CDeviceEvaluationPart"] = ReadCDeviceEvaluationPart,
            ["CConFloDeviceEvaluationPart"] = ReadCConFloDeviceEvaluationPart,
            ["CGCExtendedInterfaceDeviceEvaluationPart"] = ReadCConFloDeviceEvaluationPart,
            ["CMsDeviceEvaluationPart"] = ReadCMsDeviceEvaluationPart,
            ["CGenericGcDeviceEvaluationPart"] = ReadCConFloDeviceEvaluationPart,
            ["CFlashEA_DeviceEvaluationPart"] = ReadCFlashEA_DeviceEvaluationPart,
            ["CMultiReferenceDeviceEvaluationPart"] = ReadCConFloDeviceEvaluationPart,

            // --- CEvalDataTransferPart chain ---
            ["CEvalDataTransferPart"] = ReadCEvalDataTransferPart,
            ["CEvalDataDWORDTransferPart"] = ReadCEvalDataDWORDTransferPart,
            ["CEvalDataSecStdTransferPart"] = ReadCEvalDataSecStdTransferPart,
            ["CEvalDataStringTransferPart"] = ReadCEvalDataStringTransferPart,
            ["CEvalDataIntTransferPart"] = ReadCEvalDataIntTransferPart,
            ["CEvalDataDoubleTransferPart"] = ReadCEvalDataDoubleTransferPart,

            // --- CResultData / CSPeak / CGCPeak chain ---
            ["CResultData"] = ReadCResultData,
            ["CGCBGDData"] = ReadCGCBGDData,
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
    // ReadObject: read CRuntimeClass header → look up reader → call it
    // =======================================================================

    public static bool HasReader(string className) => _registry.ContainsKey(className);

    /// <summary>
    /// Read CRuntimeClass header from stream, then call the registered reader.
    /// Throws if the stream contains the MFC NULL WriteObject tag (<c>00 00</c>).
    /// <paramref name="expected"/> requires an exact class name match.
    /// <paramref name="pattern"/> requires the class name to contain the given substring
    /// (use for polymorphic reads where only the base-class name fragment is known).
    /// </summary>
    public static JsonObject ReadObject(IsodatFile isofile, string? expected = null, string? pattern = null)
    {
        long headerPos = isofile.Position;
        string? className = isofile.ReadCRuntimeClass(expected);
        if (className is null)
            throw new InvalidDataException(
                $"Unexpected MFC NULL WriteObject at 0x{headerPos:x}" +
                (expected is not null ? $" (expected '{expected}')" : ""));
        if (pattern is not null && !className.Contains(pattern, StringComparison.Ordinal))
            throw new InvalidDataException(
                $"Expected class matching '{pattern}' but encountered '{className}' at 0x{headerPos:x}");
        if (!_registry.TryGetValue(className, out var reader))
            throw new InvalidDataException(
                $"No reader registered for class '{className}' (header at 0x{headerPos:x})");

        int entryIndex = isofile.ObjectLog.Count - 1;
        isofile.PushContainer(isofile.ObjectLog[^1].ObjIdx);
        PartialStack.Push(null);
        bool popped = false;
        try
        {
            var result = reader(isofile);
            isofile.SetObjectLogValue(entryIndex, ExtractCDataValue(result));
            return result;
        }
        catch (IsodatParseException ipe)
        {
            popped = true;
            var partial = PartialStack.Pop();
            // Embed inner partial into outer so the tree is as deep as possible.
            // ipe.PartialResult is null when ReadObjectInto already placed it in an objects
            // dict (it nulls the field before rethrowing to prevent a second-parent violation).
            if (partial is not null && ipe.PartialResult is not null && ipe.PartialResultClassName is not null)
                partial[ClassToJsonKey(ipe.PartialResultClassName)] = ipe.PartialResult;
            var enriched = ipe.PrependPath(className, headerPos);
            enriched.PartialResult = partial ?? ipe.PartialResult;
            enriched.PartialResultClassName = className;
            throw enriched;
        }
        catch (Exception ex)
        {
            popped = true;
            var partial = PartialStack.Pop();
            var exc = new IsodatParseException(className, headerPos, isofile.Position, ex);
            exc.PartialResult = partial;
            exc.PartialResultClassName = className;
            throw exc;
        }
        finally
        {
            isofile.PopContainer();
            if (!popped) PartialStack.Pop();
        }
    }

    // Read one object into a CBlockData objects dict, keyed by snake_case class name.
    // If the object is itself a raw CBlockData alias (CAcquistionBaseBlockData, CPort) with
    // sub-children left in the stream, re-pushes its ObjIdx as container and recursively
    // reads those sub-children so they nest correctly in the tree.
    // Partial-result aware: if ReadObject fails with a partial result it is added to the
    // dict before rethrowing; if sub-child expansion fails the already-read child is added
    // before the loop (JsonObject is a reference, so in-place sub-child additions remain visible).
    // IsBlockObject is set in a finally block so it is always recorded even on parse errors.
    static void ReadObjectInto(JsonObject container, IsodatFile isofile,
                                    string? expected = null, string? pattern = null,
                                    int groupTag = 0, int? groupDeclaredSize = null,
                                    bool maybeNull = false, bool noIndex = false)
    {
        if (maybeNull)
        {
            byte[] h = isofile.ReadBytes(2);
            if (h[0] == 0 && h[1] == 0) return;  // MFC NULL WriteObject — field absent
            isofile.SkipBytes(-2);
        }
        int before = isofile.ObjectLog.Count;
        try
        {
            JsonObject child;
            string childClassName;
            try
            {
                child = ReadObject(isofile, expected, pattern);
                childClassName = isofile.ObjectLog[before].ClassName;
            }
            catch (IsodatParseException ipe) when (ipe.PartialResult is JsonObject partial)
            {
                // ReadObject failed but captured a partial result; add it so it appears in output.
                // Null out PartialResult so outer ReadObject frames don't try to embed this node
                // again — it already has a parent from AddToObjectsDict.
                AddToObjectsDict(container,
                    ipe.PartialResultClassName ?? isofile.ObjectLog[before].ClassName, partial,
                    setIdx: !noIndex);
                ipe.PartialResult = null;
                ipe.PartialResultClassName = null;
                throw;
            }

            // Add child immediately so it appears in output even if sub-child expansion fails.
            // Sub-children populate child["objects"] in-place, so the dict entry stays current.
            AddToObjectsDict(container, childClassName, child, setIdx: !noIndex);

            int n = NBlockObjects(child);
            if (n > 0)
            {
                isofile.PushContainer(isofile.ObjectLog[before].ObjIdx);
                try
                {
                    var childContainer = child["objects"]!.AsObject();
                    for (int i = 0; i < n; i++)
                        ReadObjectInto(childContainer, isofile);
                }
                finally
                {
                    isofile.PopContainer();
                }
            }
        }
        finally
        {
            if (!noIndex && before < isofile.ObjectLog.Count)
                isofile.SetObjectLogIsGroupObject(before, groupTag, groupDeclaredSize);
        }
    }

    // Push null so the reader's TrackPartial can register its jo on the PartialStack.
    // Without this the reader's TrackPartial is a no-op (top is already a non-null jo from
    // the outer ReadObject), meaning a mid-parse failure leaves jo["parent"] unset and the
    // containing class appears empty in the partial result.
    static JsonObject ReadParent(JsonObject jo, IsodatFile isofile, string parentClass)
    {
        if (!_registry.TryGetValue(parentClass, out var reader))
            throw new InvalidDataException($"No reader registered for parent class '{parentClass}'");
        PartialStack.Push(null);
        bool popped = false;
        try
        {
            var result = reader(isofile);
            popped = true;
            PartialStack.Pop();
            jo[ParentKey] = result;
            return result;
        }
        catch (IsodatParseException ipe)
        {
            popped = true;
            var partial = PartialStack.Pop();
            if (partial is not null && ipe.PartialResult is not null && ipe.PartialResultClassName is not null)
                partial[ClassToJsonKey(ipe.PartialResultClassName)] = ipe.PartialResult;
            var readerPartial = partial ?? ipe.PartialResult as JsonObject;
            if (readerPartial is not null) jo["parent"] = readerPartial;
            ipe.PartialResult = null;
            ipe.PartialResultClassName = null;
            throw;
        }
        catch (Exception)
        {
            popped = true;
            PartialStack.Pop();
            throw;
        }
        finally
        {
            if (!popped) PartialStack.Pop();
        }
    }

    // Like ReadParent but for named fields: captures partial under the field key rather than
    // letting ReadObject embed it under the class name.
    // Helper: get n_objects from a CBlockData-like JsonObject
    static int NBlockObjects(JsonObject jo) =>
        jo["n_objects"]?.GetValue<int>() ?? 0;

    // Extract the CData "value" string from a reader result.
    // CData-derived classes (CBlockData, CCalibrationPoint, …) store it in ["parent"]["value"].
    // Direct CData reads (CBasicInterface, CGasConfPart, …) store it in ["value"].
    static string? ExtractCDataValue(JsonNode? result)
    {
        // Walk the "parent" chain to find the CData "value" field (may be several levels deep
        // for CBlockData-derived classes like CDevice → CBlockData → CData).
        for (var node = result as JsonObject; node is not null; node = node[ParentKey] as JsonObject)
            if (node[ValueKey] is JsonValue v && v.TryGetValue<string>(out var s)) return s;
        return null;
    }

    static void ValidateBlockNBlockObjects(JsonObject block, int expected)
    {
        int n = NBlockObjects(block);
        if (n != expected)
            throw new InvalidDataException($"expected {expected} children, got {n}");
    }

    static void ValidateBlockValue(JsonObject block, string expected)
    {
        string? actual = block[ParentKey]?[ValueKey]?.GetValue<string>();
        if (actual != expected)
            throw new InvalidDataException(
                $"Expected CBlockData value '{expected}', got '{actual}'");
    }

    static string ToSnakeKey(string className) => className;

    // Add node to the grouped objects dict under its snake_case class key.
    // Single occurrence → direct value.  Multiple → converted to JsonArray.
    // When setIdx is true, sets node["idx"] to the 1-based insertion position across ALL classes
    // in the dict, preserving polymorphic ordering information that would otherwise be lost by
    // grouping.  setIdx is false for noIndex reads (standalone embedded objects, not group members).
    static void AddToObjectsDict(JsonObject dict, string className, JsonNode? node,
                                  bool setIdx = true)
    {
        if (setIdx && node is JsonObject jo)
        {
            int idx = 1;
            foreach (var kvp in dict)
            {
                if (kvp.Key == ParentKey) continue;  // parent is not a block object
                if (kvp.Value is JsonArray arr) idx += arr.Count;
                else if (kvp.Value is JsonObject) idx += 1;
            }
            // Rebuild property order so idx appears first, before parent and any other field.
            var props = jo.ToList();
            jo.Clear();
            jo["idx"] = idx;
            foreach (var (k, v) in props) jo[k] = v;
        }

        string key = ToSnakeKey(className);
        if (!dict.ContainsKey(key))
        {
            dict[key] = node;
        }
        else if (dict[key] is JsonArray arr)
        {
            arr.Add(node);
        }
        else
        {
            var existing = dict[key];
            dict.Remove(key);   // detach so existing can be re-parented
            var newArr = new JsonArray();
            newArr.Add(existing);
            newArr.Add(node);
            dict[key] = newArr;
        }
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
        if (Unabridged) jo["version"] = version;
        jo["runtime_class"] = isofile.ReadMfcString();
        jo["xac"] = isofile.ReadMfcString(); // no named getter; constructor arg3 = descriptive name
        if (version >= 2) jo["xb0"] = isofile.ReadInt32(); // no named getter

        if (version >= 3)
        {
            var block = ReadParent(jo, isofile, "CBlockData");
            ValidateBlockNBlockObjects(block, 2);
            var container = block["objects"]!.AsObject();
            ReadObjectInto(container, isofile, "CTimeObject");
            ReadObjectInto(container, isofile, "CStr");
        }

        if (version >= 4)
        {
            ReadObjectInto(jo, isofile, "CDataIndex", noIndex: true);
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
        TrackPartial(jo);
        int version = isofile.ReadSchemaVersion("CData", 3);
        if (Unabridged) jo["version"] = version;
        int appId = isofile.ReadUInt16();
        if (Unabridged) jo["app_id"] = appId;
        if (Unabridged) jo["label"] = isofile.ReadMfcString(); else isofile.ReadMfcString();
        jo[ValueKey] = isofile.ReadMfcString();
        if (version >= 3)
        {
            int flags = isofile.ReadInt32();
            if (Unabridged) jo["flags"] = flags;
        }
        return jo;
    }

    static JsonObject ReadCCalibrationPoint(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CCalibrationPoint", 3);
        if (Unabridged) jo["version"] = version;
        jo["x94"] = isofile.ReadInt32(); // no named getter
        jo["x98"] = isofile.ReadDouble(); // no named getter
        if (version >= 3)
        {
            jo["xa0"] = isofile.ReadDouble(); // no named getter
            jo["xa8"] = isofile.ReadDouble(); // no named getter
        }
        return jo;
    }

    static JsonObject ReadCMolecule(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CMolecule", 1);
        if (Unabridged) jo["version"] = version;
        jo["molecule"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCTimeObject(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CTimeObject", 1);
        if (Unabridged) jo["version"] = version;
        jo["datetime"] = isofile.ReadTimestamp();
        return jo;
    }

    static JsonObject ReadCISLScriptMessageData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CISLScriptMessageData", 1);
        if (Unabridged) jo["version"] = version;
        jo["display_text"] = isofile.ReadMfcString();
        jo["source_class"] = isofile.ReadMfcString();
        // x9c = 0xFFFFFFFF (-1) and xa0 = 0x00000000 as plain int32 fields.
        // These bytes superficially resemble an MFC new-class header (ff ff ff ff 00 00)
        // but they are raw serialized data, not WriteObject calls.
        jo["x9c"] = isofile.ReadInt32(); // no named getter; init 0xffffffff in constructor
        jo["xa0"] = isofile.ReadInt32(); // no named getter; init 0 in constructor
        return jo;
    }

    static JsonObject ReadCComponent(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CComponent", 1);
        if (Unabridged) jo["version"] = version;
        jo["x94"] = isofile.ReadInt32(); // no named getter; DDX_Time ctrl 0x3eb
        jo["x98"] = isofile.ReadInt32(); // no named getter; DDX_Time ctrl 0x3ec
        jo["xa0"] = isofile.ReadInt32(); // no named getter; low dword of double at xa0 (init ~pi)
        jo["xa4"] = isofile.ReadInt32(); // no named getter; high dword of double at xa0
        return jo;
    }

    static JsonObject ReadCEvalIntegrationUnitHWInfo(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CEvalIntegrationUnitHWInfo", 1);
        if (Unabridged) jo["version"] = version;
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
        TrackPartial(jo);
        int version = isofile.ReadSchemaVersion("CTraceSettings", 4);
        if (Unabridged) jo["version"] = version;
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
            jo["is_redundant"] = isofile.ReadInt32(); // IsRedundant
        }
        return jo;
    }

    static JsonObject ReadCEvalDataItemTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CEvalDataItemTransferPart", 8);
        if (Unabridged) jo["version"] = version;
        jo["id"] = isofile.ReadMfcString();
        jo["name"] = isofile.ReadMfcString();
        jo["format"] = isofile.ReadMfcString();
        jo["gas_name"] = isofile.ReadMfcString();
        jo["element_name"] = isofile.ReadMfcString();
        if (version >= 2) jo["units"] = isofile.ReadMfcString();
        if (version >= 3) jo["info"] = isofile.ReadMfcString();
        if (version >= 5) jo["xb4"] = isofile.ReadInt32(); // no named getter
        if (version >= 6) jo["free_info_string"] = isofile.ReadMfcString(); // GetFreeInfoString / SetFreeInfoString
        if (version >= 7) jo["is_extended_data"] = isofile.ReadInt32(); // IsExtendedData
        if (version >= 8) jo["ampere_calculation"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCPeakDataItem(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvalDataItemTransferPart");
        int version = isofile.ReadSchemaVersion("CPeakDataItem", 1);
        if (Unabridged) jo["version"] = version;
        isofile.ReadMfcString(); // ID recomputed at runtime, discard
        jo["item_idx"] = isofile.ReadInt32(); // SetItemIdx / GetItemIdx
        jo["item_active"] = isofile.ReadInt32(); // SetItemActiv
        return jo;
    }

    static JsonObject ReadCWinColor(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        // Serialize does NOT call parent CData::Serialize but has an embedded CBlockData
        var block = ReadObject(isofile, "CBlockData");
        jo["CBlockData"] = block;
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile, "CTraceLinCol");
        return jo;
    }

    // CTraceLinCol: Serialize does NOT call CData::Serialize
    static JsonObject ReadCTraceLinCol(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        jo["line_color"] = isofile.ReadColor();
        jo["line_type"] = isofile.ReadInt32();
        jo["line_width"] = isofile.ReadInt32();
        return jo;
    }

    // CGridColors: Serialize does NOT call CData::Serialize; 9 COLORREF values
    // All 9 fields form the GRIDCOLORS struct accessed via GetGridColors/SetGridColors;
    // no individual getter names available for the COLORREF members.
    static JsonObject ReadCGridColors(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        jo["x94"] = isofile.ReadColor(); // no named getter; GRIDCOLORS member 0
        jo["x98"] = isofile.ReadColor(); // no named getter; GRIDCOLORS member 1
        jo["x9c"] = isofile.ReadColor(); // no named getter; GRIDCOLORS member 2
        jo["xa0"] = isofile.ReadColor(); // no named getter; GRIDCOLORS member 3
        jo["xa4"] = isofile.ReadColor(); // no named getter; GRIDCOLORS member 4
        jo["xa8"] = isofile.ReadColor(); // no named getter; GRIDCOLORS member 5
        jo["xac"] = isofile.ReadColor(); // no named getter; GRIDCOLORS member 6
        jo["xb0"] = isofile.ReadColor(); // no named getter; GRIDCOLORS member 7
        jo["xb4"] = isofile.ReadColor(); // no named getter; GRIDCOLORS member 8
        return jo;
    }

    // CAxisPara: Serialize does NOT call CData::Serialize
    static JsonObject ReadCAxisPara(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        jo["x94"] = isofile.ReadInt32(); // no named getter; init 0 in Init()
        ReadObjectInto(jo, isofile, "CTraceLinCol");
        return jo;
    }

    static JsonObject ReadCH3FactorResult(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CH3FactorResult", 4);
        if (Unabridged) jo["version"] = version;
        jo["h3_factor"] = isofile.ReadDouble(); // no named getter; double at x98+x9c
        jo["sigma"] = isofile.ReadDouble(); // no named getter; double at xa0+xa4
        if (version >= 2) jo["timestamp"] = isofile.ReadUInt32(); // no named getter; xa8 = CTime
        if (version >= 3) { jo["comment"] = isofile.ReadMfcString(); jo["is_checked"] = isofile.ReadInt32(); } // no named getters; xac, xb8 DDX_Check
        if (version >= 4) { jo["gas_name_1"] = isofile.ReadMfcString(); jo["gas_name_2"] = isofile.ReadMfcString(); jo["xbc"] = isofile.ReadInt32(); } // gas_name_1/2 init "Unknown"; xbc no named getter
        return jo;
    }

    static JsonObject ReadCApplicationData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CApplicationData", 2);
        if (Unabridged) jo["version"] = version;
        jo["x94"] = isofile.ReadUInt32(); // no named getter
        jo["app_id"] = isofile.ReadUInt32(); // Name() = UTIL_GetAppNameFromID(x98)
        jo["x9c"] = isofile.ReadUInt32(); // no named getter
        jo["xa0"] = isofile.ReadUInt16(); // no named getter
        jo["xa4"] = isofile.ReadUInt32(); // no named getter
        jo["xa8"] = isofile.ReadUInt32(); // no named getter
        jo["xac"] = isofile.ReadUInt32(); // no named getter
        jo["xb0"] = isofile.ReadUInt32(); // no named getter
        return jo;
    }

    static JsonObject ReadCResultForGas(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CResultForGas", 1);
        if (Unabridged) jo["version"] = version;
        jo["eval_name"] = isofile.ReadMfcString(); // GetEvalName
        jo["eval_list"] = isofile.ReadMfcString(); // GetEvalList
        ReadObjectInto(jo, isofile);
        return jo;
    }

    static JsonObject ReadCPeakFindParameter(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int v = isofile.ReadSchemaVersion("CPeakFindParameter", 11);
        if (Unabridged) jo["version"] = v;

        // always present (v1+)
        jo["calc_slope_algorithm"] = isofile.ReadMfcString();   // x94
        jo["x98"] = isofile.ReadUInt32();                       // x98
        jo["peak_find_algorithm"] = isofile.ReadMfcString();    // x9c
        jo["int_peak_algorithm"] = isofile.ReadMfcString();     // xa0
        jo["xa8"] = isofile.ReadDouble();                       // xa8
        jo["xb0"] = isofile.ReadDouble();                       // xb0
        jo["xb8"] = isofile.ReadDouble();                       // xb8
        jo["xc0"] = isofile.ReadDouble();                       // xc0
        jo["xc8"] = isofile.ReadDouble();                       // xc8
        jo["xd0"] = isofile.ReadDouble();                       // xd0
        jo["xd8"] = isofile.ReadUInt32();                       // xd8: detection trace
        jo["xf8"] = isofile.ReadDouble();                       // xf8
        jo["x100"] = isofile.ReadUInt32();                      // x100
        jo["x108"] = isofile.ReadDouble();                      // x108
        jo["x110"] = isofile.ReadDouble();                      // x110
        jo["x118"] = isofile.ReadUInt32();                      // x118
        jo["bgd_algorithm"] = isofile.ReadMfcString();          // x11c
        jo["smoothing_algorithm"] = isofile.ReadMfcString();    // x120
        int nTau = (int)isofile.ReadUInt32();                   // tau count (x150)
        jo["n_tau"] = nTau;
        jo["x148"] = isofile.ReadDouble();                      // x148
        jo["x158"] = isofile.ReadDouble();                      // x158
        jo["x160"] = isofile.ReadDouble();                      // x160

        // version-dependent tau block
        if (v == 2)
        {
            jo["x128"] = isofile.ReadDouble();                  // tau initial
            jo["x130"] = isofile.ReadDouble();                  // tau default (all entries)
        }
        else if (v >= 3)
        {
            jo["x128"] = isofile.ReadDouble();                  // tau initial
            var taus = new JsonArray();
            for (int i = 0; i < nTau; i++) taus.Add(isofile.ReadDouble());
            jo["tau_values"] = taus;                            // x138 array
            jo["x13c"] = isofile.ReadUInt32();                  // tau mode
            if (v >= 4)
            {
                jo["x170"] = isofile.ReadUInt32();
                if (v > 5) jo["gas_name"] = isofile.ReadMfcString(); // x180
            }
        }

        if (v > 6)
        {
            jo["square_pulse_detection_enabled"] = isofile.ReadUInt32(); // x190
            jo["square_pulse_detection_factor"] = isofile.ReadDouble();  // x198
        }
        if (v > 7)
        {
            jo["x174"] = isofile.ReadUInt32();
            jo["x178"] = isofile.ReadDouble();
        }
        if (v > 8)
        {
            jo["xe8"] = isofile.ReadDouble();
            jo["xf0"] = isofile.ReadDouble();
            jo["x184"] = isofile.ReadUInt32();
        }
        if (v > 9) jo["xe0"] = isofile.ReadDouble();
        if (v > 10) jo["x140"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCMRI_DilutionList(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CMRI_DilutionList", 1);
        if (Unabridged) jo["version"] = version;
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
        TrackPartial(jo);
        int version = isofile.ReadSchemaVersion("CSimple", 2);
        if (Unabridged) jo["version"] = version;
        if (Unabridged) jo["label"] = isofile.ReadMfcString(); else isofile.ReadMfcString();
        return jo;
    }

    public static JsonObject ReadCStr(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CSimple");
        int version = isofile.ReadSchemaVersion("CStr", 2);
        if (Unabridged) jo["version"] = version;
        jo[ValueKey] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCDword(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CSimple");
        int version = isofile.ReadSchemaVersion("CDword", 2);
        if (Unabridged) jo["version"] = version;
        jo[ValueKey] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCInt(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CSimple");
        int version = isofile.ReadSchemaVersion("CInt", 2);
        if (Unabridged) jo["version"] = version;
        jo[ValueKey] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCDouble(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CSimple");
        int version = isofile.ReadSchemaVersion("CDouble", 2);
        if (Unabridged) jo["version"] = version;
        jo[ValueKey] = isofile.ReadDouble();
        return jo;
    }

    static JsonObject ReadCBinary(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CSimple");
        int version = isofile.ReadSchemaVersion("CBinary", 2);
        if (Unabridged) jo["version"] = version;
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
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CBlockData", 2);
        if (Unabridged) jo["version"] = version;
        int n = isofile.ReadInt32();
        jo["n_objects"] = n;
        jo["objects"] = new JsonObject();
        isofile.SetObjectLogNBlockObjects(isofile.ObjectLog.Count - 1, n);
        return jo;
    }

    public static JsonObject ReadCDataIndex(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        ValidateBlockNBlockObjects(block, 0);
        isofile.ReadInt32(); // trailing sentinel (always 1)
        return jo;
    }

    static JsonObject ReadCCalibration(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile); // CCalibrationPoint/CCalibrationParameter/CDouble
        int version = isofile.ReadSchemaVersion("CCalibration", 5);
        if (Unabridged) jo["version"] = version;
        jo["cal_type"] = isofile.ReadUInt8(); // no named getter; DDX_Text ctrl 0x3eb
        jo["description"] = isofile.ReadMfcString(); // no named getter; DDX_Text ctrl 0x3ec
        jo["xb0"] = isofile.ReadTimestamp();
        if (version < 5) isofile.ReadDouble(); // legacy
        jo["xbc"] = isofile.ReadInt32(); // no named getter
        if (version >= 3) jo["xc0"] = isofile.ReadUInt8(); // no named getter; DDX_Text ctrl 0x3ee
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
            if (Unabridged) jo["splines"] = splines;
        }
        return jo;
    }

    // CCalibrationParameter::Serialize (CalibrationDll.dll):
    //   parent CData + schema v2
    //   0x94 uint32 (default 300)
    //   0x98 uint32 (default 2)
    //   0x9c uint8  (default 90)
    //   0xa0 uint32 (default 2000)
    //   v>=2: 0xa4 uint32 (default 2)
    //   v>=2: 0xa8 uint32 (default 5)
    static JsonObject ReadCCalibrationParameter(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int v = isofile.ReadSchemaVersion("CCalibrationParameter", 2);
        if (Unabridged) jo["version"] = v;
        jo["x94"] = isofile.ReadUInt32();
        jo["x98"] = isofile.ReadUInt32();
        jo["x9c"] = isofile.ReadUInt8();
        jo["xa0"] = isofile.ReadUInt32();
        if (v >= 2)
        {
            jo["xa4"] = isofile.ReadUInt32();
            jo["xa8"] = isofile.ReadUInt32();
        }
        return jo;
    }

    // CPeakDetectionParameter::Serialize (IsodatClasses.dll):
    //   parent CData + schema v5
    //   v1: 4x double (0x98, 0xa0, 0xa8, 0xb0), uint32 smooth_factor (0xc0)
    //   v2: uint8 (0xb8, default 50)
    //   v3: uint32 (0xbc, default 1)
    //   v4: uint32 has_next + if non-zero ReadObject(CPeakDetectionParameter) [linked list]
    //   v5: uint8 (0xb9, default 80)
    static JsonObject ReadCPeakDetectionParameter(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int v = isofile.ReadSchemaVersion("CPeakDetectionParameter", 5);
        if (Unabridged) jo["version"] = v;
        jo["x98"] = isofile.ReadDouble();
        jo["xa0"] = isofile.ReadDouble();
        jo["xa8"] = isofile.ReadDouble();
        jo["xb0"] = isofile.ReadDouble();
        jo["smooth_factor"] = isofile.ReadUInt32();
        if (v >= 2) jo["xb8"] = isofile.ReadUInt8();
        if (v >= 3) jo["xbc"] = isofile.ReadUInt32();
        if (v >= 4)
        {
            long hasNext = isofile.ReadUInt32();
            if (hasNext != 0) ReadObjectInto(jo, isofile, "CPeakDetectionParameter");
        }
        if (v >= 5) jo["xb9"] = isofile.ReadUInt8();
        return jo;
    }

    static JsonObject ReadCVisualisationData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        ValidateBlockNBlockObjects(block, 0);
        int version = isofile.ReadSchemaVersion("CVisualisationData", 8);
        if (Unabridged) jo["version"] = version;

        jo["plot_rect"] = ReadIntArray(isofile, 4); // no named getter; RECT at xa8 (init 0,0,100,100)
        jo["trace_flags"] = ReadIntArray(isofile, 10); // no named getter; int[10] at xb8 (init all 1)
        jo["plot_flags"] = ReadIntArray(isofile, 10); // no named getter; int[10] at xe0

        if (version >= 2)
        {
            jo["font"] = isofile.ReadMfcString();
            jo["x10c"] = isofile.ReadMfcString(); // no named getter
            jo["x110"] = isofile.ReadMfcString(); // no named getter
            if (version >= 3)
            {
                jo["x120"] = isofile.ReadInt32(); // no named getter
                if (version >= 4)
                {
                    jo["x124"] = isofile.ReadInt32(); // no named getter
                    if (version >= 5)
                    {
                        jo["x148"] = isofile.ReadMfcString(); // no named getter
                        if (version >= 6)
                        {
                            jo["x11c"] = isofile.ReadInt32(); // no named getter
                            if (version >= 7)
                            {
                                jo["x128"] = isofile.ReadInt32(); // no named getter
                                if (version >= 8)
                                    jo["x12c"] = isofile.ReadInt32(); // no named getter
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
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);
        int version = isofile.ReadSchemaVersion("CGasConfiguration", 3);
        if (Unabridged) jo["version"] = version;
        if (version >= 3) jo["timestamp"] = isofile.ReadTimestamp();
        return jo;
    }

    static JsonObject ReadCMeasurmentInfos(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);
        isofile.ReadInt32(); // trailing sentinel (always 1)
        return jo;
    }

    static JsonObject ReadCMeasurmentErrors(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);
        isofile.ReadInt32(); // trailing sentinel (always 1)
        return jo;
    }

    static JsonObject ReadCPlotSettings(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);
        int version = isofile.ReadSchemaVersion("CPlotSettings", 5);
        if (Unabridged) jo["version"] = version;
        if (version >= 2) { jo["xb0"] = isofile.ReadMfcString(); jo["configuration_name"] = isofile.ReadMfcString(); } // xb0: no named getter; configuration_name: GetConfigurationName
        if (version >= 3) jo["peak_labelling"] = isofile.ReadInt32();
        if (version >= 4) jo["refresh_data_grid"] = isofile.ReadInt32();
        if (version >= 5) jo["ampere_calc_flag"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCWinSettings(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);
        int version = isofile.ReadSchemaVersion("CWinSettings", 4);
        if (Unabridged) jo["version"] = version;
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
        jo["x10c"] = isofile.ReadInt32(); // no named getter; init 0
        jo["x110"] = isofile.ReadInt32(); // no named getter; init 0
        jo["x114"] = isofile.ReadInt32(); // no named getter; init 0
        jo["x118"] = isofile.ReadInt32(); // no named getter; init 0
        ReadObjectInto(jo, isofile, "CViewColors", noIndex: true);
        if (!Unabridged) jo.Remove("CViewColors");
        if (version == 2)
        {
            isofile.AddWarning("CWinSettings v2: reading legacy object (untested)");
            ReadObject(isofile); // discard
        }
        if (version >= 4) jo["ampere_calc_flag"] = isofile.ReadInt32(); // GetAmpereCalcFlag / SetAmpereCalcFlag
        return jo;
    }

    static JsonObject ReadCViewColors(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        ValidateBlockNBlockObjects(block, 3);
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);
        // 5 COLORREF values form the PLOT_COLORS struct (GetPlotColors/SetPlotColors);
        // no individual getter names available.
        jo["xa8"] = isofile.ReadColor(); // no named getter; PLOT_COLORS member 0
        jo["xac"] = isofile.ReadColor(); // no named getter; PLOT_COLORS member 1
        jo["xb0"] = isofile.ReadColor(); // no named getter; PLOT_COLORS member 2
        jo["xb4"] = isofile.ReadColor(); // no named getter; PLOT_COLORS member 3
        jo["xb8"] = isofile.ReadColor(); // no named getter; PLOT_COLORS member 4
        return jo;
    }

    static JsonObject ReadCGasSettings(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);
        int version = isofile.ReadSchemaVersion("CGasSettings", 5);
        if (Unabridged) jo["version"] = version;
        ReadObjectInto(jo, isofile, "CPkDataItemList");
        if (version >= 2) jo["gas"] = isofile.ReadMfcString();
        if (version >= 3)
        {
            int hasShrink = isofile.ReadInt32();
            if (hasShrink != 0)
            {
                ReadObjectInto(jo, isofile, "CShrinkInfo");
                if (!Unabridged) jo.Remove("CShrinkInfo");
            }
        }
        if (version >= 4) jo["eval_list"] = isofile.ReadMfcString();
        if (version >= 5) jo["ampere_calc_flag"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCPkDataItemList(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);
        int version = isofile.ReadSchemaVersion("CPkDataItemList", 1);
        if (Unabridged) jo["version"] = version;
        jo["current_item_idx"] = isofile.ReadInt32(); // iterator cursor used in GetNextItem; init -1
        return jo;
    }

    static JsonObject ReadCAllMoleculeWeights(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        ValidateBlockNBlockObjects(block, 0);
        int version = isofile.ReadSchemaVersion("CAllMoleculeWeights", 2);
        if (Unabridged) jo["version"] = version;
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
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);

        int version = isofile.ReadSchemaVersion("CMethod", 10);
        if (Unabridged) jo["version"] = version;
        ReadObjectInto(jo, isofile, "CConfiguration", noIndex: true);
        jo["gas_config_name"] = isofile.ReadMfcString(); // C++ xb4; from CGasConfiguration::GetActiveName
        jo["gas_name"] = isofile.ReadMfcString(); // C++ xb8; no named getter
        jo["description"] = isofile.ReadMfcString(); // C++ xbc; no named getter

        // N CDeviceMethodPart objects (polymorphic — concrete subclass in stream)
        int nDeviceParts = isofile.ReadInt32();
        jo["n_device_parts"] = nDeviceParts;
        for (int i = 0; i < nDeviceParts; i++)
            ReadObjectInto(jo, isofile, pattern: "DeviceMethodPart", groupTag: 1, groupDeclaredSize: nDeviceParts);

        if (version >= 2)
        {
            int nEvalParts = isofile.ReadInt32();
            jo["n_eval_parts"] = nEvalParts;
            for (int i = 0; i < nEvalParts; i++)
                ReadObjectInto(jo, isofile, pattern: "DeviceEvaluationPart", groupTag: 2, groupDeclaredSize: nEvalParts);
        }

        if (version >= 3) jo["acq_type"] = isofile.ReadInt32();
        if (version >= 4) jo["acq_type_name"] = isofile.ReadMfcString(); // C++ xc0; from CDevice::GetAcqTypeName

        if (version >= 5)
        {
            int nSubMethods = isofile.ReadInt32();
            for (int i = 0; i < nSubMethods; i++)
                ReadObjectInto(jo, isofile, "CMethod", groupTag: 3, groupDeclaredSize: nSubMethods);
        }

        if (version >= 6) jo["xd0"] = isofile.ReadMfcString(); // no named getter; C++ xd0
        if (version >= 7) jo["correction_descriptors"] = isofile.ReadInt32(); // C++ xcc; CorrectionDescriptors_Update flag; init 0
        if (version >= 9) { jo["xd4"] = isofile.ReadInt32(); jo["xd8"] = isofile.ReadInt32(); } // no named getters; C++ xd4, xd8; init 1
        if (version >= 10) jo["xdc"] = isofile.ReadInt32(); // no named getter; C++ xdc; init 0

        return jo;
    }

    static JsonObject ReadCConfiguration(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);
        int version = isofile.ReadSchemaVersion("CConfiguration", 7);
        if (Unabridged) jo["version"] = version;
        if (version >= 3) jo["acq_type"] = isofile.ReadInt32(); // GetAcqType / SetAcqType
        if (version >= 4) jo["xac"] = isofile.ReadInt32(); // no named getter
        if (version >= 5) jo["xb0"] = isofile.ReadMfcString(); // no named getter
        if (version >= 6) jo["display_compound_ratios"] = isofile.ReadInt32(); // DisplayCompoundRatios
        if (version >= 7) jo["application_mode_idx"] = isofile.ReadInt32(); // SetApplicationMode / GetApplicationMode; init -1 then searched
        return jo;
    }

    static JsonObject ReadCComponentList(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);
        int version = isofile.ReadSchemaVersion("CComponentList", 1);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    static JsonObject ReadCParsedEvaluationStringArray(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        // children are CParsedEvaluationString objects
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);
        int version = isofile.ReadSchemaVersion("CParsedEvaluationStringArray", 4);
        if (Unabridged) jo["version"] = version;
        jo["xa8"] = isofile.ReadMfcString();
        if (version >= 2) jo["xb0"] = isofile.ReadUInt32();
        if (version >= 3) { jo["xb8"] = isofile.ReadUInt32(); jo["xbc"] = isofile.ReadUInt32(); }
        if (version >= 4) jo["xc0"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCResultArray(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);
        int version = isofile.ReadSchemaVersion("CResultArray", 2);
        if (Unabridged) jo["version"] = version;
        jo["xa8"] = isofile.ReadUInt32();
        if (version >= 2) jo["xac"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCActionScript(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);
        int version = isofile.ReadSchemaVersion("CActionScript", 5);
        if (Unabridged) jo["version"] = version;
        if (version >= 3) ReadObjectInto(jo, isofile, "CApplicationData");
        if (version >= 4) jo["x168"] = isofile.ReadUInt32();
        if (version >= 5) { jo["x1c0"] = isofile.ReadMfcString(); jo["x1c4"] = isofile.ReadUInt32(); }
        return jo;
    }

    static JsonObject ReadCGCPeakList(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile, expected: "CSPeak");
        int v = isofile.ReadSchemaVersion("CGCPeakList", 6);
        if (Unabridged) jo["version"] = v;
        jo["xc4"] = isofile.ReadUInt32();
        jo["n_traces"] = isofile.ReadUInt32();                  // xc8; InitList arg
        if (v > 1) jo["highest_peak_number"] = isofile.ReadUInt32(); // xcc; GetHighestPeakNumber
        if (v > 2)
        {
            jo["last_peak_number"] = isofile.ReadUInt32();      // xa8; set in AddPeak from GetPeakNumber
            jo["xac"] = isofile.ReadMfcString();
        }
        if (v > 3) jo["xb0"] = isofile.ReadMfcString();
        if (v > 4) jo["xb8"] = isofile.ReadMfcString();
        if (v > 5) jo["xc0"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCVisualisationDialogNamesBlockData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);
        isofile.ReadInt32();  // constant 1, discarded on load (no schema version in Serialize)
        return jo;
    }

    static JsonObject ReadCEvalDataItemListTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile);
        int version = isofile.ReadSchemaVersion("CEvalDataItemListTransferPart", 1);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    // =======================================================================
    // CDevice chain
    // =======================================================================

    static JsonObject ReadCDevice(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        var deviceBlockObjects = block["objects"]!.AsObject();
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(deviceBlockObjects, isofile);
        int version = isofile.ReadSchemaVersion("CDevice", 5);
        if (Unabridged) jo["version"] = version;
        jo["xac"] = isofile.ReadUInt32(); // no named getter; v1+
        jo["xb0"] = isofile.ReadUInt32(); // no named getter; v1+
        if (version >= 3) jo["xa8"] = isofile.ReadUInt32(); // no named getter; v3+
        if (version >= 4) jo["xb4"] = isofile.ReadUInt32(); // no named getter; v4+
        if (version >= 5) jo["xb8"] = isofile.ReadMfcString(); // no named getter; v5+; CString (CDaoIndexFieldInfo at xb8)
        return jo;
    }

    static JsonObject ReadCActiveDevice(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CDevice");
        int version = isofile.ReadSchemaVersion("CActiveDevice", 2);
        if (Unabridged) jo["version"] = version;
        if (version >= 2) jo["calib_table_file_name"] = isofile.ReadMfcString(); // GetCalibTableFileName / SetCalibTableFileName
        return jo;
    }

    // CBufferedRefillDevice::Serialize = CActiveDevice::Serialize + one int32 (always written as 1, discarded on read)
    static JsonObject ReadCBufferedRefillDevice(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CActiveDevice");
        jo["xf0"] = isofile.ReadInt32(); // no named getter; always 1
        return jo;
    }

    // CXCaliburDevice::Serialize (DevicesDll.dll):
    //   parent CActiveDevice + schema v4
    //   v1: method_ext (0xd0), device_name (0xfc), x104 (0x104 CString), x10c uint32
    //   v2: needs_raw_file (0x110), is_exclusive (0x114)
    //       [0x128 always 0 from ctor → no command string array written/read]
    //   v3: ext_method_interface_handling (0x118)
    //   v4: device_desc_name (0x100)
    static JsonObject ReadCXCaliburDevice(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CActiveDevice");
        int v = isofile.ReadSchemaVersion("CXCaliburDevice", 4);
        if (Unabridged) jo["version"] = v;
        jo["method_ext"] = isofile.ReadMfcString();
        jo["device_name"] = isofile.ReadMfcString();
        jo["x104"] = isofile.ReadMfcString();
        jo["x10c"] = isofile.ReadUInt32();
        if (v >= 2)
        {
            jo["needs_raw_file"] = isofile.ReadUInt32();
            jo["is_exclusive"] = isofile.ReadUInt32();
        }
        if (v >= 3) jo["ext_method_interface_handling"] = isofile.ReadUInt32();
        if (v >= 4) jo["device_desc_name"] = isofile.ReadMfcString();
        return jo;
    }

    // CTraceBasicDevice::Serialize (TraceGcDll.dll):
    //   parent CXCaliburDevice + sentinel uint32 (always 1)
    static JsonObject ReadCTraceBasicDevice(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CXCaliburDevice");
        jo["xcal_sentinel"] = isofile.ReadUInt32(); // always 1
        return jo;
    }

    // CTrace_II_Device::Serialize (TraceGcDll.dll):
    //   parent CTraceBasicDevice + sentinel uint32 (always 1)
    static JsonObject ReadCTrace_II_Device(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CTraceBasicDevice");
        jo["trace2_sentinel"] = isofile.ReadUInt32(); // always 1
        return jo;
    }

    // CXcalRSH2Device::Serialize (TriPlusDll.dll):
    //   parent CXCaliburDevice + sentinel uint32 (always 1)
    // CXcalRSHDevice uses this Serialize unchanged (no override in vftable)
    static JsonObject ReadCXcalRSH2Device(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CXCaliburDevice");
        jo["rsh_sentinel"] = isofile.ReadUInt32(); // always 1
        return jo;
    }

    static JsonObject ReadCConFloDevice(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CActiveDevice");
        int version = isofile.ReadSchemaVersion("CConFloDevice", 1);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    static JsonObject ReadCActivePort(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        var portBlockObjects = block["objects"]!.AsObject();
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(portBlockObjects, isofile);
        int version = isofile.ReadSchemaVersion("CActivePort", 2);
        if (Unabridged) jo["version"] = version;
        if (version >= 2) jo["xa8"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCMsDevice(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CActiveDevice");
        int version = isofile.ReadSchemaVersion("CMsDevice", 2);
        if (Unabridged) jo["version"] = version;
        jo["xfc"] = isofile.ReadUInt32(); // no named getter; ComboBox selection (init 2); DDX via combo at ctrl 0x411
        if (version >= 2) jo["x100"] = isofile.ReadUInt32(); // no named getter; DDX_Check ctrl 0x40f (init 1)
        return jo;
    }

    static JsonObject ReadCGenericGcDevice(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CActiveDevice");
        int version = isofile.ReadSchemaVersion("CGenericGcDevice", 2);
        if (Unabridged) jo["version"] = version;
        if (version >= 2) jo["device_start_time"] = isofile.ReadUInt32(); // SetDeviceStartTime (GetTickCount at init)
        return jo;
    }

    static JsonObject ReadCFlashEA_Device(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CGenericGcDevice");
        int version = isofile.ReadSchemaVersion("CFlashEA_Device", 1);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    // =======================================================================
    // IsoGCEvalData / CEvalDataStorage chain
    // =======================================================================

    static JsonObject ReadIsoGCEvalData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("IsoGCEvalData", 1);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    static JsonObject ReadCGCData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        jo["p_iso_gc_eval_data"] = ReadIsoGCEvalData(isofile);
        int version = isofile.ReadSchemaVersion("CGCData", 1);
        if (Unabridged) jo["version"] = version;
        ReadObjectInto(jo, isofile, "CEvalGCData");
        return jo;
    }

    static JsonObject ReadCRawData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CGCData");
        int version = isofile.ReadSchemaVersion("CRawData", 5);
        if (Unabridged) jo["version"] = version;

        if (version <= 1) return jo;

        jo["complete_formula"] = isofile.ReadMfcString();
        jo["formula"] = isofile.ReadMfcString();
        int nMasses = isofile.ReadInt32();
        jo["n_masses"] = nMasses;
        if (nMasses > 0) jo["masses"] = ReadIntArray(isofile, nMasses);

        ReadObjectInto(jo, isofile, "CAllMoleculeWeights");

        if (version > 2) jo["x1048"] = isofile.ReadInt32();
        if (version > 3) ReadObjectInto(jo, isofile, "CStringArray");
        if (version > 4)
        {
            jo["xf88"] = isofile.ReadInt32();
            int flag = isofile.ReadInt32();
            if (flag != 0)
                jo["integration_unit_gas_conf_part"] =
                    ReadObject(isofile, "CIntegrationUnitGasConfPart");
        }
        return jo;
    }

    // CEvalDataStorage: Serialize does NOT call CData/CBlockData
    static JsonObject ReadCEvalDataStorage(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        int version = isofile.ReadSchemaVersion("CEvalDataStorage", 1);
        if (Unabridged) jo["version"] = version;
        int nBytes = isofile.ReadInt32();
        if (Unabridged) jo["n_bytes"] = nBytes;
        // Buffer is always stored as base64 here; ReadCEvalFakeData parses it once n_traces is known.
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
        TrackPartial(jo);
        var storage = ReadParent(jo, isofile, "CEvalDataStorage");
        int version = isofile.ReadSchemaVersion("CEvalFakeData", 1);
        if (Unabridged) jo["version"] = version;
        int nTraces = isofile.ReadInt32();
        jo["n_traces"] = nTraces;

        if (storage["buffer"] is JsonValue bufNode && nTraces > 0)
        {
            byte[] buf = Convert.FromBase64String(bufNode.GetValue<string>());
            int stride = 4 + nTraces * 8;  // float time + nTraces doubles
            int n = buf.Length / stride;
            var time = new JsonArray();
            var traces = Enumerable.Range(0, nTraces).Select(_ => new JsonArray()).ToArray();
            for (int i = 0; i < n; i++)
            {
                int off = i * stride;
                time.Add(JsonValue.Create(BitConverter.ToSingle(buf, off)));
                for (int t = 0; t < nTraces; t++)
                    traces[t].Add(JsonValue.Create(BitConverter.ToDouble(buf, off + 4 + t * 8)));
            }
            storage["time"] = time;
            storage["traces"] = new JsonArray(traces.Select(a => (JsonNode)a).ToArray());
            if (!Unabridged) storage.Remove("buffer");
        }

        return jo;
    }

    static JsonObject ReadCEvalGCData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvalFakeData");
        int version = isofile.ReadSchemaVersion("CEvalGCData", 1);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    // =======================================================================
    // CBasicInterface chain (= CData)
    // =======================================================================

    static JsonObject ReadCFinniganInterface(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CFinniganInterface", 6);
        if (Unabridged) jo["version"] = version;
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
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CGpibInterface", 3);
        if (Unabridged) jo["version"] = version;
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
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CTransferPart", 2);
        if (Unabridged) jo["version"] = version;
        jo["x9c"] = isofile.ReadInt32();
        jo["xa0"] = isofile.ReadInt32(); // seems it's always 0
        return jo;
    }

    static JsonObject ReadCAdcTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CTransferPart");
        int version = isofile.ReadSchemaVersion("CAdcTransferPart", 2);
        if (Unabridged) jo["version"] = version;
        jo["raw_value"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCMagnetCurrentTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CAdcTransferPart");
        jo["xa8"] = isofile.ReadBool32();
        return jo;
    }

    // =======================================================================
    // CGasConfPart chain
    // =======================================================================

    static JsonObject ReadCIntegrationUnitGasConfPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CIntegrationUnitGasConfPart", 2);
        if (Unabridged) jo["version"] = version;
        int n = isofile.ReadUInt8();
        jo["n_configs"] = n;
        for (int i = 0; i < n; i++) ReadObjectInto(jo, isofile, "CChannelGasConfPart", groupTag: 1, groupDeclaredSize: n);
        return jo;
    }

    static JsonObject ReadCChannelGasConfPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CChannelGasConfPart", 4);
        if (Unabridged) jo["version"] = version;
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
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CBasicScan", 4);
        if (Unabridged) jo["version"] = version;
        ReadObjectInto(jo, isofile, pattern: "ScanPart");
        ReadObjectInto(jo, isofile, pattern: "ScanPart");
        var block = ReadObject(isofile, "CBlockData");
        jo["CBlockData"] = block;
        ValidateBlockNBlockObjects(block, 0);
        jo["x04"] = isofile.ReadInt32();
        jo["xa4"] = isofile.ReadInt32();
        if (version >= 4) jo["x94"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCScanPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CScanPart", 3);
        if (Unabridged) jo["version"] = version;
        ReadObjectInto(jo, isofile, pattern: "HardwarePart");
        jo["xa0"] = isofile.ReadInt32();
        jo["xa4"] = isofile.ReadInt32();
        jo["xb0"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCClockScanPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CScanPart");
        int version = isofile.ReadSchemaVersion("CClockScanPart", 2);
        if (Unabridged) jo["version"] = version;
        jo["scan_time"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCScaleHvScanPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CScanPart");
        int version = isofile.ReadSchemaVersion("CScaleHvScanPart", 2);
        if (Unabridged) jo["version"] = version;
        jo["start"] = isofile.ReadInt32();
        jo["stop"] = isofile.ReadInt32();
        jo["step"] = isofile.ReadInt32();
        jo["delay"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCMagnetCurrentScanPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CScanPart");
        int version = isofile.ReadSchemaVersion("CMagnetCurrentScanPart", 2);
        if (Unabridged) jo["version"] = version;
        jo["start"] = isofile.ReadInt32();
        jo["stop"] = isofile.ReadInt32();
        jo["step"] = isofile.ReadInt32();
        jo["delay"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCIntegrationUnitScanPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CScanPart");
        int version = isofile.ReadSchemaVersion("CIntegrationUnitScanPart", 3);
        if (Unabridged) jo["version"] = version;
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
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CHardwarePart", 10);
        if (Unabridged) jo["version"] = version;
        ReadObjectInto(jo, isofile, pattern: "Interface");
        bool hasGas = isofile.ReadBool32();
        jo["has_c_gas_conf_part"] = hasGas;
        if (hasGas) ReadObjectInto(jo, isofile, pattern: "GasConfPart");
        bool hasMethod = isofile.ReadBool32();
        jo["has_c_method_part"] = hasMethod;
        if (hasMethod)
            throw new InvalidDataException("CHardwarePart: non-zero CMethodPart not implemented");
        bool hasExtra = isofile.ReadBool32();
        jo["has_extra_c_data"] = hasExtra;
        if (hasExtra) ReadObjectInto(jo, isofile);

        if (version >= 3)
        {
            jo["xac"] = isofile.ReadBool32();
            jo["xb0"] = isofile.ReadBool32();
            jo["xb4"] = isofile.ReadBool32();
            jo["xb8"] = isofile.ReadBool32();
            if (version >= 7)
            {
                ReadObjectInto(jo, isofile, "CVisualisationData");
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
        TrackPartial(jo);
        ReadParent(jo, isofile, "CHardwarePart");
        int version = isofile.ReadSchemaVersion("CCupHardwarePart", 5);
        if (Unabridged) jo["version"] = version;
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
        TrackPartial(jo);
        ReadParent(jo, isofile, "CHardwarePart");
        int version = isofile.ReadSchemaVersion("CChannelHardwarePart", 2);
        if (Unabridged) jo["version"] = version;
        jo["x120"] = isofile.ReadInt32();
        jo["x124"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCScaleHardwarePart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CHardwarePart");
        int version = isofile.ReadSchemaVersion("CScaleHardwarePart", 12);
        if (Unabridged) jo["version"] = version;
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
        TrackPartial(jo);
        ReadParent(jo, isofile, "CScaleHardwarePart");
        int version = isofile.ReadSchemaVersion("CClockHardwarePart", 2);
        if (Unabridged) jo["version"] = version;
        jo["x190"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCIntegrationUnitHardwarePart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CScaleHardwarePart");
        int version = isofile.ReadSchemaVersion("CIntegrationUnitHardwarePart", 3);
        if (Unabridged) jo["version"] = version;
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
        for (int i = 0; i < nCups; i++) ReadObjectInto(jo, isofile, "CCupHardwarePart", groupTag: 1, groupDeclaredSize: nCups);
        int nChan = isofile.ReadUInt8();
        jo["n_channels"] = nChan;
        for (int i = 0; i < nChan; i++) ReadObjectInto(jo, isofile, "CChannelHardwarePart", groupTag: 2, groupDeclaredSize: nChan);
        if (version >= 3) { jo["x1a8"] = isofile.ReadBool32(); jo["x1ac"] = isofile.ReadBool32(); }
        return jo;
    }

    static JsonObject ReadCDacHardwarePart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CScaleHardwarePart");
        int version = isofile.ReadSchemaVersion("CDacHardwarePart", 3);
        if (Unabridged) jo["version"] = version;
        jo["x190"] = isofile.ReadUInt8(); jo["x191"] = isofile.ReadUInt8();
        jo["x192"] = isofile.ReadUInt8(); jo["x193"] = isofile.ReadUInt8();
        if (version >= 3) jo["format"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCScaleHvHardwarePart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CDacHardwarePart");
        int version = isofile.ReadSchemaVersion("CScaleHvHardwarePart", 3);
        if (Unabridged) jo["version"] = version;
        if (version >= 3) jo["x198"] = isofile.ReadDouble();
        return jo;
    }

    static JsonObject ReadCMagnetCurrentHardwarePart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CDacHardwarePart");
        int version = isofile.ReadSchemaVersion("CMagnetCurrentHardwarePart", 2);
        if (Unabridged) jo["version"] = version;
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
        TrackPartial(jo);
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
        ReadObjectInto(jo, isofile, "CTraceInfo");
        ReadObjectInto(jo, isofile, "CPlotRange");
        ReadObjectInto(jo, isofile, "CPlotRange");
        if (version > 1) { jo["x08"] = isofile.ReadInt32(); jo["x0c"] = isofile.ReadInt32(); }
        jo["plot_range_zoom2"] = ReadCPlotRange(isofile);
        jo["plot_range2"] = ReadCPlotRange(isofile);
        int nTraces = jo["CTraceInfo"]?["n_traces"]?.GetValue<int>() ?? 0;
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
        TrackPartial(jo);
        jo["x04"] = isofile.ReadInt32();
        int nTraces = isofile.ReadUInt8();
        jo["n_traces"] = nTraces;
        for (int i = 0; i < nTraces; i++) ReadObjectInto(jo, isofile, "CTraceInfoEntry");
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
        TrackPartial(jo);
        jo["idx"] = isofile.ReadUInt8();
        jo["x05"] = Convert.ToBase64String(isofile.ReadBytes(1));
        jo["trace_color"] = isofile.ReadColor();
        jo["x0c"] = isofile.ReadInt32();
        jo["x10"] = isofile.ReadInt32();
        jo["x14"] = isofile.ReadInt32();
        return jo;
    }

    // CPlotRange registered as a named readable object
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
        TrackPartial(jo);
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
        TrackPartial(jo);
        int version = isofile.ReadSchemaVersion("CParsedEvaluationString", 2);
        if (Unabridged) jo["version"] = version;
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
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CAction", 6);
        if (Unabridged) jo["version"] = version;
        if (version >= 3) jo["x94"] = isofile.ReadInt32();
        if (version >= 4) jo["xb0"] = isofile.ReadMfcString();
        if (version >= 5) jo["x9c"] = isofile.ReadInt32();
        if (version >= 6) jo["xa0"] = isofile.ReadInt32();
        return jo;
    }

    static JsonObject ReadCActionPeakCenter(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CAction");
        int version = isofile.ReadSchemaVersion("CActionPeakCenter", 1);
        if (Unabridged) jo["version"] = version;
        jo["xbc"] = isofile.ReadUInt32();
        jo["xb8"] = isofile.ReadUInt8();
        return jo;
    }

    static JsonObject ReadCActionHwTransferContainer(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CAction");
        int version = isofile.ReadSchemaVersion("CActionHwTransferContainer", 2);
        if (Unabridged) jo["version"] = version;
        jo["xb8"] = isofile.ReadUInt32();
        ReadObjectInto(jo, isofile, pattern: "TransferPart");
        if (version >= 2) { jo["xd8"] = isofile.ReadUInt32(); jo["xb4"] = isofile.ReadMfcString(); }
        return jo;
    }

    static JsonObject ReadCActionSubScript(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CAction");
        int version = isofile.ReadSchemaVersion("CActionSubScript", 3);
        if (Unabridged) jo["version"] = version;
        string xb8 = isofile.ReadMfcString();
        jo["xb8"] = xb8;
        if (xb8 == "") ReadObjectInto(jo, isofile, "CActionScript");
        return jo;
    }

    static JsonObject ReadCDelay(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CAction");
        int version = isofile.ReadSchemaVersion("CCounter", 2);
        if (Unabridged) jo["version"] = version;
        jo["counts"] = isofile.ReadInt32();  // CCounter::GetCounts/SetCounts; delay time value for CDelay (DDX_Time)
        return jo;
    }

    static JsonObject ReadCActionInterpreter(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CAction");
        // Serialize = CActionCommand::Serialize (inherited via vftable)
        int version = isofile.ReadSchemaVersion("CActionInterpreter", 1);
        if (Unabridged) jo["version"] = version;
        jo["xb4"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCMethodSwitcher(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CAction");
        int version = isofile.ReadSchemaVersion("CMethodSwitcher", 5);
        if (Unabridged) jo["version"] = version;
        jo["gas_conf_name"] = isofile.ReadMfcString();
        if (version >= 3) { jo["wait_time"] = isofile.ReadUInt32(); jo["method_name"] = isofile.ReadMfcString(); }
        if (version >= 4) jo["script_path"] = isofile.ReadMfcString();
        if (version >= 5) jo["use_hysteresis"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCTimeEventList(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CAction");
        int version = isofile.ReadSchemaVersion("CTimeEventList", 3);
        if (Unabridged) jo["version"] = version;
        int n = isofile.ReadInt32();
        jo["n_actions"] = n;
        for (int i = 0; i < n; i++) ReadObjectInto(jo, isofile, groupTag: 1, groupDeclaredSize: n);
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
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CEvaluationPart", 2);
        if (Unabridged) jo["version"] = version;
        jo["x9c"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCMethodPrintoutDesc(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvaluationPart");
        int version = isofile.ReadSchemaVersion("CMethodPrintoutDesc", 2);
        if (Unabridged) jo["version"] = version;
        jo["xa0"] = isofile.ReadMfcString();
        jo["xa4"] = isofile.ReadMfcString();
        if (version >= 2) { jo["xa8"] = isofile.ReadMfcString(); jo["xac"] = isofile.ReadMfcString(); }
        return jo;
    }

    static JsonObject ReadCComponentListMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvaluationPart");
        ReadObjectInto(jo, isofile, "CComponentList");
        return jo;
    }

    static JsonObject ReadCPartMirror(IsodatFile isofile) => new JsonObject();

    static JsonObject ReadCTimeEventListMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvaluationPart");
        ReadObjectInto(jo, isofile, "CTimeEventList");
        return jo;
    }

    static JsonObject ReadCContiniousFlowStandardizationMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvaluationPart");
        int version = isofile.ReadSchemaVersion("CContiniousFlowStandardizationMethodPart", 1);
        if (Unabridged) jo["version"] = version;
        jo["xa0"] = isofile.ReadUInt32();
        jo["xa8"] = isofile.ReadUInt32();
        jo["xac"] = isofile.ReadUInt32();
        jo["xb0"] = isofile.ReadMfcString();
        long flag = isofile.ReadUInt32();
        if (flag != 0) ReadObjectInto(jo, isofile);
        return jo;
    }

    static JsonObject ReadCContiniousFlowStandardizationListMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvaluationPart");
        int version = isofile.ReadSchemaVersion("CContiniousFlowStandardizationListMethodPart", 9);
        if (Unabridged) jo["version"] = version;
        jo["xac"] = isofile.ReadUInt32();
        jo["xb8"] = isofile.ReadUInt32();
        jo["xb4"] = isofile.ReadUInt32();
        long flag1 = isofile.ReadUInt32();
        if (flag1 != 0) ReadObjectInto(jo, isofile);
        long flag2 = isofile.ReadUInt32();
        if (flag2 != 0) ReadObjectInto(jo, isofile);
        if (version > 2) ReadObjectInto(jo, isofile);
        if (version == 4) { ReadObject(isofile); ReadObject(isofile); } // discard
        if (version > 5) jo["x100"] = isofile.ReadUInt32();
        if (version > 6) jo["x104"] = isofile.ReadUInt32();
        if (version > 7) jo["x108"] = isofile.ReadMfcString();
        if (version > 8) jo["x10c"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCPrimaryStandardMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvaluationPart");
        int version = isofile.ReadSchemaVersion("CPrimaryStandardMethodPart", 2);
        if (Unabridged) jo["version"] = version;
        jo["xa0"] = isofile.ReadMfcString();
        if (version == 1) isofile.ReadUInt32(); // element_num, discard
        ReadObjectInto(jo, isofile);
        if (version > 1) jo["xb0"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCSecondaryStandardMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvaluationPart");
        int version = isofile.ReadSchemaVersion("CSecondaryStandardMethodPart", 3);
        if (Unabridged) jo["version"] = version;
        jo["xa0"] = isofile.ReadMfcString();
        jo["xa4"] = isofile.ReadMfcString();
        jo["xb0"] = isofile.ReadUInt32();
        ReadObjectInto(jo, isofile);
        if (version > 1) jo["xa8"] = isofile.ReadUInt32();
        if (version > 2)
        {
            long flag = isofile.ReadUInt32();
            if (flag != 0) ReadObjectInto(jo, isofile);
        }
        return jo;
    }

    static JsonObject ReadCConFloMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvaluationPart");

        int v = isofile.ReadSchemaVersion("CConFloMethodPart", 11);
        if (Unabridged) jo["version"] = v;

        // v == 1: legacy dilution_type integer (replaced by sample_ident in v2)
        if (v == 1)
            jo["dilution_type"] = isofile.ReadInt32();

        // Always present: catch window and weight percentage
        jo["catch_time_start"] = isofile.ReadDouble(); // "Significant Peak Start [s]"
        jo["catch_time_end"] = isofile.ReadDouble(); // "Significant Peak End [s]"
        jo["weight_pct"] = isofile.ReadDouble(); // "Amount Percent [%]" (index 0)
        jo["xa0"] = isofile.ReadDouble(); // internal factor, default 1.0

        // v == 1: legacy start-only flag (replaced by sample_ident in v2)
        if (v == 1)
            jo["start_only"] = isofile.ReadInt32();

        if (v > 1)
        {
            jo["sample_ident"] = isofile.ReadMfcString(); // "Sample type" — e.g. "Sample", "Blank Mean"
            jo["sample_type"] = isofile.ReadInt32();     // SMP_TYPES enum (computed from sample_ident on load)
        }

        if (v > 3)
            jo["unit"] = isofile.ReadMfcString(); // "Unit" — e.g. "mg"

        if (v > 5)
            isofile.ReadInt32(); // deprecated field, always 0

        if (v >= 7)
        {
            int nCatchTimes = isofile.ReadInt32();
            if (nCatchTimes > 0)
            {
                var arr = new JsonArray();
                for (int i = 0; i < nCatchTimes; i++)
                    arr.Add(new JsonObject { ["start"] = isofile.ReadDouble(), ["end"] = isofile.ReadDouble() });
                jo["catch_times"] = arr;
            }
            int nGasNames = isofile.ReadInt32();
            if (nGasNames > 0)
            {
                var arr = new JsonArray();
                for (int i = 0; i < nGasNames; i++)
                    arr.Add(isofile.ReadMfcString());
                jo["detector_gas_names"] = arr;
            }
        }

        if (v >= 8)
            jo["weight_pct_1"] = isofile.ReadDouble(); // "Amount Percent [%]" index 1

        if (v >= 9)
            jo["x128"] = isofile.ReadMfcString(); // "EA Auto Peak Identification"

        if (v >= 10)
            jo["weight_pct_2"] = isofile.ReadDouble(); // "Amount Percent [%]" index 2

        if (v >= 11)
            jo["weight_calc_type"] = isofile.ReadInt32();

        return jo;
    }

    static JsonObject ReadCICA_BasicMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvaluationPart");
        int version = isofile.ReadSchemaVersion("CICA_BasicMethodPart", 12);
        if (Unabridged) jo["version"] = version;
        ReadObjectInto(jo, isofile);
        jo["xa4"] = isofile.ReadMfcString();
        if (version > 1) ReadObjectInto(jo, isofile);
        if (version > 2) jo["xd8"] = isofile.ReadUInt32();
        if (version == 4)
        {
            ReadObject(isofile); // pre-v5 layout remnants, discarded
            ReadObject(isofile);
        }
        if (version > 5)
        {
            // xd0 is a CBlockData container whose children are serialized inline by CBlockData::Serialize.
            // Unlike the split pattern used in subclasses, here we must read children explicitly.
            var xd0 = ReadObject(isofile);
            jo["xd0"] = xd0;
            for (int i = 0; i < NBlockObjects(xd0); i++)
                ReadObjectInto(xd0["objects"]!.AsObject(), isofile);
        }
        if (version > 6) jo["xdc"] = isofile.ReadMfcString();
        if (version > 7) jo["xe0"] = isofile.ReadMfcString();
        if (version > 8) jo["xe8"] = isofile.ReadUInt32();
        if (version > 9) ReadObjectInto(jo, isofile, "CParsedEvaluationStringArray");
        if (version > 10) jo["xe4"] = isofile.ReadMfcString();
        if (version > 11) jo["xec"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCPeakFindMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CMethodPart");
        int v = isofile.ReadSchemaVersion("CPeakFindMethodPart", 18);
        if (Unabridged) jo["version"] = v;
        jo["current_set_index"] = isofile.ReadUInt32();         // xc4; GetCurrentSetIndex
        ReadObjectInto(jo, isofile, expected: "CBlockData");                            // xc0: parameter sets (CBlockData)
        ReadObjectInto(jo, isofile, expected: "CGasConfiguration", maybeNull: true);           // xd0: gas configuration (nullable)
        jo["xa8"] = isofile.ReadUInt32();
        if (v == 3) isofile.ReadUInt32();                       // v3-only legacy field, discarded on load
        jo["integration_time"] = isofile.ReadDouble();          // xc8; GetIntegrationTime
        if (v >= 4)
        {
            ReadObjectInto(jo, isofile, expected: "CH3FactorResult", maybeNull: true);       // xb0: h3 factor result (nullable)
            jo["linearity_value"] = isofile.ReadDouble();       // xa0; GetLinearityValue
        }
        if (v >= 5)
        {
            jo["xe4"] = isofile.ReadUInt32();
            jo["xfc"] = isofile.ReadUInt32();
        }
        if (v >= 6) ReadObjectInto(jo, isofile, maybeNull: true); // xe8 (nullable), deprecated object, not used anymore
        if (v >= 7) jo["xf0"] = isofile.ReadUInt32(); else jo["xf0"] = 1;
        if (v >= 8) jo["xf4"] = isofile.ReadUInt32(); else jo["xf4"] = 1;
        if (v >= 9) jo["enable_H3_correction"] = isofile.ReadUInt32(); else jo["enable_H3_correction"] = 1;  // xf8; EnableH3Correction
        if (v >= 10) jo["x104"] = isofile.ReadUInt32(); else jo["x104"] = 0;
        if (v >= 11) jo["gas_name"] = isofile.ReadMfcString();  // x108
        if (v >= 12) jo["x100"] = isofile.ReadUInt32(); else jo["x100"] = 0;
        if (v >= 13) jo["x10c"] = isofile.ReadMfcString();
        if (v >= 14) jo["component_list_offset"] = isofile.ReadDouble(); // x110; GetComponentListOffset
        if (v >= 15) jo["de_spike_flag"] = isofile.ReadUInt32();          // x118; GetDeSpikeFlage
        if (v >= 16) jo["x11c"] = isofile.ReadUInt32();
        if (v >= 17)
        {
            jo["x120"] = isofile.ReadDouble();
            jo["xac"] = isofile.ReadUInt32();
        }
        if (v >= 18) jo["xb8"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCSimplePeakFindMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CPeakFindMethodPart");
        int version = isofile.ReadSchemaVersion("CSimplePeakFindMethodPart", 1);
        if (Unabridged) jo["version"] = version;
        jo["x128"] = isofile.ReadMfcString();
        return jo;
    }

    static JsonObject ReadCSimplePeakFindParameter(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CPeakFindParameter");
        int version = isofile.ReadSchemaVersion("CSimplePeakFindParameter", 1);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    // =======================================================================
    // CDeviceMethodPart chain
    // =======================================================================

    static JsonObject ReadCDeviceMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvaluationPart");
        int version = isofile.ReadSchemaVersion("CDeviceMethodPart", 2);
        if (Unabridged) jo["version"] = version;
        if (version >= 2)
        {
            var block = ReadObject(isofile, "CBlockData");
            jo["CBlockData"] = block;
            for (int i = 0; i < NBlockObjects(block); i++)
                ReadObjectInto(block["objects"]!.AsObject(), isofile);
        }
        else
        {
            int n = isofile.ReadInt32();
            for (int i = 0; i < n; i++) ReadObjectInto(jo, isofile, groupTag: 1, groupDeclaredSize: n);
        }
        return jo;
    }

    static JsonObject ReadCConFloDeviceMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CDeviceMethodPart");
        int version = isofile.ReadSchemaVersion("CConFloDeviceMethodPart", 1);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    static JsonObject ReadCMsDeviceMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CDeviceMethodPart");
        int version = isofile.ReadSchemaVersion("CMsDeviceMethodPart", 3);
        if (Unabridged) jo["version"] = version;
        jo["xb0"] = isofile.ReadUInt32();
        jo["xac"] = isofile.ReadUInt8();
        ReadObjectInto(jo, isofile, "CActionPeakCenter");
        if (version >= 2) jo["xb8"] = isofile.ReadUInt32();
        if (version >= 3) jo["xbc"] = isofile.ReadUInt8();
        return jo;
    }

    static JsonObject ReadCStandardDeviceMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CDeviceMethodPart");
        int version = isofile.ReadSchemaVersion("CStandardDeviceMethodPart", 1);
        if (Unabridged) jo["version"] = version;
        jo["xac"] = isofile.ReadMfcString();
        jo["xb0"] = isofile.ReadMfcString();
        jo["xb4"] = isofile.ReadMfcString();
        jo["xb8"] = isofile.ReadUInt32();
        return jo;
    }

    static JsonObject ReadCGenericGcDeviceMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CDeviceMethodPart");
        int version = isofile.ReadSchemaVersion("CGenericGcDeviceMethodPart", 1);
        if (Unabridged) jo["version"] = version;
        jo["xb0"] = isofile.ReadUInt32();
        return jo;
    }

    // CGCExtendedInterfaceDeviceMethodPart::Serialize (DevicesDll.dll):
    //   parent CDeviceMethodPart + schema version uint32 (current = 2, no version-gated fields)
    static JsonObject ReadCGCExtendedInterfaceDeviceMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CDeviceMethodPart");
        int version = isofile.ReadSchemaVersion("CGCExtendedInterfaceDeviceMethodPart", 2);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    static JsonObject ReadCFlashEA_DeviceMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CGenericGcDeviceMethodPart");
        int version = isofile.ReadSchemaVersion("CFlashEA_DeviceMethodPart", 2);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    static JsonObject ReadCMultiReferenceDeviceMethodPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CDeviceMethodPart");
        int version = isofile.ReadSchemaVersion("CMultiReferenceDeviceMethodPart", 7);
        if (Unabridged) jo["version"] = version;
        jo["xac"] = isofile.ReadMfcString();
        jo["xb0"] = isofile.ReadDouble();
        jo["xb8"] = isofile.ReadUInt32();
        if (version == 3)
        {
            isofile.ReadDouble(); // pre-v3 layout remnant, discarded
            jo["xc8"] = isofile.ReadDouble();
            jo["xf8"] = isofile.ReadMfcString();
            jo["xfc"] = isofile.ReadMfcString();
        }
        else
        {
            jo["xc8"] = isofile.ReadDouble();
        }
        if (version >= 5)
        {
            jo["xd0"] = isofile.ReadDouble();
            jo["xe0"] = isofile.ReadDouble();
            jo["xe8"] = isofile.ReadDouble();
            jo["x100"] = isofile.ReadUInt32();
        }
        if (version >= 6) jo["x104"] = isofile.ReadUInt32();
        if (version >= 7)
        {
            jo["xd8"] = isofile.ReadDouble();
            jo["xf0"] = isofile.ReadDouble();
        }
        return jo;
    }

    // =======================================================================
    // CDeviceEvaluationPart chain
    // =======================================================================

    static JsonObject ReadCDeviceEvaluationPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvaluationPart");
        int version = isofile.ReadSchemaVersion("CDeviceEvaluationPart", 2);
        if (Unabridged) jo["version"] = version;
        if (version >= 2)
        {
            var block = ReadObject(isofile, "CBlockData");
            jo["CBlockData"] = block;
            for (int i = 0; i < NBlockObjects(block); i++)
                ReadObjectInto(block["objects"]!.AsObject(), isofile);
        }
        else
        {
            int n = isofile.ReadInt32();
            for (int i = 0; i < n; i++) ReadObjectInto(jo, isofile, groupTag: 1, groupDeclaredSize: n);
        }
        return jo;
    }

    static JsonObject ReadCConFloDeviceEvaluationPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CDeviceEvaluationPart");
        int version = isofile.ReadSchemaVersion("CConFloDeviceEvaluationPart", 1);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    static JsonObject ReadCMsDeviceEvaluationPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CDeviceEvaluationPart");
        int version = isofile.ReadSchemaVersion("CMsDeviceEvaluationPart", 2);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    static JsonObject ReadCFlashEA_DeviceEvaluationPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CGenericGcDeviceEvaluationPart");
        int version = isofile.ReadSchemaVersion("CFlashEA_DeviceEvaluationPart", 1);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    // =======================================================================
    // CEvalDataTransferPart chain
    // =======================================================================

    static JsonObject ReadCEvalDataTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvalDataItemTransferPart");
        int version = isofile.ReadSchemaVersion("CEvalDataTransferPart", 2);
        if (Unabridged) jo["version"] = version;
        if (version >= 1)
        {
            long n = isofile.ReadUInt32();  // byte count
            if (Unabridged) jo["n_bytes"] = n;
            if (n > 0)
            {
                byte[] raw = isofile.ReadBytes((int)n);
                if (Unabridged) jo["raw_data"] = Convert.ToBase64String(raw);
                if (n == 4)
                    jo["data"] = BitConverter.ToInt32(raw, 0);
                else if (n == 8)
                    jo["data"] = BitConverter.ToDouble(raw, 0);
            }
        }
        if (version >= 2) ReadObjectInto(jo, isofile, "CBlockData", maybeNull: true);
        return jo;
    }

    // CEvalDataDWORDTransferPart, CEvalDataDoubleTransferPart, CEvalDataIntTransferPart, and
    // CEvalDataSecStdTransferPart all share the same stream layout (CEvalDataTransferPart parent
    // + DWORD schema version). The data interpretation is handled by ReadCEvalDataTransferPart
    // based on n_bytes (4 → int32, 8 → double).
    static JsonObject ReadCEvalDataDWORDTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvalDataTransferPart");
        int version = isofile.ReadSchemaVersion("CEvalDataDWORDTransferPart", 1);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    static JsonObject ReadCEvalDataDoubleTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvalDataTransferPart");
        int version = isofile.ReadSchemaVersion("CEvalDataDWORDTransferPart", 1);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    static JsonObject ReadCEvalDataIntTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvalDataTransferPart");
        int version = isofile.ReadSchemaVersion("CEvalDataDWORDTransferPart", 1);
        if (Unabridged) jo["version"] = version;
        return jo;
    }

    static JsonObject ReadCEvalDataSecStdTransferPart(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvalDataTransferPart");
        isofile.ReadSchemaVersion("CEvalDataDWORDTransferPart", 1);  // parent schema version
        int version = isofile.ReadSchemaVersion("CEvalDataSecStdTransferPart", 2);
        if (Unabridged) jo["version"] = version;
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
        TrackPartial(jo);
        ReadParent(jo, isofile, "CEvalDataTransferPart");
        int version = isofile.ReadSchemaVersion("CEvalDataStringTransferPart", 1);
        if (Unabridged) jo["version"] = version;
        long n = isofile.ReadUInt32();
        jo["data_string"] = n > 0
            ? Encoding.Latin1.GetString(isofile.ReadBytes((int)n))
            : "";
        return jo;
    }

    // =======================================================================
    // Peak stubs
    // =======================================================================

    // CResultData::Serialize (IsoPeakData.dll):
    //   parent CBlockData + schema v6
    //   0xa8 uint32 peak_id
    //   0xac MFC string gas_name
    //   0xbc WriteObject (CData-derived results object)
    //   v>=2: 0xc4 uint32 eval_group (else computed via GetEvalGroup)
    //   v>2:  0xb0 MFC string gas_name_list, 0xb4 MFC string converted_gas_name
    //   v>=4: 0xb8 MFC string eval_name (else copies gas_name)
    //   v>4:  0xc0 MFC string
    //   v>5:  0xcc uint32
    static JsonObject ReadCResultData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        var block = ReadParent(jo, isofile, "CBlockData");
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(block["objects"]!.AsObject(), isofile, expected: "CGCPeak");
        int v = isofile.ReadSchemaVersion("CResultData", 6);
        if (Unabridged) jo["version"] = v;
        jo["peak_id"] = isofile.ReadUInt32();
        jo["gas_name"] = isofile.ReadMfcString();
        ReadObjectInto(jo, isofile);                      // 0xbc: embedded results WriteObject
        if (v >= 2)
            jo["eval_group"] = isofile.ReadUInt32();
        if (v > 2)
        {
            jo["gas_name_list"] = isofile.ReadMfcString();
            jo["converted_gas_name"] = isofile.ReadMfcString();
        }
        if (v >= 4)
            jo["eval_name"] = isofile.ReadMfcString();
        if (v > 4)
            jo["xc0"] = isofile.ReadMfcString();
        if (v > 5)
            jo["xcc"] = isofile.ReadUInt32();
        return jo;
    }

    // CGCBGDData::Serialize (IsoPeakData.dll):
    //   NO parent Serialize call (skips CData::Serialize entirely)
    //   raw version uint32 (writes 2 currently)
    //   SLOPE_BUFFER at 0xa8 (serialized subset): bgd0 (double), bgd1 (double), xc0 (uint32)
    //   slope double (0x98), intercept double (0xa0)
    //   trace_idx (0xcc), bgd_method (0xc8)
    //   v>1: mass (0xd0)
    static JsonObject ReadCGCBGDData(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        int v = isofile.ReadSchemaVersion("CGCBGDData", 2);
        if (Unabridged) jo["version"] = v;
        jo["bgd0"] = isofile.ReadDouble();
        jo["bgd1"] = isofile.ReadDouble();
        jo["xc0"] = isofile.ReadUInt32();
        jo["slope"] = isofile.ReadDouble();
        jo["intercept"] = isofile.ReadDouble();
        jo["trace_idx"] = isofile.ReadUInt32();
        long bgdMethodId = isofile.ReadUInt32();
        if (Unabridged) jo["bgd_method_id"] = bgdMethodId;
        jo["bgd_method"] = bgdMethodId switch
        {
            0 => "Uninitialized",
            1 => "Single BGD",
            2 => "Individual BGD",
            3 => "TimeBased BGD",
            4 => "Dynamic BGD",
            5 => "Mean",
            6 => "Slope",
            8 => "Dynamic Invalid",
            9 => "Timebased Invalid",
            15 => "BaseFit BGD",
            16 => "Skimmed BGD",
            17 => "Individual RDA BGD",
            _ => $"Unknown ({bgdMethodId})",
        };
        if (v > 1)
            jo["mass"] = isofile.ReadUInt32();
        return jo;
    }

    // CGCPeak::Serialize (IsoPeakData.dll):
    //   parent CGCBGDData + raw version uint32 (writes 3 currently)
    //   peak_number (0xd8)
    //   per SLOPE_BUFFER pattern (count uint32 + 2 doubles):
    //     start_val: xf8 (0xf8), start_val_a (0xe0), start_val_b (0xe8)
    //     top_val:   x118 (0x118), top_val_a (0x100), top_val_b (0x108)
    //     end_val:   x138 (0x138), end_val_a (0x120), end_val_b (0x128)
    //   raw_area (0x140), area (0x148)
    //   valid (0x150), square_peak (0x154), as_standard (0x158)
    //   v>1:  time_shift double (0x160)
    //   v>=3: ampere_calculation (0x16c)
    static JsonObject ReadCGCPeak(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CGCBGDData");
        int v = isofile.ReadSchemaVersion("CGCPeak", 3);
        if (Unabridged) jo["version"] = v;
        jo["peak_number"] = isofile.ReadUInt32();
        jo["start_idx"] = isofile.ReadUInt32();
        jo["start_rt"] = isofile.ReadDouble();
        jo["start_signal"] = isofile.ReadDouble();
        jo["apex_idx"] = isofile.ReadUInt32();
        jo["apex_rt"] = isofile.ReadDouble();
        jo["apex_signal"] = isofile.ReadDouble();
        jo["end_idx"] = isofile.ReadUInt32();
        jo["end_rt"] = isofile.ReadDouble();
        jo["end_signal"] = isofile.ReadDouble();
        jo["raw_area"] = isofile.ReadDouble();
        jo["area"] = isofile.ReadDouble();
        jo["valid"] = isofile.ReadUInt32();
        jo["square_peak"] = isofile.ReadUInt32();
        jo["as_standard"] = isofile.ReadUInt32();
        if (v > 1)
            jo["time_shift"] = isofile.ReadDouble();
        if (v >= 3)
            jo["ampere_calculation"] = isofile.ReadUInt32();
        return jo;
    }

    // CSPeak::Serialize (IsoPeakData.dll):
    //   parent CResultData + schema v3
    //   0xd8 uint32 bgd_method (BGD_METHOD enum)
    //   0xd4 uint32 n_traces
    //   v==2 only: 0xcc uint32 ampere_calculation (deprecated in v3, field absorbed by CResultData v6)
    static JsonObject ReadCSPeak(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadParent(jo, isofile, "CResultData");
        int v = isofile.ReadSchemaVersion("CSPeak", 3);
        if (Unabridged) jo["version"] = v;
        jo["bgd_method"] = isofile.ReadUInt32();
        jo["n_traces"] = isofile.ReadUInt32();
        if (v == 2)
            jo["ampere_calculation"] = isofile.ReadUInt32();
        return jo;
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
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CScrBase", 2);
        if (Unabridged) jo["version"] = version;
        jo["x9c"] = isofile.ReadMfcString();   // headline / description
        jo["xa0"] = isofile.ReadInt32();
        jo["xa4"] = isofile.ReadInt32();
        ReadObjectInto(jo, isofile);          // optional WriteObject (null in observed files)
        ReadObjectInto(jo, isofile);          // optional WriteObject (null in observed files)
        jo["xb0"] = isofile.ReadInt32();
        jo["xb4"] = isofile.ReadInt32();
    }

    static JsonObject ReadCScrHeadLine(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadScrBase(isofile, jo);
        ReadObjectInto(jo, isofile, "CDynExternal");
        return jo;
    }

    static JsonObject ReadCScrNumber(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        ReadScrBase(isofile, jo);
        ReadObjectInto(jo, isofile, "CNumericValue");
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
        TrackPartial(jo);
        ReadParent(jo, isofile, "CData");
        int version = isofile.ReadSchemaVersion("CDynExternal", 4);
        if (Unabridged) jo["version"] = version;
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
        jo["xdesc"] = desc;
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
        TrackPartial(jo);
        jo[ValueKey] = isofile.ReadDouble();       // IEEE 754 double
        jo["x08"] = isofile.ReadInt32();        // some flag/type
        ReadObjectInto(jo, isofile);      // descriptor CData subclass
        return jo;
    }

    // =======================================================================
    // CShrinkInfo (not CData/CBlockData derived)
    // =======================================================================

    static JsonObject ReadCShrinkInfo(IsodatFile isofile)
    {
        var jo = new JsonObject();
        TrackPartial(jo);
        int version = isofile.ReadSchemaVersion("CShrinkInfo", 2);
        if (Unabridged) jo["version"] = version;
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
        var block = ReadParent(jo, isofile, "CBlockData");
        var cfBlockObjects = block["objects"]!.AsObject();
        for (int i = 0; i < NBlockObjects(block); i++)
            ReadObjectInto(cfBlockObjects, isofile);
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
