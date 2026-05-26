# Inheritance Hierarchy

## CData
- CApplicationData
- CAxisPara
- CBasicScan
- CCalibrationParameter
- CCalibrationPoint
- CComponent
- CDeviceInterface
- CDynExternal
- CEvalDataItemTransferPart
  - CPeakDataItem
  - CEvalDataTransferPart
    - CEvalDataDWORDTransferPart
      - CEvalDataSecStdTransferPart
      - CEvalDataDoubleTransferPart
      - CEvalDataIntTransferPart
    - CEvalDataStringTransferPart
- CEvalIntegrationUnitHWInfo
- CH3FactorResult
- CGridColors
- CISLScriptMessageData
- CMolecule
- CMRI_DilutionList
- COutlierData
- COutlierTest
- COutlierTestSigma
- CPeakDetectionParameter
- CPeakFindParameter
  - CSimplePeakFindParameter
- CResultDataSimple
- CResultForGas
- CScrBase
  - CScrChannel
  - CScrHeadLine
  - CScrNumber
- CTimeObject
- CTraceLinCol
- CTraceSettings
- CTwoDoublesArrayData
- CWinColor
- IsoGCEvalData
  - CGCData
    - CRawData

## CData::CAction
- CActionBackground
- CActionCommand
  - CActionInterpreter
- CActionHwTransferContainer
- CActionPeakCenter
- CActionPressAdjust
- CActionSubScript
- CCounter
  - CDelay
- CMethodSwitcher
- CTimeEventList

## CData::CBlockData
- CActionScript
- CAcquistionBaseBlockData
  - CDualInletBlockData
  - CContiniousFlowBlockData
- CAllMoleculeWeights
- CBlockDataContext
- CCalibration
- CComponentList
- CConfiguration
- CDataIndex
- CDualInletEvaluatedData
- CDualInletEvaluatedDataCollect
- CDualInletRawData
- CDualInletShout
- CEvalDataItemListTransferPart
  - CEvalIntegrationUnitHWInfoList
  - CEvalIntegrationUnitHWInfoStore
- CFileHeader
- CGasConfiguration
- CGasSettings
- CGCPeakList
- CMeasurmentErrors
- CMeasurmentInfos
- CMethod
- CParsedEvaluationStringArray
- CPeakList
- CPkDataItemList
- CPlotSettings
- CResultArray
- CResultData
- CResultDataList
- CResultDataSimpleList
- CScanStorage
- CSeqLineIndexData
- CSequenceGridParam
  - CSequenceCmd
  - CSequenceFlag
  - CSequenceText
  - CSequenceTextFiles
  - CSequenceTextSamplerMethod
- CViewColors
- CVisualisationData
- CVisualisationDialogNamesBlockData
- CWinSettings

## CData::CBlockData::CDevice
- CActiveDevice
  - CBufferedRefillDevice
    - CCarbonateDevice
    - CGCExtendedInterfaceDevice
    - CMultiReferenceDevice
    - CReferenceRefillDevice
    - CUserDevice
  - CChangeOver2Device
  - CConFloDevice
  - CDualInletDevice
  - CGenericGcDevice
    - CElementalAnalyzerDevice
      - CGCBoxDevice (same as CElementalAnalyzerDevice)
      - CHeMDevice (same as CElementalAnalyzerDevice)
    - CFlashEA_Device
      - CFlashEaIsoLink_Device (same as CFlashEA_Device)
      - CFlashHT_Device (same as CFlashEA_Device)
    - CTraceGcDevice
  - CXCaliburDevice
    - CAS3000Device
    - CTraceBasicDevice
      - CTrace_II_Device
    - CXcalRSH2Device
    - CXcalRSHDevice
- CMsDevice
- CSamplerDevice
  - CA200SDevice

## CData::CBlockData::CPort
- CActivePort

## CData::CBasicInterface
- CFinniganInterface
- CGpibInterface
- CTransferPart
  - CAdcTransferPart
    - CDioTransferPart
      - CValveTransferPart
        - CSplitTransferPart
        - CSwitchTransferPart (same as CAdcTransferPart)
  - CDacTransferPart
    - CBasicHvTransferPart
      - CCalculatingDacTransferPart
      - CScaleHvTransferPart
    - CMagnetCurrentTransferPart
  - CIntegrationUnitTransferPart
  - CIntensityData
- CGasConfPart
  - CChannelGasConfPart
  - CIntegrationUnitGasConfPart
- CScanPart
  - CCalculatingDacScanPart
  - CClockScanPart
  - CIntegrationUnitScanPart
  - CMagnetCurrentScanPart
  - CScaleHvScanPart
- CHardwarePart
  - CAdcHardwarePart
    - CCalculatingAdcHardwarePart
    - CHVStatusHardwarePart
    - CPressureMeterHardwarePart
  - CBoardIdHardwarePart
  - CChannelHardwarePart
  - CCupHardwarePart
  - CDioHardwarePart
    - CSwitchHardwarePart
    - CValveHardwarePart
  - CIdReaderHardwarePart
  - CScaleHardwarePart
    - CAdcHardwarePart (see above)
    - CClockHardwarePart
    - CDacHardwarePart
      - CCalculatingDacHardwarePart
      - CMagnetCurrentHardwarePart
      - CScaleHvHardwarePart
    - CIntegrationUnitHardwarePart
- CSequencePart
  - CDeviceSequencePart
    - CCarbonateSequencePart
    - CBufferedRefillSequencePart
      - CConFloSequencePart (same as CDeviceSequencePart)
    - CGenericGcSequencePart
    - CReferenceRefillSequencePart
    - CSamplerSequencePart
    - CDualInletSequencePart
  - CMsSequencePart
- CEvaluationPart
  - CDeviceMethodPart
    - CActiveDeviceMethodPart (same as CDeviceMethodPart)
      - CHeMDeviceMethodPart
    - CCarbonateDeviceMethodPart
    - CConFloDeviceMethodPart
      - CGCBoxDeviceMethodPart (same as CConFloDeviceMethodPart)
    - CDualInletDeviceMethodPart
    - CGCExtendedInterfaceDeviceMethodPart
    - CGenericGcDeviceMethodPart
      - CElementalAnalyzerDeviceMethodPart
      - CFlashEA_DeviceMethodPart
        - CFlashEaIsoLink_DeviceMethodPart
      - CTraceGcDeviceMethodPart
    - CMsDeviceMethodPart
    - CMultiReferenceDeviceMethodPart
    - CReferenceRefillDeviceMethodPart
    - CStandardDeviceMethodPart
- CDeviceEvaluationPart
  - CGCExtendedInterfaceDeviceEvaluationPart (same as CConFloDeviceEvaluationPart)
  - CConFloDeviceEvaluationPart
    - CCarbonateDeviceEvaluationPart (same as CConFloDeviceEvaluationPart)
    - CDualInletDeviceEvaluationPart (same as CConFloDeviceEvaluationPart)
    - CElementalAnalyzerDeviceEvaluationPart
      - CGCBoxDeviceEvaluationPart (same as CConFloDeviceEvaluationPart)
      - CHeMDeviceEvaluationPart (same as CConFloDeviceEvaluationPart)
    - CGenericGcDeviceEvaluationPart (same as CConFloDeviceEvaluationPart)
      - CFlashEA_DeviceEvaluationPart
      - CTraceGcDeviceEvaluationPart
    - CMultiReferenceDeviceEvaluationPart (same as CConFloDeviceEvaluationPart)
  - CMsDeviceEvaluationPart

## CMethodPart / CEvaluationPart
- CICA_BasicMethodPart
- CComponentListMethodPart
- CConFloMethodPart
- CContiniousFlowStandardizationListMethodPart
- CContiniousFlowStandardizationMethodPart
- CDualInletStandardizationMethodPart
- CExtEvaluation
- CMethodPrintoutDesc
- COutlierTestMethodPart
- CPeakFindMethodPart
  - CSimplePeakFindMethodPart
- CPrimaryStandardMethodPart
- CSecondaryStandardMethodPart
- CTimeEventListMethodPart

## CSimple
- CBinary
- CColorType
  - CDword
    - CPeakCenterOffset
  - CInt
    - CLong
- CDouble
- CStr

## CEvalDataStorage
- CEvalFakeData
  - CEvalGCData

## CGridStorage
- CEvaluatedDataGridStorage (same as CErrorGridStorage)
- CErrorGridStorage
  - CH3FactorGridStorage (same as CErrorGridStorage)
- CExtendedInformationGridStorage (same as CErrorGridStorage)
- CPeakDataGridStorage (same as CErrorGridStorage)
- CRawDataGridStorage (same as CErrorGridStorage)
- CSequenceLineInformationGridStorage (same as CErrorGridStorage)

## CGridCtrl
- CPkDataListBox

## CObject
- CPlotInfo
- CPlotRange
- CTraceInfo
  - CTraceInfoEntry

## CStringArray

## CParsedEvaluationString

## CNumericValue

## CShrinkInfo

## CPartMirror
pure virtual no-op Serialize; concrete subclasses used only as runtime mirrors, not serialized

## CGCBGDData
CData-derived; no parent Serialize call

## CGCPeak
parent class CGCBGDData

## CSPeak
parent class CResultData
