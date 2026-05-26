This is an example of a scan `scn` file structure tree (generated with the `--tree` flag of `isoextract`) from the `scan_HV_ex01.scn` test file.

CScanStorage v6 0x0
  CBinary v2 0x3c
  CBinary v2 0x7b65
    CPlotInfo v2 0x7b77
      CTraceInfo v1 0x7bfa
        1-7/7: CTraceInfoEntry v1 0x7c0f
      CPlotRange v1 0x7dab
  CScaleHvScanPart v2 0x7f4c "Isotope MS/Integration Unit@Isotope MS/ScaleHv@456456"
    CScaleHvHardwarePart v3 0x7fe2 "High Voltage"
      CFinniganInterface v6 0x804e "Delta"
      CVisualisationData v8 0x80b6
  CIntegrationUnitScanPart v3 0x823a
    CIntegrationUnitHardwarePart v3 0x826e "Intensity"
      CFinniganInterface v6 0x82ee "Delta"
      CIntegrationUnitGasConfPart v2 0x8328 "IntegrationUnit"
        1-3/3: CChannelGasConfPart v4 0x83b4
      CCalibration v5 0x847e "JB SP 11.01.02"
        1/3: CCalibrationPoint v3 0x84e2 "0. Point"
        2/3: CCalibrationPoint v3 0x854f "1. Point"
        3/3: CCalibrationPoint v3 0x85a7 "2. Point"
      CVisualisationData v8 0x9983
      1/10: CCupHardwarePart v5 0x9afc "Cup 1"
        CBasicInterface v2 0x9ba4 "Undefined"
        CVisualisationData v8 0x9bf9
      2/10: CCupHardwarePart v5 0x9cd2 "Cup 2"
        CBasicInterface v2 0x9d12 "Undefined"
        CVisualisationData v8 0x9d54
      3/10: CCupHardwarePart v5 0x9e2d "Cup 3"
        CBasicInterface v2 0x9e79 "Undefined"
        CVisualisationData v8 0x9ebb
      4/10: CCupHardwarePart v5 0x9f94 "Cup 4"
        CBasicInterface v2 0x9fe0 "Undefined"
        CVisualisationData v8 0xa022
      5/10: CCupHardwarePart v5 0xa0fb "Cup 5"
        CBasicInterface v2 0xa143 "Undefined"
        CVisualisationData v8 0xa185
      6/10: CCupHardwarePart v5 0xa25e "Cup 6"
        CBasicInterface v2 0xa29a "Undefined"
        CVisualisationData v8 0xa2dc
      7/10: CCupHardwarePart v5 0xa3b5 "Cup 7"
        CBasicInterface v2 0xa3f5 "Undefined"
        CVisualisationData v8 0xa437
      8/10: CCupHardwarePart v5 0xa510 "Cup 8"
        CBasicInterface v2 0xa550 "Undefined"
        CVisualisationData v8 0xa592
      9/10: CCupHardwarePart v5 0xa66b "Cup 9"
        CBasicInterface v2 0xa6af "Undefined"
        CVisualisationData v8 0xa6f1
      10/10: CCupHardwarePart v5 0xa7ca "Cup 10"
        CBasicInterface v2 0xa80e "Undefined"
        CVisualisationData v8 0xa850
      1/10: CChannelHardwarePart v2 0xa92a "Channel 1"
        CBasicInterface v2 0xa97e "Undefined"
        CVisualisationData v8 0xa9c0
      2/10: CChannelHardwarePart v2 0xaa88 "Channel 2"
        CBasicInterface v2 0xaac4 "Undefined"
        CVisualisationData v8 0xab06
      3/10: CChannelHardwarePart v2 0xabce "Channel 3"
        CBasicInterface v2 0xac0a "Undefined"
        CVisualisationData v8 0xac4c
      4/10: CChannelHardwarePart v2 0xad14 "Channel 4"
        CBasicInterface v2 0xad50 "Undefined"
        CVisualisationData v8 0xad92
      5/10: CChannelHardwarePart v2 0xae5a "Channel 5"
        CBasicInterface v2 0xae96 "Undefined"
        CVisualisationData v8 0xaed8
      6/10: CChannelHardwarePart v2 0xafa0 "Channel 6"
        CBasicInterface v2 0xafdc "Undefined"
        CVisualisationData v8 0xb01e
      7/10: CChannelHardwarePart v2 0xb0e6 "Channel 7"
        CBasicInterface v2 0xb122 "Undefined"
        CVisualisationData v8 0xb164
      8/10: CChannelHardwarePart v2 0xb22c "Channel 8"
        CBasicInterface v2 0xb268 "Undefined"
        CVisualisationData v8 0xb2aa
      9/10: CChannelHardwarePart v2 0xb372 "Channel 9"
        CBasicInterface v2 0xb3ae "Undefined"
        CVisualisationData v8 0xb3f0
      10/10: CChannelHardwarePart v2 0xb4b8 "Channel 10"
        CBasicInterface v2 0xb4f8 "Undefined"
        CVisualisationData v8 0xb53a
  CGasConfiguration v3 0xb62b "Clumped CO2"
    1/29: CBasicScan v4 0xb672 "Peak Center"
      CScaleHvScanPart v2 0xb6c4 "Isotope MS/Integration Unit@Isotope MS/ScaleHv@"
        CScaleHvHardwarePart v3 0xb73a "High Voltage"
          CFinniganInterface v6 0xb78e "Delta"
          CVisualisationData v8 0xb7e0
      CIntegrationUnitScanPart v3 0xb94e
        CIntegrationUnitHardwarePart v3 0xb966 "Integration Unit"
          CGpibInterface v3 0xb9d4
          CIntegrationUnitGasConfPart v2 0xba05 "IntegrationUnit"
            1-3/3: CChannelGasConfPart v4 0xba72
          CVisualisationData v8 0xbb35
          1/8: CCupHardwarePart v5 0xbc8a "Cup 1"
            CBasicInterface v2 0xbcb6
            CVisualisationData v8 0xbce6
          2/8: CCupHardwarePart v5 0xbdbf "Cup 2"
            CBasicInterface v2 0xbdeb
            CVisualisationData v8 0xbe1b
          3/8: CCupHardwarePart v5 0xbef4 "Cup 3"
            CBasicInterface v2 0xbf20
            CVisualisationData v8 0xbf50
          4/8: CCupHardwarePart v5 0xc029 "Cup 4"
            CBasicInterface v2 0xc055
            CVisualisationData v8 0xc085
          5/8: CCupHardwarePart v5 0xc15e "Cup 5"
            CBasicInterface v2 0xc18a
            CVisualisationData v8 0xc1ba
          6/8: CCupHardwarePart v5 0xc293 "Cup 6"
            CBasicInterface v2 0xc2bf
            CVisualisationData v8 0xc2ef
          7/8: CCupHardwarePart v5 0xc3c8 "Cup 7"
            CBasicInterface v2 0xc3f4
            CVisualisationData v8 0xc424
          8/8: CCupHardwarePart v5 0xc4fd "Cup 8"
            CBasicInterface v2 0xc529
            CVisualisationData v8 0xc559
          1/3: CChannelHardwarePart v2 0xc633 "Channel 1"
            CBasicInterface v2 0xc66f
            CVisualisationData v8 0xc69f
          2/3: CChannelHardwarePart v2 0xc767 "Channel 2"
            CBasicInterface v2 0xc7a3
            CVisualisationData v8 0xc7d3
          3/3: CChannelHardwarePart v2 0xc89b "Channel 3"
            CBasicInterface v2 0xc8d7
            CVisualisationData v8 0xc907
      CBlockData v2 0xc9ec
    2/29: CIntegrationUnitGasConfPart v2 0xca22 "IntegrationUnit"
      1-7/7: CChannelGasConfPart v4 0xca8f
    3/29: CDioTransferPart v2 0xcc1e "Resitor Channel 1"
    4/29: CDioTransferPart v2 0xcca8 "Resitor Channel 2"
    5/29: CDioTransferPart v2 0xcd1e "Resitor Channel 3"
    6/29: CDioTransferPart v2 0xcd94 "Resitor Channel 4"
    7/29: CDioTransferPart v2 0xce0a "Resitor Channel 5"
    8/29: CDioTransferPart v2 0xce80 "Resitor Channel 6"
    9/29: CDioTransferPart v2 0xcef6 "Resitor Channel 7"
    10/29: CDioTransferPart v2 0xcf6c "Resitor Channel 8"
    11/29: CDioTransferPart v2 0xcfe2 "Resitor Channel 9"
    12/29: CDioTransferPart v2 0xd058 "Resitor Channel 10"
    13/29: CPeakCenterOffset v1 0xd0d0
    14/29: CMagnetCurrentTransferPart v3 0xd117 "MagnetCurrent"
    15/29: CCalibration v5 0xd1ab "CO2_280617"
      1/3: CCalibrationPoint v3 0xd1ef "0. Point"
      2/3: CCalibrationPoint v3 0xd247 "1. Point"
      3/3: CCalibrationPoint v3 0xd29f "2. Point"
    16/29: CScaleHvTransferPart v2 0xed5b "Isotope MS/ScaleHv"
    17/29: CCalculatingDacTransferPart v1 0xede3 "Trap"
    18/29: CCalculatingDacTransferPart v1 0xee50 "Electron Energy"
    19/29: CCalculatingDacTransferPart v1 0xeeca "Emission"
    20/29: CCalculatingDacTransferPart v1 0xef28 "Extraction"
    21/29: CCalculatingDacTransferPart v1 0xef8e "Shield"
    22/29: CCalculatingDacTransferPart v1 0xefe4 "R-Plate"
    23/29: CCalculatingDacTransferPart v1 0xf03e "Einzel-Lens"
    24/29: CCalculatingDacTransferPart v1 0xf0a8 "Einzel-Lens Symmetry"
    25/29: CCalculatingDacTransferPart v1 0xf136 "X-Focus"
    26/29: CCalculatingDacTransferPart v1 0xf190 "X-Focus Symmetry"
    27/29: CCalculatingDacTransferPart v1 0xf20e "Y-Deflection"
    28/29: CCalculatingDacTransferPart v1 0xf27c "Y-Deflection Symmetry"
    29/29: CMolecule v1 0xf30e
