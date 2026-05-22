# Inheritance Hierarchy

## CData (DONE)
- CApplicationData (DONE)
- CAxisPara (DONE)
- CBasicScan (DONE)
- CCalibrationParameter (DONE)
- CCalibrationPoint (DONE)
- CComponent (DONE)
- CDynExternal (DONE)
- CEvalDataItemTransferPart (DONE)
  - CPeakDataItem (DONE)
  - CEvalDataTransferPart (DONE)
    - CEvalDataDWORDTransferPart (DONE)
      - CEvalDataSecStdTransferPart (DONE)
      - CEvalDataDoubleTransferPart (DONE)
      - CEvalDataIntTransferPart (DONE)
    - CEvalDataStringTransferPart (DONE)
- CEvalIntegrationUnitHWInfo (DONE)
- CH3FactorResult (DONE)
- CGridColors (DONE)
- CISLScriptMessageData (DONE)
- CMolecule (DONE)
- CMRI_DilutionList (DONE)
- COutlierData
- CPeakDetectionParameter (DONE)
- CPeakFindParameter (DONE)
  - CSimplePeakFindParameter (DONE)
- CResultDataSimple
- CResultForGas (DONE)
- CScrBase
  - CScrHeadLine (DONE)
  - CScrNumber (DONE)
- CTimeObject (DONE)
- CTraceLinCol (DONE)
- CTraceSettings (DONE)
- CWinColor (DONE)
- IsoGCEvalData (DONE)
  - CGCData (DONE)
    - CRawData (DONE)

## CData::CAction (DONE)
- CActionBackground
- CActionCommand (DONE)
  - CActionInterpreter (DONE)
- CActionHwTransferContainer (DONE)
- CActionPeakCenter (DONE)
- CActionPressAdjust
- CActionSubScript (DONE)
- CCounter (DONE)
  - CDelay (DONE)
- CMethodSwitcher (DONE)
- CTimeEventList (DONE)

## CData::CBlockData (DONE)
- CActionScript (DONE)
- CAcquistionBaseBlockData (DONE)
  - CDualInletBlockData
  - CContiniousFlowBlockData (DONE)
- CAllMoleculeWeights (DONE)
- CBlockDataContext
- CCalibration (DONE)
- CComponentList (DONE)
- CConfiguration (DONE)
- CDataIndex (DONE)
- CDualInletEvaluatedData
- CDualInletEvaluatedDataCollect
- CDualInletRawData
- CDualInletShout
- CEvalDataItemListTransferPart (DONE)
  - CEvalIntegrationUnitHWInfoList (DONE)
  - CEvalIntegrationUnitHWInfoStore (DONE)
- CFileHeader (DONE)
- CGasConfiguration (DONE)
- CGasSettings (DONE)
- CGCPeakList (DONE)
- CMeasurmentErrors (DONE)
- CMeasurmentInfos (DONE)
- CMethod (DONE)
- CParsedEvaluationStringArray (DONE)
- CPeakList
- CPkDataItemList (DONE)
- CPlotSettings (DONE)
- CResultArray (DONE)
- CResultData (DONE)
- CResultDataList
- CScanStorage (DONE)
- CSequenceGridParam
  - CSequenceFlag
  - CSequenceText
- CViewColors (DONE)
- CVisualisationData (DONE)
- CVisualisationDialogNamesBlockData (DONE)
- CWinSettings (DONE)

## CData::CBlockData::CDevice (DONE)
- CActiveDevice (DONE)
  - CBufferedRefillDevice (DONE)
    - CGCExtendedInterfaceDevice (DONE)
    - CMultiReferenceDevice (DONE)
    - CUserDevice (DONE)
  - CConFloDevice (DONE)
  - CGenericGcDevice (DONE)
    - CFlashEA_Device (DONE)
    - CTraceGcDevice
  - CXCaliburDevice (DONE)
    - CTraceBasicDevice (DONE)
      - CTrace_II_Device (DONE)
    - CXcalRSH2Device (DONE)
    - CXcalRSHDevice (DONE)
- CCarbonateDevice
- CMsDevice (DONE)
- CSamplerDevice
  - CA200SDevice

## CData::CBlockData::CPort (DONE)
- CActivePort (DONE)

## CData::CBasicInterface (DONE)
- CFinniganInterface (DONE)
- CGpibInterface (DONE)
- CTransferPart (DONE)
  - CAdcTransferPart (DONE)
    - CDioTransferPart (DONE)
      - CValveTransferPart (DONE)
  - CDacTransferPart (DONE)
    - CBasicHvTransferPart (DONE)
      - CCalculatingDacTransferPart (DONE)
      - CScaleHvTransferPart (DONE)
    - CMagnetCurrentTransferPart (DONE)
  - CIntegrationUnitTransferPart
  - CIntensityData
- CGasConfPart (DONE)
  - CChannelGasConfPart (DONE)
  - CIntegrationUnitGasConfPart (DONE)
- CScanPart (DONE)
  - CClockScanPart (DONE)
  - CIntegrationUnitScanPart (DONE)
  - CMagnetCurrentScanPart (DONE)
  - CScaleHvScanPart (DONE)
- CHardwarePart (DONE)
  - CAdcHardwarePart
    - CCalculatingAdcHardwarePart
    - CHVStatusHardwarePart
    - CPressureMeterHardwarePart
  - CChannelHardwarePart (DONE)
  - CCupHardwarePart (DONE)
  - CDioHardwarePart
  - CIdReaderHardwarePart
  - CScaleHardwarePart (DONE)
    - CAdcHardwarePart (see above)
    - CClockHardwarePart (DONE)
    - CDacHardwarePart (DONE)
      - CCalculatingDacHardwarePart
      - CMagnetCurrentHardwarePart (DONE)
      - CScaleHvHardwarePart (DONE)
    - CIntegrationUnitHardwarePart (DONE)
- CSequencePart
  - CDeviceSequencePart
    - CDualInletSequencePart
- CEvaluationPart (DONE)
  - CDeviceMethodPart (DONE)
    - CActiveDeviceMethodPart (DONE)
    - CConFloDeviceMethodPart (DONE)
    - CDualInletDeviceMethodPart
    - CGenericGcDeviceMethodPart (DONE)
      - CFlashEA_DeviceMethodPart (DONE)
      - CTraceGcDeviceMethodPart
    - CMsDeviceMethodPart (DONE)
    - CMultiReferenceDeviceMethodPart (DONE)
    - CStandardDeviceMethodPart (DONE)
- CDeviceEvaluationPart (DONE)
  - CCarbonateDeviceEvaluationPart
  - CConFloDeviceEvaluationPart (DONE)
    - CGenericGcDeviceEvaluationPart (DONE)
      - CFlashEA_DeviceEvaluationPart (DONE)
      - CTraceGcDeviceEvaluationPart
    - CMultiReferenceDeviceEvaluationPart (DONE)
  - CMsDeviceEvaluationPart (DONE)

## CMethodPart / CEvaluationPart (DONE)
- CICA_BasicMethodPart (DONE)
- CComponentListMethodPart (DONE)
- CConFloMethodPart (DONE)
- CContiniousFlowStandardizationListMethodPart (DONE)
- CContiniousFlowStandardizationMethodPart (DONE)
- CDualInletStandardizationMethodPart
- CExtEvaluation
- CMethodPrintoutDesc (DONE)
- COutlierTestMethodPart
- CPeakFindMethodPart (DONE)
  - CSimplePeakFindMethodPart (DONE)
- CPrimaryStandardMethodPart (DONE)
- CSecondaryStandardMethodPart (DONE)
- CTimeEventListMethodPart (DONE)

## CSimple (DONE)
- CBinary (DONE)
- CColorType
  - CDword (DONE)
    - CPeakCenterOffset (DONE)
  - CInt (DONE)
- CDouble (DONE)
- CStr (DONE)

## CEvalDataStorage (DONE)
- CEvalFakeData (DONE)
  - CEvalGCData (DONE)

## CGridStorage
- CErrorGridStorage

## CObject
- CPlotInfo (DONE)
- CPlotRange (DONE)
- CTraceInfo (DONE)
  - CTraceInfoEntry (DONE)

## CStringArray (DONE)

## CParsedEvaluationString (DONE)

## CNumericValue (DONE)

## CShrinkInfo (DONE)

## CPartMirror (DONE)
pure virtual no-op Serialize; concrete subclasses used only as runtime mirrors, not serialized

## CGCBGDData (DONE)
CData-derived; no parent Serialize call

## CGCPeak (DONE)
parent class CGCBGDData

## CSPeak (DONE)
parent class CResultData
