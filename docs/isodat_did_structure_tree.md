This is an example of a dual inlet `did` file structure tree (generated with the `--tree` flag of `isoextract`) from the `di_CO2_ex01.did` test file.

CFileHeader v6 0x0 "File Header"
  1/2: CTimeObject v1 0xbf "Date"
  2/2: CStr v2 0xfa "C:\Thermo\Isodat NT\Global\User\Dual Inlet System\Result Workshop\Default Result.IRW"
  CDataIndex v1 0x1e0
    1/1: CSeqLineIndexData v1 0x20a "Sequence Info"
      1/11: CData v3 0x255 "Row"
      2/11: CData v3 0x27a "Peak Center"
      3/11: CData v3 0x2a6 "Background"
      4/11: CData v3 0x2d0 "Pressadjust"
      5/11: CData v3 0x2fc "Reference Refill"
      6/11: CData v3 0x332 "Identifier 1"
      7/11: CData v3 0x366 "Identifier 2"
      8/11: CData v3 0x3a4 "Analysis"
      9/11: CData v3 0x3d2 "Comment"
      10/11: CData v3 0x402 "Preparation"
      11/11: CData v3 0x42c "Method"
CDualInletBlockData v1 0x496 "DualInlet Data"
  1/12: CMeasurmentInfos v1 0x501 "ISL Infos"
    1/5: CISLScriptMessageData v1 0x555 "PC [62200]"
    2/5: CISLScriptMessageData v1 0x5d0 "Background: 4.71.."
    3/5: CISLScriptMessageData v1 0x6d8 "PressAdj: L: 124.."
    4/5: CISLScriptMessageData v1 0x7a2 " mBar l 54.1   l.."
    5/5: CISLScriptMessageData v1 0x834 " mBar r 53.4   r.."
  2/12: CMeasurmentErrors v1 0x8c8 "ISL Errors"
  3/12: CDualInletRawData v1 0x925 "Raw"
    1/3: CBlockData v2 0x962 "DualInlet RawData Standard Block"
      1/7: CIntegrationUnitTransferPart v3 0xa0c "Standard 0"
        1/1: CIntensityData v6 0xa68
        CIntegrationUnitGasConfPart v2 0xaf3 "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0xb7f
      2/7: CIntegrationUnitTransferPart v3 0xd25 "Standard 1"
        1/1: CIntensityData v6 0xd61
        CIntegrationUnitGasConfPart v2 0xdda "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0xe47
      3/7: CIntegrationUnitTransferPart v3 0xfd6 "Standard 2"
        1/1: CIntensityData v6 0x1012
        CIntegrationUnitGasConfPart v2 0x108b "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x10f8
      4/7: CIntegrationUnitTransferPart v3 0x1287 "Standard 3"
        1/1: CIntensityData v6 0x12c3
        CIntegrationUnitGasConfPart v2 0x133c "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x13a9
      5/7: CIntegrationUnitTransferPart v3 0x1538 "Standard 4"
        1/1: CIntensityData v6 0x1574
        CIntegrationUnitGasConfPart v2 0x15ed "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x165a
      6/7: CIntegrationUnitTransferPart v3 0x17e9 "Standard 5"
        1/1: CIntensityData v6 0x1825
        CIntegrationUnitGasConfPart v2 0x189e "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x190b
      7/7: CIntegrationUnitTransferPart v3 0x1a9a "Standard 6"
        1/1: CIntensityData v6 0x1ad6
        CIntegrationUnitGasConfPart v2 0x1b4f "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x1bbc
    2/3: CBlockData v2 0x1d4b "DualInlet RawData Sample Block"
      1/7: CIntegrationUnitTransferPart v3 0x1ddf "Sample 0"
        1/1: CIntensityData v6 0x1e17
        CIntegrationUnitGasConfPart v2 0x1e90 "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x1efd
      2/7: CIntegrationUnitTransferPart v3 0x208c "Sample 1"
        1/1: CIntensityData v6 0x20c4
        CIntegrationUnitGasConfPart v2 0x213d "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x21aa
      3/7: CIntegrationUnitTransferPart v3 0x2339 "Sample 2"
        1/1: CIntensityData v6 0x2371
        CIntegrationUnitGasConfPart v2 0x23ea "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x2457
      4/7: CIntegrationUnitTransferPart v3 0x25e6 "Sample 3"
        1/1: CIntensityData v6 0x261e
        CIntegrationUnitGasConfPart v2 0x2697 "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x2704
      5/7: CIntegrationUnitTransferPart v3 0x2893 "Sample 4"
        1/1: CIntensityData v6 0x28cb
        CIntegrationUnitGasConfPart v2 0x2944 "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x29b1
      6/7: CIntegrationUnitTransferPart v3 0x2b40 "Sample 5"
        1/1: CIntensityData v6 0x2b78
        CIntegrationUnitGasConfPart v2 0x2bf1 "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x2c5e
      7/7: CIntegrationUnitTransferPart v3 0x2ded "Sample 6"
        1/1: CIntensityData v6 0x2e25
        CIntegrationUnitGasConfPart v2 0x2e9e "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x2f0b
    3/3: CIntegrationUnitTransferPart v3 0x309a "Standard Pre"
      1/1: CIntensityData v6 0x30da
      CIntegrationUnitGasConfPart v2 0x3153 "IntegrationUnit"
        1-7/7: CChannelGasConfPart v4 0x31c0
  4/12: CBlockData v2 0x3353 "Index to Mass"
    1/7: CData v3 0x33a3 "Mass 44"
    2/7: CData v3 0x33c9 "Mass 45"
    3/7: CData v3 0x33ef "Mass 46"
    4/7: CData v3 0x3415 "Mass 47"
    5/7: CData v3 0x343b "Mass 54"
    6/7: CData v3 0x3461 "Mass 48"
    7/7: CData v3 0x3487 "Mass 49"
  5/12: CBlockData v2 0x34ad "Pre Calculated"
    1/15: CDualInletShout v1 0x3501 "Reference Pre"
      1/6: CBlockData v2 0x354a "Drift Corrected"
        1/7: CTwoDoublesArrayData v1 0x35a2 "44"
        2/7: CTwoDoublesArrayData v1 0x35fe "45"
        3/7: CTwoDoublesArrayData v1 0x3642 "46"
        4/7: CTwoDoublesArrayData v1 0x3686 "47"
        5/7: CTwoDoublesArrayData v1 0x36ca "54"
        6/7: CTwoDoublesArrayData v1 0x370e "48"
        7/7: CTwoDoublesArrayData v1 0x3752 "49"
      2/6: CBlockData v2 0x3796 "Orginal"
        1/7: CTwoDoublesArrayData v1 0x37ce "44"
        2/7: CTwoDoublesArrayData v1 0x3812 "45"
        3/7: CTwoDoublesArrayData v1 0x3856 "46"
        4/7: CTwoDoublesArrayData v1 0x389a "47"
        5/7: CTwoDoublesArrayData v1 0x38de "54"
        6/7: CTwoDoublesArrayData v1 0x3922 "48"
        7/7: CTwoDoublesArrayData v1 0x3966 "49"
      3/6: CBlockData v2 0x39aa "Outlier"
        1/7: CStatusArrayData v1 0x39e2 "44"
        2/7: CStatusArrayData v1 0x3a15 "45"
        3/7: CStatusArrayData v1 0x3a34 "46"
        4/7: CStatusArrayData v1 0x3a53 "47"
        5/7: CStatusArrayData v1 0x3a72 "54"
        6/7: CStatusArrayData v1 0x3a91 "48"
        7/7: CStatusArrayData v1 0x3ab0 "49"
      4/6: CBlockData v2 0x3acf "Ratio Outlier"
        1/4: CStatusArrayData v1 0x3b1f "45CO2/44CO2"
        2/4: COutlierData v1 0x3b50 "45CO2/44CO2"
        3/4: CStatusArrayData v1 0x3b9e "46CO2/44CO2"
        4/4: COutlierData v1 0x3bcf "46CO2/44CO2"
      5/6: CBlockData v2 0x3c0d "Ratio"
        1-2/2: CTwoDoublesArrayData v1 0x3c3d "Reference Pre"
      6/6: CIntegrationUnitTransferPart v3 0x3d1d
        1/1: CIntensityData v6 0x3d45
        CIntegrationUnitGasConfPart v2 0x3db7 "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x3e24
    2/15: CDualInletShout v1 0x3fb7 "Sample 1"
      1/6: CBlockData v2 0x3fe3 "Drift Corrected"
        1/7: CTwoDoublesArrayData v1 0x403b "44"
        2/7: CTwoDoublesArrayData v1 0x407f "45"
        3/7: CTwoDoublesArrayData v1 0x40c3 "46"
        4/7: CTwoDoublesArrayData v1 0x4107 "47"
        5/7: CTwoDoublesArrayData v1 0x414b "54"
        6/7: CTwoDoublesArrayData v1 0x418f "48"
        7/7: CTwoDoublesArrayData v1 0x41d3 "49"
      2/6: CBlockData v2 0x4217 "Orginal"
        1/7: CTwoDoublesArrayData v1 0x424f "44"
        2/7: CTwoDoublesArrayData v1 0x4293 "45"
        3/7: CTwoDoublesArrayData v1 0x42d7 "46"
        4/7: CTwoDoublesArrayData v1 0x431b "47"
        5/7: CTwoDoublesArrayData v1 0x435f "54"
        6/7: CTwoDoublesArrayData v1 0x43a3 "48"
        7/7: CTwoDoublesArrayData v1 0x43e7 "49"
      3/6: CBlockData v2 0x442b "Outlier"
        1/7: CStatusArrayData v1 0x4463 "44"
        2/7: CStatusArrayData v1 0x4482 "45"
        3/7: CStatusArrayData v1 0x44a1 "46"
        4/7: CStatusArrayData v1 0x44c0 "47"
        5/7: CStatusArrayData v1 0x44df "54"
        6/7: CStatusArrayData v1 0x44fe "48"
        7/7: CStatusArrayData v1 0x451d "49"
      4/6: CBlockData v2 0x453c "Ratio Outlier"
        1/4: CStatusArrayData v1 0x458c "45CO2/44CO2"
        2/4: COutlierData v1 0x45bd "45CO2/44CO2"
        3/4: CStatusArrayData v1 0x45fb "46CO2/44CO2"
        4/4: COutlierData v1 0x462c "46CO2/44CO2"
      5/6: CBlockData v2 0x466a "Ratio"
        1-2/2: CTwoDoublesArrayData v1 0x469a "Sample 1"
      6/6: CIntegrationUnitTransferPart v3 0x4752
        1/1: CIntensityData v6 0x477a
        CIntegrationUnitGasConfPart v2 0x47ec "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x4859
    3/15: CDualInletShout v1 0x49ec "Reference 1"
      1/6: CBlockData v2 0x4a1e "Drift Corrected"
        1/7: CTwoDoublesArrayData v1 0x4a76 "44"
        2/7: CTwoDoublesArrayData v1 0x4aba "45"
        3/7: CTwoDoublesArrayData v1 0x4afe "46"
        4/7: CTwoDoublesArrayData v1 0x4b42 "47"
        5/7: CTwoDoublesArrayData v1 0x4b86 "54"
        6/7: CTwoDoublesArrayData v1 0x4bca "48"
        7/7: CTwoDoublesArrayData v1 0x4c0e "49"
      2/6: CBlockData v2 0x4c52 "Orginal"
        1/7: CTwoDoublesArrayData v1 0x4c8a "44"
        2/7: CTwoDoublesArrayData v1 0x4cce "45"
        3/7: CTwoDoublesArrayData v1 0x4d12 "46"
        4/7: CTwoDoublesArrayData v1 0x4d56 "47"
        5/7: CTwoDoublesArrayData v1 0x4d9a "54"
        6/7: CTwoDoublesArrayData v1 0x4dde "48"
        7/7: CTwoDoublesArrayData v1 0x4e22 "49"
      3/6: CBlockData v2 0x4e66 "Outlier"
        1/7: CStatusArrayData v1 0x4e9e "44"
        2/7: CStatusArrayData v1 0x4ebd "45"
        3/7: CStatusArrayData v1 0x4edc "46"
        4/7: CStatusArrayData v1 0x4efb "47"
        5/7: CStatusArrayData v1 0x4f1a "54"
        6/7: CStatusArrayData v1 0x4f39 "48"
        7/7: CStatusArrayData v1 0x4f58 "49"
      4/6: CBlockData v2 0x4f77 "Ratio Outlier"
        1/4: CStatusArrayData v1 0x4fc7 "45CO2/44CO2"
        2/4: COutlierData v1 0x4ff8 "45CO2/44CO2"
        3/4: CStatusArrayData v1 0x5036 "46CO2/44CO2"
        4/4: COutlierData v1 0x5067 "46CO2/44CO2"
      5/6: CBlockData v2 0x50a5 "Ratio"
        1-2/2: CTwoDoublesArrayData v1 0x50d5 "Reference 1"
      6/6: CIntegrationUnitTransferPart v3 0x51a5
        1/1: CIntensityData v6 0x51cd
        CIntegrationUnitGasConfPart v2 0x523f "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x52ac
    4/15: CDualInletShout v1 0x543f "Sample 2"
      1/6: CBlockData v2 0x546b "Drift Corrected"
        1/7: CTwoDoublesArrayData v1 0x54c3 "44"
        2/7: CTwoDoublesArrayData v1 0x5507 "45"
        3/7: CTwoDoublesArrayData v1 0x554b "46"
        4/7: CTwoDoublesArrayData v1 0x558f "47"
        5/7: CTwoDoublesArrayData v1 0x55d3 "54"
        6/7: CTwoDoublesArrayData v1 0x5617 "48"
        7/7: CTwoDoublesArrayData v1 0x565b "49"
      2/6: CBlockData v2 0x569f "Orginal"
        1/7: CTwoDoublesArrayData v1 0x56d7 "44"
        2/7: CTwoDoublesArrayData v1 0x571b "45"
        3/7: CTwoDoublesArrayData v1 0x575f "46"
        4/7: CTwoDoublesArrayData v1 0x57a3 "47"
        5/7: CTwoDoublesArrayData v1 0x57e7 "54"
        6/7: CTwoDoublesArrayData v1 0x582b "48"
        7/7: CTwoDoublesArrayData v1 0x586f "49"
      3/6: CBlockData v2 0x58b3 "Outlier"
        1/7: CStatusArrayData v1 0x58eb "44"
        2/7: CStatusArrayData v1 0x590a "45"
        3/7: CStatusArrayData v1 0x5929 "46"
        4/7: CStatusArrayData v1 0x5948 "47"
        5/7: CStatusArrayData v1 0x5967 "54"
        6/7: CStatusArrayData v1 0x5986 "48"
        7/7: CStatusArrayData v1 0x59a5 "49"
      4/6: CBlockData v2 0x59c4 "Ratio Outlier"
        1/4: CStatusArrayData v1 0x5a14 "45CO2/44CO2"
        2/4: COutlierData v1 0x5a45 "45CO2/44CO2"
        3/4: CStatusArrayData v1 0x5a83 "46CO2/44CO2"
        4/4: COutlierData v1 0x5ab4 "46CO2/44CO2"
      5/6: CBlockData v2 0x5af2 "Ratio"
        1-2/2: CTwoDoublesArrayData v1 0x5b22 "Sample 2"
      6/6: CIntegrationUnitTransferPart v3 0x5bda
        1/1: CIntensityData v6 0x5c02
        CIntegrationUnitGasConfPart v2 0x5c74 "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x5ce1
    5/15: CDualInletShout v1 0x5e74 "Reference 2"
      1/6: CBlockData v2 0x5ea6 "Drift Corrected"
        1/7: CTwoDoublesArrayData v1 0x5efe "44"
        2/7: CTwoDoublesArrayData v1 0x5f42 "45"
        3/7: CTwoDoublesArrayData v1 0x5f86 "46"
        4/7: CTwoDoublesArrayData v1 0x5fca "47"
        5/7: CTwoDoublesArrayData v1 0x600e "54"
        6/7: CTwoDoublesArrayData v1 0x6052 "48"
        7/7: CTwoDoublesArrayData v1 0x6096 "49"
      2/6: CBlockData v2 0x60da "Orginal"
        1/7: CTwoDoublesArrayData v1 0x6112 "44"
        2/7: CTwoDoublesArrayData v1 0x6156 "45"
        3/7: CTwoDoublesArrayData v1 0x619a "46"
        4/7: CTwoDoublesArrayData v1 0x61de "47"
        5/7: CTwoDoublesArrayData v1 0x6222 "54"
        6/7: CTwoDoublesArrayData v1 0x6266 "48"
        7/7: CTwoDoublesArrayData v1 0x62aa "49"
      3/6: CBlockData v2 0x62ee "Outlier"
        1/7: CStatusArrayData v1 0x6326 "44"
        2/7: CStatusArrayData v1 0x6345 "45"
        3/7: CStatusArrayData v1 0x6364 "46"
        4/7: CStatusArrayData v1 0x6383 "47"
        5/7: CStatusArrayData v1 0x63a2 "54"
        6/7: CStatusArrayData v1 0x63c1 "48"
        7/7: CStatusArrayData v1 0x63e0 "49"
      4/6: CBlockData v2 0x63ff "Ratio Outlier"
        1/4: CStatusArrayData v1 0x644f "45CO2/44CO2"
        2/4: COutlierData v1 0x6480 "45CO2/44CO2"
        3/4: CStatusArrayData v1 0x64be "46CO2/44CO2"
        4/4: COutlierData v1 0x64ef "46CO2/44CO2"
      5/6: CBlockData v2 0x652d "Ratio"
        1-2/2: CTwoDoublesArrayData v1 0x655d "Reference 2"
      6/6: CIntegrationUnitTransferPart v3 0x662d
        1/1: CIntensityData v6 0x6655
        CIntegrationUnitGasConfPart v2 0x66c7 "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x6734
    6/15: CDualInletShout v1 0x68c7 "Sample 3"
      1/6: CBlockData v2 0x68f3 "Drift Corrected"
        1/7: CTwoDoublesArrayData v1 0x694b "44"
        2/7: CTwoDoublesArrayData v1 0x698f "45"
        3/7: CTwoDoublesArrayData v1 0x69d3 "46"
        4/7: CTwoDoublesArrayData v1 0x6a17 "47"
        5/7: CTwoDoublesArrayData v1 0x6a5b "54"
        6/7: CTwoDoublesArrayData v1 0x6a9f "48"
        7/7: CTwoDoublesArrayData v1 0x6ae3 "49"
      2/6: CBlockData v2 0x6b27 "Orginal"
        1/7: CTwoDoublesArrayData v1 0x6b5f "44"
        2/7: CTwoDoublesArrayData v1 0x6ba3 "45"
        3/7: CTwoDoublesArrayData v1 0x6be7 "46"
        4/7: CTwoDoublesArrayData v1 0x6c2b "47"
        5/7: CTwoDoublesArrayData v1 0x6c6f "54"
        6/7: CTwoDoublesArrayData v1 0x6cb3 "48"
        7/7: CTwoDoublesArrayData v1 0x6cf7 "49"
      3/6: CBlockData v2 0x6d3b "Outlier"
        1/7: CStatusArrayData v1 0x6d73 "44"
        2/7: CStatusArrayData v1 0x6d92 "45"
        3/7: CStatusArrayData v1 0x6db1 "46"
        4/7: CStatusArrayData v1 0x6dd0 "47"
        5/7: CStatusArrayData v1 0x6def "54"
        6/7: CStatusArrayData v1 0x6e0e "48"
        7/7: CStatusArrayData v1 0x6e2d "49"
      4/6: CBlockData v2 0x6e4c "Ratio Outlier"
        1/4: CStatusArrayData v1 0x6e9c "45CO2/44CO2"
        2/4: COutlierData v1 0x6ecd "45CO2/44CO2"
        3/4: CStatusArrayData v1 0x6f0b "46CO2/44CO2"
        4/4: COutlierData v1 0x6f3c "46CO2/44CO2"
      5/6: CBlockData v2 0x6f7a "Ratio"
        1-2/2: CTwoDoublesArrayData v1 0x6faa "Sample 3"
      6/6: CIntegrationUnitTransferPart v3 0x7062
        1/1: CIntensityData v6 0x708a
        CIntegrationUnitGasConfPart v2 0x70fc "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x7169
    7/15: CDualInletShout v1 0x72fc "Reference 3"
      1/6: CBlockData v2 0x732e "Drift Corrected"
        1/7: CTwoDoublesArrayData v1 0x7386 "44"
        2/7: CTwoDoublesArrayData v1 0x73ca "45"
        3/7: CTwoDoublesArrayData v1 0x740e "46"
        4/7: CTwoDoublesArrayData v1 0x7452 "47"
        5/7: CTwoDoublesArrayData v1 0x7496 "54"
        6/7: CTwoDoublesArrayData v1 0x74da "48"
        7/7: CTwoDoublesArrayData v1 0x751e "49"
      2/6: CBlockData v2 0x7562 "Orginal"
        1/7: CTwoDoublesArrayData v1 0x759a "44"
        2/7: CTwoDoublesArrayData v1 0x75de "45"
        3/7: CTwoDoublesArrayData v1 0x7622 "46"
        4/7: CTwoDoublesArrayData v1 0x7666 "47"
        5/7: CTwoDoublesArrayData v1 0x76aa "54"
        6/7: CTwoDoublesArrayData v1 0x76ee "48"
        7/7: CTwoDoublesArrayData v1 0x7732 "49"
      3/6: CBlockData v2 0x7776 "Outlier"
        1/7: CStatusArrayData v1 0x77ae "44"
        2/7: CStatusArrayData v1 0x77cd "45"
        3/7: CStatusArrayData v1 0x77ec "46"
        4/7: CStatusArrayData v1 0x780b "47"
        5/7: CStatusArrayData v1 0x782a "54"
        6/7: CStatusArrayData v1 0x7849 "48"
        7/7: CStatusArrayData v1 0x7868 "49"
      4/6: CBlockData v2 0x7887 "Ratio Outlier"
        1/4: CStatusArrayData v1 0x78d7 "45CO2/44CO2"
        2/4: COutlierData v1 0x7908 "45CO2/44CO2"
        3/4: CStatusArrayData v1 0x7946 "46CO2/44CO2"
        4/4: COutlierData v1 0x7977 "46CO2/44CO2"
      5/6: CBlockData v2 0x79b5 "Ratio"
        1-2/2: CTwoDoublesArrayData v1 0x79e5 "Reference 3"
      6/6: CIntegrationUnitTransferPart v3 0x7ab5
        1/1: CIntensityData v6 0x7add
        CIntegrationUnitGasConfPart v2 0x7b4f "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x7bbc
    8/15: CDualInletShout v1 0x7d4f "Sample 4"
      1/6: CBlockData v2 0x7d7b "Drift Corrected"
        1/7: CTwoDoublesArrayData v1 0x7dd3 "44"
        2/7: CTwoDoublesArrayData v1 0x7e17 "45"
        3/7: CTwoDoublesArrayData v1 0x7e5b "46"
        4/7: CTwoDoublesArrayData v1 0x7e9f "47"
        5/7: CTwoDoublesArrayData v1 0x7ee3 "54"
        6/7: CTwoDoublesArrayData v1 0x7f27 "48"
        7/7: CTwoDoublesArrayData v1 0x7f6b "49"
      2/6: CBlockData v2 0x7faf "Orginal"
        1/7: CTwoDoublesArrayData v1 0x7fe7 "44"
        2/7: CTwoDoublesArrayData v1 0x802b "45"
        3/7: CTwoDoublesArrayData v1 0x806f "46"
        4/7: CTwoDoublesArrayData v1 0x80b3 "47"
        5/7: CTwoDoublesArrayData v1 0x80f7 "54"
        6/7: CTwoDoublesArrayData v1 0x813b "48"
        7/7: CTwoDoublesArrayData v1 0x817f "49"
      3/6: CBlockData v2 0x81c3 "Outlier"
        1/7: CStatusArrayData v1 0x81fb "44"
        2/7: CStatusArrayData v1 0x821a "45"
        3/7: CStatusArrayData v1 0x8239 "46"
        4/7: CStatusArrayData v1 0x8258 "47"
        5/7: CStatusArrayData v1 0x8277 "54"
        6/7: CStatusArrayData v1 0x8296 "48"
        7/7: CStatusArrayData v1 0x82b5 "49"
      4/6: CBlockData v2 0x82d4 "Ratio Outlier"
        1/4: CStatusArrayData v1 0x8324 "45CO2/44CO2"
        2/4: COutlierData v1 0x8355 "45CO2/44CO2"
        3/4: CStatusArrayData v1 0x8393 "46CO2/44CO2"
        4/4: COutlierData v1 0x83c4 "46CO2/44CO2"
      5/6: CBlockData v2 0x8402 "Ratio"
        1-2/2: CTwoDoublesArrayData v1 0x8432 "Sample 4"
      6/6: CIntegrationUnitTransferPart v3 0x84ea
        1/1: CIntensityData v6 0x8512
        CIntegrationUnitGasConfPart v2 0x8584 "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x85f1
    9/15: CDualInletShout v1 0x8784 "Reference 4"
      1/6: CBlockData v2 0x87b6 "Drift Corrected"
        1/7: CTwoDoublesArrayData v1 0x880e "44"
        2/7: CTwoDoublesArrayData v1 0x8852 "45"
        3/7: CTwoDoublesArrayData v1 0x8896 "46"
        4/7: CTwoDoublesArrayData v1 0x88da "47"
        5/7: CTwoDoublesArrayData v1 0x891e "54"
        6/7: CTwoDoublesArrayData v1 0x8962 "48"
        7/7: CTwoDoublesArrayData v1 0x89a6 "49"
      2/6: CBlockData v2 0x89ea "Orginal"
        1/7: CTwoDoublesArrayData v1 0x8a22 "44"
        2/7: CTwoDoublesArrayData v1 0x8a66 "45"
        3/7: CTwoDoublesArrayData v1 0x8aaa "46"
        4/7: CTwoDoublesArrayData v1 0x8aee "47"
        5/7: CTwoDoublesArrayData v1 0x8b32 "54"
        6/7: CTwoDoublesArrayData v1 0x8b76 "48"
        7/7: CTwoDoublesArrayData v1 0x8bba "49"
      3/6: CBlockData v2 0x8bfe "Outlier"
        1/7: CStatusArrayData v1 0x8c36 "44"
        2/7: CStatusArrayData v1 0x8c55 "45"
        3/7: CStatusArrayData v1 0x8c74 "46"
        4/7: CStatusArrayData v1 0x8c93 "47"
        5/7: CStatusArrayData v1 0x8cb2 "54"
        6/7: CStatusArrayData v1 0x8cd1 "48"
        7/7: CStatusArrayData v1 0x8cf0 "49"
      4/6: CBlockData v2 0x8d0f "Ratio Outlier"
        1/4: CStatusArrayData v1 0x8d5f "45CO2/44CO2"
        2/4: COutlierData v1 0x8d90 "45CO2/44CO2"
        3/4: CStatusArrayData v1 0x8dce "46CO2/44CO2"
        4/4: COutlierData v1 0x8dff "46CO2/44CO2"
      5/6: CBlockData v2 0x8e3d "Ratio"
        1-2/2: CTwoDoublesArrayData v1 0x8e6d "Reference 4"
      6/6: CIntegrationUnitTransferPart v3 0x8f3d
        1/1: CIntensityData v6 0x8f65
        CIntegrationUnitGasConfPart v2 0x8fd7 "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x9044
    10/15: CDualInletShout v1 0x91d7 "Sample 5"
      1/6: CBlockData v2 0x9203 "Drift Corrected"
        1/7: CTwoDoublesArrayData v1 0x925b "44"
        2/7: CTwoDoublesArrayData v1 0x929f "45"
        3/7: CTwoDoublesArrayData v1 0x92e3 "46"
        4/7: CTwoDoublesArrayData v1 0x9327 "47"
        5/7: CTwoDoublesArrayData v1 0x936b "54"
        6/7: CTwoDoublesArrayData v1 0x93af "48"
        7/7: CTwoDoublesArrayData v1 0x93f3 "49"
      2/6: CBlockData v2 0x9437 "Orginal"
        1/7: CTwoDoublesArrayData v1 0x946f "44"
        2/7: CTwoDoublesArrayData v1 0x94b3 "45"
        3/7: CTwoDoublesArrayData v1 0x94f7 "46"
        4/7: CTwoDoublesArrayData v1 0x953b "47"
        5/7: CTwoDoublesArrayData v1 0x957f "54"
        6/7: CTwoDoublesArrayData v1 0x95c3 "48"
        7/7: CTwoDoublesArrayData v1 0x9607 "49"
      3/6: CBlockData v2 0x964b "Outlier"
        1/7: CStatusArrayData v1 0x9683 "44"
        2/7: CStatusArrayData v1 0x96a2 "45"
        3/7: CStatusArrayData v1 0x96c1 "46"
        4/7: CStatusArrayData v1 0x96e0 "47"
        5/7: CStatusArrayData v1 0x96ff "54"
        6/7: CStatusArrayData v1 0x971e "48"
        7/7: CStatusArrayData v1 0x973d "49"
      4/6: CBlockData v2 0x975c "Ratio Outlier"
        1/4: CStatusArrayData v1 0x97ac "45CO2/44CO2"
        2/4: COutlierData v1 0x97dd "45CO2/44CO2"
        3/4: CStatusArrayData v1 0x981b "46CO2/44CO2"
        4/4: COutlierData v1 0x984c "46CO2/44CO2"
      5/6: CBlockData v2 0x988a "Ratio"
        1-2/2: CTwoDoublesArrayData v1 0x98ba "Sample 5"
      6/6: CIntegrationUnitTransferPart v3 0x9972
        1/1: CIntensityData v6 0x999a
        CIntegrationUnitGasConfPart v2 0x9a0c "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x9a79
    11/15: CDualInletShout v1 0x9c0c "Reference 5"
      1/6: CBlockData v2 0x9c3e "Drift Corrected"
        1/7: CTwoDoublesArrayData v1 0x9c96 "44"
        2/7: CTwoDoublesArrayData v1 0x9cda "45"
        3/7: CTwoDoublesArrayData v1 0x9d1e "46"
        4/7: CTwoDoublesArrayData v1 0x9d62 "47"
        5/7: CTwoDoublesArrayData v1 0x9da6 "54"
        6/7: CTwoDoublesArrayData v1 0x9dea "48"
        7/7: CTwoDoublesArrayData v1 0x9e2e "49"
      2/6: CBlockData v2 0x9e72 "Orginal"
        1/7: CTwoDoublesArrayData v1 0x9eaa "44"
        2/7: CTwoDoublesArrayData v1 0x9eee "45"
        3/7: CTwoDoublesArrayData v1 0x9f32 "46"
        4/7: CTwoDoublesArrayData v1 0x9f76 "47"
        5/7: CTwoDoublesArrayData v1 0x9fba "54"
        6/7: CTwoDoublesArrayData v1 0x9ffe "48"
        7/7: CTwoDoublesArrayData v1 0xa042 "49"
      3/6: CBlockData v2 0xa086 "Outlier"
        1/7: CStatusArrayData v1 0xa0be "44"
        2/7: CStatusArrayData v1 0xa0dd "45"
        3/7: CStatusArrayData v1 0xa0fc "46"
        4/7: CStatusArrayData v1 0xa11b "47"
        5/7: CStatusArrayData v1 0xa13a "54"
        6/7: CStatusArrayData v1 0xa159 "48"
        7/7: CStatusArrayData v1 0xa178 "49"
      4/6: CBlockData v2 0xa197 "Ratio Outlier"
        1/4: CStatusArrayData v1 0xa1e7 "45CO2/44CO2"
        2/4: COutlierData v1 0xa218 "45CO2/44CO2"
        3/4: CStatusArrayData v1 0xa256 "46CO2/44CO2"
        4/4: COutlierData v1 0xa287 "46CO2/44CO2"
      5/6: CBlockData v2 0xa2c5 "Ratio"
        1-2/2: CTwoDoublesArrayData v1 0xa2f5 "Reference 5"
      6/6: CIntegrationUnitTransferPart v3 0xa3c5
        1/1: CIntensityData v6 0xa3ed
        CIntegrationUnitGasConfPart v2 0xa45f "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0xa4cc
    12/15: CDualInletShout v1 0xa65f "Sample 6"
      1/6: CBlockData v2 0xa68b "Drift Corrected"
        1/7: CTwoDoublesArrayData v1 0xa6e3 "44"
        2/7: CTwoDoublesArrayData v1 0xa727 "45"
        3/7: CTwoDoublesArrayData v1 0xa76b "46"
        4/7: CTwoDoublesArrayData v1 0xa7af "47"
        5/7: CTwoDoublesArrayData v1 0xa7f3 "54"
        6/7: CTwoDoublesArrayData v1 0xa837 "48"
        7/7: CTwoDoublesArrayData v1 0xa87b "49"
      2/6: CBlockData v2 0xa8bf "Orginal"
        1/7: CTwoDoublesArrayData v1 0xa8f7 "44"
        2/7: CTwoDoublesArrayData v1 0xa93b "45"
        3/7: CTwoDoublesArrayData v1 0xa97f "46"
        4/7: CTwoDoublesArrayData v1 0xa9c3 "47"
        5/7: CTwoDoublesArrayData v1 0xaa07 "54"
        6/7: CTwoDoublesArrayData v1 0xaa4b "48"
        7/7: CTwoDoublesArrayData v1 0xaa8f "49"
      3/6: CBlockData v2 0xaad3 "Outlier"
        1/7: CStatusArrayData v1 0xab0b "44"
        2/7: CStatusArrayData v1 0xab2a "45"
        3/7: CStatusArrayData v1 0xab49 "46"
        4/7: CStatusArrayData v1 0xab68 "47"
        5/7: CStatusArrayData v1 0xab87 "54"
        6/7: CStatusArrayData v1 0xaba6 "48"
        7/7: CStatusArrayData v1 0xabc5 "49"
      4/6: CBlockData v2 0xabe4 "Ratio Outlier"
        1/4: CStatusArrayData v1 0xac34 "45CO2/44CO2"
        2/4: COutlierData v1 0xac65 "45CO2/44CO2"
        3/4: CStatusArrayData v1 0xaca3 "46CO2/44CO2"
        4/4: COutlierData v1 0xacd4 "46CO2/44CO2"
      5/6: CBlockData v2 0xad12 "Ratio"
        1-2/2: CTwoDoublesArrayData v1 0xad42 "Sample 6"
      6/6: CIntegrationUnitTransferPart v3 0xadfa
        1/1: CIntensityData v6 0xae22
        CIntegrationUnitGasConfPart v2 0xae94 "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0xaf01
    13/15: CDualInletShout v1 0xb094 "Reference 6"
      1/6: CBlockData v2 0xb0c6 "Drift Corrected"
        1/7: CTwoDoublesArrayData v1 0xb11e "44"
        2/7: CTwoDoublesArrayData v1 0xb162 "45"
        3/7: CTwoDoublesArrayData v1 0xb1a6 "46"
        4/7: CTwoDoublesArrayData v1 0xb1ea "47"
        5/7: CTwoDoublesArrayData v1 0xb22e "54"
        6/7: CTwoDoublesArrayData v1 0xb272 "48"
        7/7: CTwoDoublesArrayData v1 0xb2b6 "49"
      2/6: CBlockData v2 0xb2fa "Orginal"
        1/7: CTwoDoublesArrayData v1 0xb332 "44"
        2/7: CTwoDoublesArrayData v1 0xb376 "45"
        3/7: CTwoDoublesArrayData v1 0xb3ba "46"
        4/7: CTwoDoublesArrayData v1 0xb3fe "47"
        5/7: CTwoDoublesArrayData v1 0xb442 "54"
        6/7: CTwoDoublesArrayData v1 0xb486 "48"
        7/7: CTwoDoublesArrayData v1 0xb4ca "49"
      3/6: CBlockData v2 0xb50e "Outlier"
        1/7: CStatusArrayData v1 0xb546 "44"
        2/7: CStatusArrayData v1 0xb565 "45"
        3/7: CStatusArrayData v1 0xb584 "46"
        4/7: CStatusArrayData v1 0xb5a3 "47"
        5/7: CStatusArrayData v1 0xb5c2 "54"
        6/7: CStatusArrayData v1 0xb5e1 "48"
        7/7: CStatusArrayData v1 0xb600 "49"
      4/6: CBlockData v2 0xb61f "Ratio Outlier"
        1/4: CStatusArrayData v1 0xb66f "45CO2/44CO2"
        2/4: COutlierData v1 0xb6a0 "45CO2/44CO2"
        3/4: CStatusArrayData v1 0xb6de "46CO2/44CO2"
        4/4: COutlierData v1 0xb70f "46CO2/44CO2"
      5/6: CBlockData v2 0xb74d "Ratio"
        1-2/2: CTwoDoublesArrayData v1 0xb77d "Reference 6"
      6/6: CIntegrationUnitTransferPart v3 0xb84d
        1/1: CIntensityData v6 0xb875
        CIntegrationUnitGasConfPart v2 0xb8e7 "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0xb954
    14/15: CDualInletShout v1 0xbae7 "Sample 7"
      1/6: CBlockData v2 0xbb13 "Drift Corrected"
        1/7: CTwoDoublesArrayData v1 0xbb6b "44"
        2/7: CTwoDoublesArrayData v1 0xbbaf "45"
        3/7: CTwoDoublesArrayData v1 0xbbf3 "46"
        4/7: CTwoDoublesArrayData v1 0xbc37 "47"
        5/7: CTwoDoublesArrayData v1 0xbc7b "54"
        6/7: CTwoDoublesArrayData v1 0xbcbf "48"
        7/7: CTwoDoublesArrayData v1 0xbd03 "49"
      2/6: CBlockData v2 0xbd47 "Orginal"
        1/7: CTwoDoublesArrayData v1 0xbd7f "44"
        2/7: CTwoDoublesArrayData v1 0xbdc3 "45"
        3/7: CTwoDoublesArrayData v1 0xbe07 "46"
        4/7: CTwoDoublesArrayData v1 0xbe4b "47"
        5/7: CTwoDoublesArrayData v1 0xbe8f "54"
        6/7: CTwoDoublesArrayData v1 0xbed3 "48"
        7/7: CTwoDoublesArrayData v1 0xbf17 "49"
      3/6: CBlockData v2 0xbf5b "Outlier"
        1/7: CStatusArrayData v1 0xbf93 "44"
        2/7: CStatusArrayData v1 0xbfb2 "45"
        3/7: CStatusArrayData v1 0xbfd1 "46"
        4/7: CStatusArrayData v1 0xbff0 "47"
        5/7: CStatusArrayData v1 0xc00f "54"
        6/7: CStatusArrayData v1 0xc02e "48"
        7/7: CStatusArrayData v1 0xc04d "49"
      4/6: CBlockData v2 0xc06c "Ratio Outlier"
        1/4: CStatusArrayData v1 0xc0bc "45CO2/44CO2"
        2/4: COutlierData v1 0xc0ed "45CO2/44CO2"
        3/4: CStatusArrayData v1 0xc12b "46CO2/44CO2"
        4/4: COutlierData v1 0xc15c "46CO2/44CO2"
      5/6: CBlockData v2 0xc19a "Ratio"
        1-2/2: CTwoDoublesArrayData v1 0xc1ca "Sample 7"
      6/6: CIntegrationUnitTransferPart v3 0xc282
        1/1: CIntensityData v6 0xc2aa
        CIntegrationUnitGasConfPart v2 0xc31c "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0xc389
    15/15: CDualInletShout v1 0xc51c "Reference 7"
      1/6: CBlockData v2 0xc54e "Drift Corrected"
        1/7: CTwoDoublesArrayData v1 0xc5a6 "44"
        2/7: CTwoDoublesArrayData v1 0xc5ea "45"
        3/7: CTwoDoublesArrayData v1 0xc62e "46"
        4/7: CTwoDoublesArrayData v1 0xc672 "47"
        5/7: CTwoDoublesArrayData v1 0xc6b6 "54"
        6/7: CTwoDoublesArrayData v1 0xc6fa "48"
        7/7: CTwoDoublesArrayData v1 0xc73e "49"
      2/6: CBlockData v2 0xc782 "Orginal"
        1/7: CTwoDoublesArrayData v1 0xc7ba "44"
        2/7: CTwoDoublesArrayData v1 0xc7fe "45"
        3/7: CTwoDoublesArrayData v1 0xc842 "46"
        4/7: CTwoDoublesArrayData v1 0xc886 "47"
        5/7: CTwoDoublesArrayData v1 0xc8ca "54"
        6/7: CTwoDoublesArrayData v1 0xc90e "48"
        7/7: CTwoDoublesArrayData v1 0xc952 "49"
      3/6: CBlockData v2 0xc996 "Outlier"
        1/7: CStatusArrayData v1 0xc9ce "44"
        2/7: CStatusArrayData v1 0xc9ed "45"
        3/7: CStatusArrayData v1 0xca0c "46"
        4/7: CStatusArrayData v1 0xca2b "47"
        5/7: CStatusArrayData v1 0xca4a "54"
        6/7: CStatusArrayData v1 0xca69 "48"
        7/7: CStatusArrayData v1 0xca88 "49"
      4/6: CBlockData v2 0xcaa7 "Ratio Outlier"
        1/4: CStatusArrayData v1 0xcaf7 "45CO2/44CO2"
        2/4: COutlierData v1 0xcb28 "45CO2/44CO2"
        3/4: CStatusArrayData v1 0xcb66 "46CO2/44CO2"
        4/4: COutlierData v1 0xcb97 "46CO2/44CO2"
      5/6: CBlockData v2 0xcbd5 "Ratio"
        1-2/2: CTwoDoublesArrayData v1 0xcc05 "Reference 7"
      6/6: CIntegrationUnitTransferPart v3 0xccd5
        1/1: CIntensityData v6 0xccfd
        CIntegrationUnitGasConfPart v2 0xcd6f "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0xcddc
  6/12: CBlockData v2 0xcf6f "Results"
    1/15: CBlockData v2 0xcfa7 "Reference Pre"
      1/4: CResultDataSimpleList v1 0xcfdd "Extra ITEMS"
        1/18: CResultDataSimple v2 0xd028 "1. Cycle Int. Ref. 44"
        2/18: CResultDataSimple v2 0xd0cb "1. Cycle Int. Ref. 45"
        3/18: CResultDataSimple v2 0xd159 "1. Cycle Int. Ref. 46"
        4/18: CResultDataSimple v2 0xd1e7 "1. Cycle Int. Ref. 47"
        5/18: CResultDataSimple v2 0xd275 "1. Cycle Int. Ref. 54"
        6/18: CResultDataSimple v2 0xd301 "1. Cycle Int. Ref. 48"
        7/18: CResultDataSimple v2 0xd38b "1. Cycle Int. Samp. 44"
        8/18: CResultDataSimple v2 0xd41d "1. Cycle Int. Samp. 45"
        9/18: CResultDataSimple v2 0xd4af "1. Cycle Int. Samp. 46"
        10/18: CResultDataSimple v2 0xd541 "1. Cycle Int. Samp. 47"
        11/18: CResultDataSimple v2 0xd5d3 "1. Cycle Int. Samp. 54"
        12/18: CResultDataSimple v2 0xd663 "1. Cycle Int. Samp. 48"
        13/18: CResultDataSimple v2 0xd6f1 "1. Cycle Int. Samp. 44"
        14/18: CResultDataSimple v2 0xd783 "1. Cycle Int. Samp. 45"
        15/18: CResultDataSimple v2 0xd815 "1. Cycle Int. Samp. 46"
        16/18: CResultDataSimple v2 0xd8a7 "1. Cycle Int. Samp. 47"
        17/18: CResultDataSimple v2 0xd939 "1. Cycle Int. Samp. 54"
        18/18: CResultDataSimple v2 0xd9c9 "1. Cycle Int. Samp. 48"
      2/4: CResultDataSimpleList v1 0xda5b "CO2"
        1/13: CResultDataSimple v2 0xda7d "rR 45CO2/44CO2    "
        2/13: CResultDataSimple v2 0xdaf1 "rR 46CO2/44CO2    "
        3/13: CResultDataSimple v2 0xdb65 "R 45CO2/44CO2    "
        4/13: CResultDataSimple v2 0xdbd5 "R 46CO2/44CO2    "
        5/13: CResultDataSimple v2 0xdc45 "rd 45CO2/44CO2 [per mil]  vs. Yellowstone"
        6/13: CResultDataSimple v2 0xdd0f "rd 46CO2/44CO2 [per mil]  vs. Yellowstone"
        7/13: CResultDataSimple v2 0xddd9 "d 45CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        8/13: CResultDataSimple v2 0xde9d "d 46CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        9/13: CResultDataSimple v2 0xdf61 "d 13C/12C [per mil]  vs. VPDB"
        10/13: CResultDataSimple v2 0xe007 "d 18O/16O [per mil]  vs. VSMOW"
        11/13: CResultDataSimple v2 0xe0af "d 17O/16O    vs.  "
        12/13: CResultDataSimple v2 0xe127 "AT% 13C/12C [%]  "
        13/13: CResultDataSimple v2 0xe195 "AT% 18O/16O [%]  "
      3/4: CResultDataSimpleList v1 0xe207 "Extra ITEMS"
        1/3: CResultDataSimple v2 0xe239 "1. Cycle Int. Ref. 44"
        2-3/3: CResultDataSimple v2 0xe2c3 "1. Cycle Int. Samp. 44"
      4/4: CResultDataSimpleList v1 0xe3e3 "baseline"
    2/15: CBlockData v2 0xe413 "Sample 1"
      1/2: CResultDataSimpleList v1 0xe43f "CO2"
        1/13: CResultDataSimple v2 0xe461 "rR 45CO2/44CO2    "
        2/13: CResultDataSimple v2 0xe4d5 "rR 46CO2/44CO2    "
        3/13: CResultDataSimple v2 0xe549 "R 45CO2/44CO2    "
        4/13: CResultDataSimple v2 0xe5b9 "R 46CO2/44CO2    "
        5/13: CResultDataSimple v2 0xe629 "rd 45CO2/44CO2 [per mil]  vs. Yellowstone"
        6/13: CResultDataSimple v2 0xe6f5 "rd 46CO2/44CO2 [per mil]  vs. Yellowstone"
        7/13: CResultDataSimple v2 0xe7bf "d 45CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        8/13: CResultDataSimple v2 0xe883 "d 46CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        9/13: CResultDataSimple v2 0xe947 "d 13C/12C [per mil]  vs. VPDB"
        10/13: CResultDataSimple v2 0xe9ed "d 18O/16O [per mil]  vs. VSMOW"
        11/13: CResultDataSimple v2 0xea95 "d 17O/16O    vs.  "
        12/13: CResultDataSimple v2 0xeb0b "AT% 13C/12C [%]  "
        13/13: CResultDataSimple v2 0xeb79 "AT% 18O/16O [%]  "
      2/2: CResultDataSimpleList v1 0xebeb "baseline"
    3/15: CBlockData v2 0xec1b "Reference 1"
      1/2: CResultDataSimpleList v1 0xec4d "CO2"
        1/13: CResultDataSimple v2 0xec6f "rR 45CO2/44CO2    "
        2/13: CResultDataSimple v2 0xece3 "rR 46CO2/44CO2    "
        3/13: CResultDataSimple v2 0xed57 "R 45CO2/44CO2    "
        4/13: CResultDataSimple v2 0xedc7 "R 46CO2/44CO2    "
        5/13: CResultDataSimple v2 0xee37 "rd 45CO2/44CO2 [per mil]  vs. Yellowstone"
        6/13: CResultDataSimple v2 0xef01 "rd 46CO2/44CO2 [per mil]  vs. Yellowstone"
        7/13: CResultDataSimple v2 0xefcb "d 45CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        8/13: CResultDataSimple v2 0xf08f "d 46CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        9/13: CResultDataSimple v2 0xf153 "d 13C/12C [per mil]  vs. VPDB"
        10/13: CResultDataSimple v2 0xf1f9 "d 18O/16O [per mil]  vs. VSMOW"
        11/13: CResultDataSimple v2 0xf2a1 "d 17O/16O    vs.  "
        12/13: CResultDataSimple v2 0xf319 "AT% 13C/12C [%]  "
        13/13: CResultDataSimple v2 0xf387 "AT% 18O/16O [%]  "
      2/2: CResultDataSimpleList v1 0xf3f9 "baseline"
    4/15: CBlockData v2 0xf429 "Sample 2"
      1/2: CResultDataSimpleList v1 0xf455 "CO2"
        1/13: CResultDataSimple v2 0xf477 "rR 45CO2/44CO2    "
        2/13: CResultDataSimple v2 0xf4eb "rR 46CO2/44CO2    "
        3/13: CResultDataSimple v2 0xf55f "R 45CO2/44CO2    "
        4/13: CResultDataSimple v2 0xf5cf "R 46CO2/44CO2    "
        5/13: CResultDataSimple v2 0xf63f "rd 45CO2/44CO2 [per mil]  vs. Yellowstone"
        6/13: CResultDataSimple v2 0xf709 "rd 46CO2/44CO2 [per mil]  vs. Yellowstone"
        7/13: CResultDataSimple v2 0xf7d3 "d 45CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        8/13: CResultDataSimple v2 0xf897 "d 46CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        9/13: CResultDataSimple v2 0xf95b "d 13C/12C [per mil]  vs. VPDB"
        10/13: CResultDataSimple v2 0xfa01 "d 18O/16O [per mil]  vs. VSMOW"
        11/13: CResultDataSimple v2 0xfaa9 "d 17O/16O    vs.  "
        12/13: CResultDataSimple v2 0xfb1f "AT% 13C/12C [%]  "
        13/13: CResultDataSimple v2 0xfb8d "AT% 18O/16O [%]  "
      2/2: CResultDataSimpleList v1 0xfbff "baseline"
    5/15: CBlockData v2 0xfc2f "Reference 2"
      1/2: CResultDataSimpleList v1 0xfc61 "CO2"
        1/13: CResultDataSimple v2 0xfc83 "rR 45CO2/44CO2    "
        2/13: CResultDataSimple v2 0xfcf7 "rR 46CO2/44CO2    "
        3/13: CResultDataSimple v2 0xfd6b "R 45CO2/44CO2    "
        4/13: CResultDataSimple v2 0xfddb "R 46CO2/44CO2    "
        5/13: CResultDataSimple v2 0xfe4b "rd 45CO2/44CO2 [per mil]  vs. Yellowstone"
        6/13: CResultDataSimple v2 0xff15 "rd 46CO2/44CO2 [per mil]  vs. Yellowstone"
        7/13: CResultDataSimple v2 0xffdf "d 45CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        8/13: CResultDataSimple v2 0x100a3 "d 46CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        9/13: CResultDataSimple v2 0x10167 "d 13C/12C [per mil]  vs. VPDB"
        10/13: CResultDataSimple v2 0x1020d "d 18O/16O [per mil]  vs. VSMOW"
        11/13: CResultDataSimple v2 0x102b5 "d 17O/16O    vs.  "
        12/13: CResultDataSimple v2 0x1032d "AT% 13C/12C [%]  "
        13/13: CResultDataSimple v2 0x1039b "AT% 18O/16O [%]  "
      2/2: CResultDataSimpleList v1 0x1040d "baseline"
    6/15: CBlockData v2 0x1043d "Sample 3"
      1/2: CResultDataSimpleList v1 0x10469 "CO2"
        1/13: CResultDataSimple v2 0x1048b "rR 45CO2/44CO2    "
        2/13: CResultDataSimple v2 0x104ff "rR 46CO2/44CO2    "
        3/13: CResultDataSimple v2 0x10573 "R 45CO2/44CO2    "
        4/13: CResultDataSimple v2 0x105e3 "R 46CO2/44CO2    "
        5/13: CResultDataSimple v2 0x10653 "rd 45CO2/44CO2 [per mil]  vs. Yellowstone"
        6/13: CResultDataSimple v2 0x1071d "rd 46CO2/44CO2 [per mil]  vs. Yellowstone"
        7/13: CResultDataSimple v2 0x107e9 "d 45CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        8/13: CResultDataSimple v2 0x108ad "d 46CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        9/13: CResultDataSimple v2 0x10971 "d 13C/12C [per mil]  vs. VPDB"
        10/13: CResultDataSimple v2 0x10a17 "d 18O/16O [per mil]  vs. VSMOW"
        11/13: CResultDataSimple v2 0x10abf "d 17O/16O    vs.  "
        12/13: CResultDataSimple v2 0x10b37 "AT% 13C/12C [%]  "
        13/13: CResultDataSimple v2 0x10ba5 "AT% 18O/16O [%]  "
      2/2: CResultDataSimpleList v1 0x10c17 "baseline"
    7/15: CBlockData v2 0x10c47 "Reference 3"
      1/2: CResultDataSimpleList v1 0x10c79 "CO2"
        1/13: CResultDataSimple v2 0x10c9b "rR 45CO2/44CO2    "
        2/13: CResultDataSimple v2 0x10d0f "rR 46CO2/44CO2    "
        3/13: CResultDataSimple v2 0x10d83 "R 45CO2/44CO2    "
        4/13: CResultDataSimple v2 0x10df3 "R 46CO2/44CO2    "
        5/13: CResultDataSimple v2 0x10e63 "rd 45CO2/44CO2 [per mil]  vs. Yellowstone"
        6/13: CResultDataSimple v2 0x10f2d "rd 46CO2/44CO2 [per mil]  vs. Yellowstone"
        7/13: CResultDataSimple v2 0x10ff7 "d 45CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        8/13: CResultDataSimple v2 0x110bb "d 46CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        9/13: CResultDataSimple v2 0x1117f "d 13C/12C [per mil]  vs. VPDB"
        10/13: CResultDataSimple v2 0x11225 "d 18O/16O [per mil]  vs. VSMOW"
        11/13: CResultDataSimple v2 0x112cd "d 17O/16O    vs.  "
        12/13: CResultDataSimple v2 0x11345 "AT% 13C/12C [%]  "
        13/13: CResultDataSimple v2 0x113b3 "AT% 18O/16O [%]  "
      2/2: CResultDataSimpleList v1 0x11425 "baseline"
    8/15: CBlockData v2 0x11455 "Sample 4"
      1/2: CResultDataSimpleList v1 0x11481 "CO2"
        1/13: CResultDataSimple v2 0x114a3 "rR 45CO2/44CO2    "
        2/13: CResultDataSimple v2 0x11517 "rR 46CO2/44CO2    "
        3/13: CResultDataSimple v2 0x1158b "R 45CO2/44CO2    "
        4/13: CResultDataSimple v2 0x115fb "R 46CO2/44CO2    "
        5/13: CResultDataSimple v2 0x1166b "rd 45CO2/44CO2 [per mil]  vs. Yellowstone"
        6/13: CResultDataSimple v2 0x11735 "rd 46CO2/44CO2 [per mil]  vs. Yellowstone"
        7/13: CResultDataSimple v2 0x117ff "d 45CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        8/13: CResultDataSimple v2 0x118c3 "d 46CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        9/13: CResultDataSimple v2 0x11987 "d 13C/12C [per mil]  vs. VPDB"
        10/13: CResultDataSimple v2 0x11a2d "d 18O/16O [per mil]  vs. VSMOW"
        11/13: CResultDataSimple v2 0x11ad5 "d 17O/16O    vs.  "
        12/13: CResultDataSimple v2 0x11b4b "AT% 13C/12C [%]  "
        13/13: CResultDataSimple v2 0x11bb9 "AT% 18O/16O [%]  "
      2/2: CResultDataSimpleList v1 0x11c2b "baseline"
    9/15: CBlockData v2 0x11c5b "Reference 4"
      1/2: CResultDataSimpleList v1 0x11c8d "CO2"
        1/13: CResultDataSimple v2 0x11caf "rR 45CO2/44CO2    "
        2/13: CResultDataSimple v2 0x11d23 "rR 46CO2/44CO2    "
        3/13: CResultDataSimple v2 0x11d97 "R 45CO2/44CO2    "
        4/13: CResultDataSimple v2 0x11e07 "R 46CO2/44CO2    "
        5/13: CResultDataSimple v2 0x11e77 "rd 45CO2/44CO2 [per mil]  vs. Yellowstone"
        6/13: CResultDataSimple v2 0x11f41 "rd 46CO2/44CO2 [per mil]  vs. Yellowstone"
        7/13: CResultDataSimple v2 0x1200b "d 45CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        8/13: CResultDataSimple v2 0x120cf "d 46CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        9/13: CResultDataSimple v2 0x12193 "d 13C/12C [per mil]  vs. VPDB"
        10/13: CResultDataSimple v2 0x12239 "d 18O/16O [per mil]  vs. VSMOW"
        11/13: CResultDataSimple v2 0x122e1 "d 17O/16O    vs.  "
        12/13: CResultDataSimple v2 0x12359 "AT% 13C/12C [%]  "
        13/13: CResultDataSimple v2 0x123c7 "AT% 18O/16O [%]  "
      2/2: CResultDataSimpleList v1 0x12439 "baseline"
    10/15: CBlockData v2 0x12469 "Sample 5"
      1/2: CResultDataSimpleList v1 0x12495 "CO2"
        1/13: CResultDataSimple v2 0x124b7 "rR 45CO2/44CO2    "
        2/13: CResultDataSimple v2 0x1252b "rR 46CO2/44CO2    "
        3/13: CResultDataSimple v2 0x1259f "R 45CO2/44CO2    "
        4/13: CResultDataSimple v2 0x1260f "R 46CO2/44CO2    "
        5/13: CResultDataSimple v2 0x1267f "rd 45CO2/44CO2 [per mil]  vs. Yellowstone"
        6/13: CResultDataSimple v2 0x1274b "rd 46CO2/44CO2 [per mil]  vs. Yellowstone"
        7/13: CResultDataSimple v2 0x12817 "d 45CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        8/13: CResultDataSimple v2 0x128db "d 46CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        9/13: CResultDataSimple v2 0x1299f "d 13C/12C [per mil]  vs. VPDB"
        10/13: CResultDataSimple v2 0x12a45 "d 18O/16O [per mil]  vs. VSMOW"
        11/13: CResultDataSimple v2 0x12aed "d 17O/16O    vs.  "
        12/13: CResultDataSimple v2 0x12b65 "AT% 13C/12C [%]  "
        13/13: CResultDataSimple v2 0x12bd3 "AT% 18O/16O [%]  "
      2/2: CResultDataSimpleList v1 0x12c45 "baseline"
    11/15: CBlockData v2 0x12c75 "Reference 5"
      1/2: CResultDataSimpleList v1 0x12ca7 "CO2"
        1/13: CResultDataSimple v2 0x12cc9 "rR 45CO2/44CO2    "
        2/13: CResultDataSimple v2 0x12d3d "rR 46CO2/44CO2    "
        3/13: CResultDataSimple v2 0x12db1 "R 45CO2/44CO2    "
        4/13: CResultDataSimple v2 0x12e21 "R 46CO2/44CO2    "
        5/13: CResultDataSimple v2 0x12e91 "rd 45CO2/44CO2 [per mil]  vs. Yellowstone"
        6/13: CResultDataSimple v2 0x12f5b "rd 46CO2/44CO2 [per mil]  vs. Yellowstone"
        7/13: CResultDataSimple v2 0x13025 "d 45CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        8/13: CResultDataSimple v2 0x130e9 "d 46CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        9/13: CResultDataSimple v2 0x131ad "d 13C/12C [per mil]  vs. VPDB"
        10/13: CResultDataSimple v2 0x13253 "d 18O/16O [per mil]  vs. VSMOW"
        11/13: CResultDataSimple v2 0x132fb "d 17O/16O    vs.  "
        12/13: CResultDataSimple v2 0x13373 "AT% 13C/12C [%]  "
        13/13: CResultDataSimple v2 0x133e1 "AT% 18O/16O [%]  "
      2/2: CResultDataSimpleList v1 0x13453 "baseline"
    12/15: CBlockData v2 0x13483 "Sample 6"
      1/2: CResultDataSimpleList v1 0x134af "CO2"
        1/13: CResultDataSimple v2 0x134d1 "rR 45CO2/44CO2    "
        2/13: CResultDataSimple v2 0x13545 "rR 46CO2/44CO2    "
        3/13: CResultDataSimple v2 0x135b9 "R 45CO2/44CO2    "
        4/13: CResultDataSimple v2 0x13629 "R 46CO2/44CO2    "
        5/13: CResultDataSimple v2 0x13699 "rd 45CO2/44CO2 [per mil]  vs. Yellowstone"
        6/13: CResultDataSimple v2 0x13763 "rd 46CO2/44CO2 [per mil]  vs. Yellowstone"
        7/13: CResultDataSimple v2 0x1382f "d 45CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        8/13: CResultDataSimple v2 0x138f3 "d 46CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        9/13: CResultDataSimple v2 0x139b7 "d 13C/12C [per mil]  vs. VPDB"
        10/13: CResultDataSimple v2 0x13a5d "d 18O/16O [per mil]  vs. VSMOW"
        11/13: CResultDataSimple v2 0x13b05 "d 17O/16O    vs.  "
        12/13: CResultDataSimple v2 0x13b7d "AT% 13C/12C [%]  "
        13/13: CResultDataSimple v2 0x13beb "AT% 18O/16O [%]  "
      2/2: CResultDataSimpleList v1 0x13c5d "baseline"
    13/15: CBlockData v2 0x13c8d "Reference 6"
      1/2: CResultDataSimpleList v1 0x13cbf "CO2"
        1/13: CResultDataSimple v2 0x13ce1 "rR 45CO2/44CO2    "
        2/13: CResultDataSimple v2 0x13d55 "rR 46CO2/44CO2    "
        3/13: CResultDataSimple v2 0x13dc9 "R 45CO2/44CO2    "
        4/13: CResultDataSimple v2 0x13e39 "R 46CO2/44CO2    "
        5/13: CResultDataSimple v2 0x13ea9 "rd 45CO2/44CO2 [per mil]  vs. Yellowstone"
        6/13: CResultDataSimple v2 0x13f73 "rd 46CO2/44CO2 [per mil]  vs. Yellowstone"
        7/13: CResultDataSimple v2 0x1403d "d 45CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        8/13: CResultDataSimple v2 0x14101 "d 46CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        9/13: CResultDataSimple v2 0x141c5 "d 13C/12C [per mil]  vs. VPDB"
        10/13: CResultDataSimple v2 0x1426b "d 18O/16O [per mil]  vs. VSMOW"
        11/13: CResultDataSimple v2 0x14313 "d 17O/16O    vs.  "
        12/13: CResultDataSimple v2 0x1438b "AT% 13C/12C [%]  "
        13/13: CResultDataSimple v2 0x143f9 "AT% 18O/16O [%]  "
      2/2: CResultDataSimpleList v1 0x1446b "baseline"
    14/15: CBlockData v2 0x1449b "Sample 7"
      1/2: CResultDataSimpleList v1 0x144c7 "CO2"
        1/13: CResultDataSimple v2 0x144e9 "rR 45CO2/44CO2    "
        2/13: CResultDataSimple v2 0x1455d "rR 46CO2/44CO2    "
        3/13: CResultDataSimple v2 0x145d1 "R 45CO2/44CO2    "
        4/13: CResultDataSimple v2 0x14641 "R 46CO2/44CO2    "
        5/13: CResultDataSimple v2 0x146b1 "rd 45CO2/44CO2 [per mil]  vs. Yellowstone"
        6/13: CResultDataSimple v2 0x1477b "rd 46CO2/44CO2 [per mil]  vs. Yellowstone"
        7/13: CResultDataSimple v2 0x14847 "d 45CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        8/13: CResultDataSimple v2 0x1490b "d 46CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        9/13: CResultDataSimple v2 0x149cf "d 13C/12C [per mil]  vs. VPDB"
        10/13: CResultDataSimple v2 0x14a75 "d 18O/16O [per mil]  vs. VSMOW"
        11/13: CResultDataSimple v2 0x14b1d "d 17O/16O    vs.  "
        12/13: CResultDataSimple v2 0x14b95 "AT% 13C/12C [%]  "
        13/13: CResultDataSimple v2 0x14c03 "AT% 18O/16O [%]  "
      2/2: CResultDataSimpleList v1 0x14c75 "baseline"
    15/15: CBlockData v2 0x14ca5 "Reference 7"
      1/2: CResultDataSimpleList v1 0x14cd7 "CO2"
        1/13: CResultDataSimple v2 0x14cf9 "rR 45CO2/44CO2    "
        2/13: CResultDataSimple v2 0x14d6d "rR 46CO2/44CO2    "
        3/13: CResultDataSimple v2 0x14de1 "R 45CO2/44CO2    "
        4/13: CResultDataSimple v2 0x14e51 "R 46CO2/44CO2    "
        5/13: CResultDataSimple v2 0x14ec1 "rd 45CO2/44CO2 [per mil]  vs. Yellowstone"
        6/13: CResultDataSimple v2 0x14f8b "rd 46CO2/44CO2 [per mil]  vs. Yellowstone"
        7/13: CResultDataSimple v2 0x15055 "d 45CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        8/13: CResultDataSimple v2 0x15119 "d 46CO2/44CO2 [per mil]  vs. VPDB VSMOW"
        9/13: CResultDataSimple v2 0x151dd "d 13C/12C [per mil]  vs. VPDB"
        10/13: CResultDataSimple v2 0x15283 "d 18O/16O [per mil]  vs. VSMOW"
        11/13: CResultDataSimple v2 0x1532b "d 17O/16O    vs.  "
        12/13: CResultDataSimple v2 0x153a3 "AT% 13C/12C [%]  "
        13/13: CResultDataSimple v2 0x15411 "AT% 18O/16O [%]  "
      2/2: CResultDataSimpleList v1 0x15483 "baseline"
  7/12: CBlockData v2 0x154b3 "Evaluated Results"
    1/2: CDualInletEvaluatedDataCollect v1 0x15513 "CO2"
      1/7: CDualInletEvaluatedData v2 0x15557 "d 45CO2/44CO2 "
        1/1: CTwoDoublesArrayData v1 0x155b4 "Delta"
      2/7: CDualInletEvaluatedData v2 0x1568b "d 46CO2/44CO2 "
        1/1: CTwoDoublesArrayData v1 0x156cd "Delta"
      3/7: CDualInletEvaluatedData v2 0x157a4 "d 13C/12C "
        1/1: CTwoDoublesArrayData v1 0x157de "Delta"
      4/7: CDualInletEvaluatedData v2 0x158b5 "d 18O/16O "
        1/1: CTwoDoublesArrayData v1 0x158ef "Delta"
      5/7: CDualInletEvaluatedData v2 0x159c6 "d 17O/16O "
        1/1: CTwoDoublesArrayData v1 0x15a00 "Delta"
      6/7: CDualInletEvaluatedData v2 0x15ad7 "AT% 13C/12C "
        1/1: CTwoDoublesArrayData v1 0x15b11 "AT%"
      7/7: CDualInletEvaluatedData v2 0x15be4 "AT% 18O/16O "
        1/1: CTwoDoublesArrayData v1 0x15c1e "AT%"
    2/2: CDualInletEvaluatedDataCollect v1 0x15cf5 "baseline"
  8/12: CBlockData v2 0x15d25 "Gas Indices"
    1/2: CStatusArrayData v1 0x15d6d "CO2"
    2/2: CStatusArrayData v1 0x15d93 "baseline"
  9/12: CMethod v10 0x15dbe "Method"
    1/7: CMolecule v1 0x15df1 "Eval@Molecule"
    2/7: CPartMirror v0 0x15e54
    3/7: CBlockData v2 0x15e65 "Correction Descriptors"
    4/7: CMethodPrintoutDesc v2 0x15ed9
    5/7: CBlockData v2 0x15f76 "External Dynamic Variables"
      1/19: CScrHeadLine v1 0x15ffa "g_strHeadline"
        CDynExternal v4 0x16088
      2/19: CScrHeadLine v1 0x16162 "g_strInfo"
        CDynExternal v4 0x161c8
      3/19: CScrBool v1 0x162b4 "g_bRelativeTo1"
        CDynExternal v4 0x1633e
      4/19: CNumericValue v0 0x163ee
        CScrHeadLine v1 0x1640d "g_strRelInfo"
          CDynExternal v4 0x16485
      5/19: CScrNumber v1 0x16567 "g_nInterMass1"
        CDynExternal v4 0x165e5
      6/19: CNumericValue v0 0x16689
        CScrChannel v1 0x16697 "g_nInterChan1"
          CDynExternal v4 0x16718
      7/19: CNumericValue v0 0x167be
        CScrNumber v1 0x167cc "g_nInterDelay1"
          CDynExternal v4 0x16846
      8/19: CNumericValue v0 0x168f2
        CScrNumber v1 0x16900 "g_nInterMass2"
          CDynExternal v4 0x16970
      9/19: CNumericValue v0 0x16a14
        CScrChannel v1 0x16a22 "g_nInterChan2"
          CDynExternal v4 0x16a94
      10/19: CNumericValue v0 0x16b3a
        CScrNumber v1 0x16b48 "g_nInterDelay2"
          CDynExternal v4 0x16bc2
      11/19: CNumericValue v0 0x16c6e
        CScrNumber v1 0x16c7c "g_nInterMass3"
          CDynExternal v4 0x16cec
      12/19: CNumericValue v0 0x16d90
        CScrChannel v1 0x16d9e "g_nInterChan3"
          CDynExternal v4 0x16e10
      13/19: CNumericValue v0 0x16eb6
        CScrNumber v1 0x16ec4 "g_nInterDelay3"
          CDynExternal v4 0x16f3e
      14/19: CNumericValue v0 0x16fea
        CScrNumber v1 0x16ff8 "g_nInterMass4"
          CDynExternal v4 0x17068
      15/19: CNumericValue v0 0x1710c
        CScrChannel v1 0x1711a "g_nInterChan4"
          CDynExternal v4 0x1718c
      16/19: CNumericValue v0 0x17232
        CScrNumber v1 0x17240 "g_nInterDelay4"
          CDynExternal v4 0x172ba
      17/19: CNumericValue v0 0x17366
        CScrNumber v1 0x17374 "g_nInterMass5"
          CDynExternal v4 0x173e4
      18/19: CNumericValue v0 0x17488
        CScrChannel v1 0x17496 "g_nInterChan5"
          CDynExternal v4 0x17508
      19/19: CNumericValue v0 0x175ae
        CScrNumber v1 0x175bc "g_nInterDelay5"
          CDynExternal v4 0x17636
    6/7: CNumericValue v0 0x176e2
      CEvalIntegrationUnitHWInfoStore v1 0x176f0
        1/1: CEvalIntegrationUnitHWInfoList v1 0x1772f
          1-7/7: CEvalIntegrationUnitHWInfo v1 0x1776d
    7/7: CGasConfiguration v3 0x178e3 "CO2"
      1/30: CBasicScan v4 0x1791a "Peak Center"
        CScaleHvScanPart v2 0x1796c "Isotope MS/Integration Unit@Isotope MS/ScaleHv@"
          CScaleHvHardwarePart v3 0x179f6 "High Voltage"
            CFinniganInterface v6 0x17a62 "Delta"
            CVisualisationData v8 0x17aca
        CIntegrationUnitScanPart v3 0x17c4e
          CIntegrationUnitHardwarePart v3 0x17c82 "Integration Unit"
            CGpibInterface v3 0x17d10
            CIntegrationUnitGasConfPart v2 0x17d41 "IntegrationUnit"
              1-3/3: CChannelGasConfPart v4 0x17dae
            CVisualisationData v8 0x17e71
            1/8: CCupHardwarePart v5 0x17fc6 "Cup 1"
              CBasicInterface v2 0x18006
              CVisualisationData v8 0x18049
            2/8: CCupHardwarePart v5 0x18122 "Cup 2"
              CBasicInterface v2 0x1814e
              CVisualisationData v8 0x1817e
            3/8: CCupHardwarePart v5 0x18257 "Cup 3"
              CBasicInterface v2 0x18283
              CVisualisationData v8 0x182b3
            4/8: CCupHardwarePart v5 0x1838c "Cup 4"
              CBasicInterface v2 0x183b8
              CVisualisationData v8 0x183e8
            5/8: CCupHardwarePart v5 0x184c1 "Cup 5"
              CBasicInterface v2 0x184ed
              CVisualisationData v8 0x1851d
            6/8: CCupHardwarePart v5 0x185f6 "Cup 6"
              CBasicInterface v2 0x18622
              CVisualisationData v8 0x18652
            7/8: CCupHardwarePart v5 0x1872b "Cup 7"
              CBasicInterface v2 0x18757
              CVisualisationData v8 0x18787
            8/8: CCupHardwarePart v5 0x18860 "Cup 8"
              CBasicInterface v2 0x1888c
              CVisualisationData v8 0x188bc
            1/3: CChannelHardwarePart v2 0x18996 "Channel 1"
              CBasicInterface v2 0x189ea
              CVisualisationData v8 0x18a1a
            2/3: CChannelHardwarePart v2 0x18ae2 "Channel 2"
              CBasicInterface v2 0x18b1e
              CVisualisationData v8 0x18b4e
            3/3: CChannelHardwarePart v2 0x18c16 "Channel 3"
              CBasicInterface v2 0x18c52
              CVisualisationData v8 0x18c82
        CBlockData v2 0x18d67
      2/30: CIntegrationUnitGasConfPart v2 0x18d8f "IntegrationUnit"
        1-7/7: CChannelGasConfPart v4 0x18dfc
      3/30: CDioTransferPart v2 0x18f8b "Resitor Channel 1"
      4/30: CDioTransferPart v2 0x19015 "Resitor Channel 2"
      5/30: CDioTransferPart v2 0x1908b "Resitor Channel 3"
      6/30: CDioTransferPart v2 0x19101 "Resitor Channel 4"
      7/30: CDioTransferPart v2 0x19177 "Resitor Channel 5"
      8/30: CDioTransferPart v2 0x191ed "Resitor Channel 6"
      9/30: CDioTransferPart v2 0x19263 "Resitor Channel 7"
      10/30: CDioTransferPart v2 0x192d9 "Resitor Channel 8"
      11/30: CDioTransferPart v2 0x1934f "Resitor Channel 9"
      12/30: CDioTransferPart v2 0x193c5 "Resitor Channel 10"
      13/30: CPeakCenterOffset v1 0x1943d
      14/30: CMagnetCurrentTransferPart v3 0x19484 "MagnetCurrent"
      15/30: CCalibration v5 0x19518 "CO2_cal_03022016"
        1/3: CCalibrationPoint v3 0x19584 "0. Point"
        2/3: CCalibrationPoint v3 0x195f1 "1. Point"
        3/3: CCalibrationPoint v3 0x19649 "2. Point"
      16-17/30: CMolecule v1 0x1b0f5
      18/30: CScaleHvTransferPart v2 0x1b143 "Isotope MS/ScaleHv"
      19/30: CCalculatingDacTransferPart v1 0x1b1cb "Trap"
      20/30: CCalculatingDacTransferPart v1 0x1b238 "Electron Energy"
      21/30: CCalculatingDacTransferPart v1 0x1b2b2 "Emission"
      22/30: CCalculatingDacTransferPart v1 0x1b310 "Extraction"
      23/30: CCalculatingDacTransferPart v1 0x1b376 "Shield"
      24/30: CCalculatingDacTransferPart v1 0x1b3cc "R-Plate"
      25/30: CCalculatingDacTransferPart v1 0x1b426 "Einzel-Lens"
      26/30: CCalculatingDacTransferPart v1 0x1b490 "Einzel-Lens Symmetry"
      27/30: CCalculatingDacTransferPart v1 0x1b51e "X-Focus"
      28/30: CCalculatingDacTransferPart v1 0x1b578 "X-Focus Symmetry"
      29/30: CCalculatingDacTransferPart v1 0x1b5f6 "Y-Deflection"
      30/30: CCalculatingDacTransferPart v1 0x1b664 "Y-Deflection Symmetry"
    CConfiguration v7 0x1b702 "Dual Inlet"
      1/3: CMsDevice v2 0x1b758 "MAT 253"
        1/2: CActivePort v2 0x1b7a3 "Source"
          1/1: CDualInletDevice v1 0x1b7e6 "Dual Inlet System"
            1/7: CStr v2 0x1b85a "Dual Inlet System"
            2/7: CActivePort v2 0x1b8ae "COV Connection"
              1/1: CChangeOver2Device v1 0x1b902 "Change Over 2"
                1/1: CActivePort v2 0x1b968 "COV Ext"
            3/7: CActivePort v2 0x1b9da "Direct COV"
            4/7: CActivePort v2 0x1ba26 "Intern Right"
              1/1: CReferenceRefillDevice v1 0x1ba72 "Reference Refill"
            5/7: CActivePort v2 0x1bb24 "Intern Left"
            6/7: CActivePort v2 0x1bb84 "Extern Right"
            7/7: CActivePort v2 0x1bbe8 "Extern Left"
        2/2: CPort v2 0x1bc74 "Capillary"
      2/3: CVisualisationDialogNamesBlockData v1 0x1bce9 "Visualisation Dialogs"
        1/4: CData v3 0x1bd55 "MS"
        2/4: CData v3 0x1bd71 "MS State"
        3/4: CData v3 0x1bda5 "Focus 253"
        4/4: CData v3 0x1bddd "Dual Inlet"
      3/3: CBlockData v2 0x1be1d "Sequence Commands"
    1/4: CMsDeviceMethodPart v3 0x1bf01
      CBlockData v2 0x1bf4c
      CActionPeakCenter v1 0x1bf71
    2/4: CDualInletDeviceMethodPart v5 0x1bfbc
      CBlockData v2 0x1c010
      CActionPressAdjust v3 0x1c034
      CActionBackground v4 0x1c097
    3/4: CActiveDeviceMethodPart v1 0x1c11d
      CBlockData v2 0x1c16c
    4/4: CReferenceRefillDeviceMethodPart v1 0x1c188
      CBlockData v2 0x1c1e0
    1/2: CMsDeviceEvaluationPart v2 0x1c218
      CBlockData v2 0x1c253
      1/4: COutlierTestMethodPart v2 0x1c26f
        CBlockData v2 0x1c2bd
          1/2: COutlierTestSigma v1 0x1c2d9
          2/2: COutlierTest v1 0x1c2fc
      2/4: CExtEvaluation v5 0x1c31e
      3/4: CICA_BasicMethodPart v12 0x1c3a8 "CO2 Ion Correction Method"
        CEvalDataItemListTransferPart v1 0x1c426
        CGasConfiguration v3 0x1c483 "CO2"
          1/30: CBasicScan v4 0x1c4a5 "Peak Center"
            CScaleHvScanPart v2 0x1c4e9 "Isotope MS/Integration Unit@Isotope MS/ScaleHv@"
              CScaleHvHardwarePart v3 0x1c55f "High Voltage"
                CFinniganInterface v6 0x1c5b3 "Delta"
                CVisualisationData v8 0x1c605
            CIntegrationUnitScanPart v3 0x1c773
              CIntegrationUnitHardwarePart v3 0x1c78b "Integration Unit"
                CGpibInterface v3 0x1c7f9
                CIntegrationUnitGasConfPart v2 0x1c818 "IntegrationUnit"
                  1-3/3: CChannelGasConfPart v4 0x1c885
                CVisualisationData v8 0x1c948
                1/8: CCupHardwarePart v5 0x1ca9d "Cup 1"
                  CBasicInterface v2 0x1cac9
                  CVisualisationData v8 0x1caf9
                2/8: CCupHardwarePart v5 0x1cbd2 "Cup 2"
                  CBasicInterface v2 0x1cbfe
                  CVisualisationData v8 0x1cc2e
                3/8: CCupHardwarePart v5 0x1cd07 "Cup 3"
                  CBasicInterface v2 0x1cd33
                  CVisualisationData v8 0x1cd63
                4/8: CCupHardwarePart v5 0x1ce3c "Cup 4"
                  CBasicInterface v2 0x1ce68
                  CVisualisationData v8 0x1ce98
                5/8: CCupHardwarePart v5 0x1cf71 "Cup 5"
                  CBasicInterface v2 0x1cf9d
                  CVisualisationData v8 0x1cfcd
                6/8: CCupHardwarePart v5 0x1d0a6 "Cup 6"
                  CBasicInterface v2 0x1d0d2
                  CVisualisationData v8 0x1d102
                7/8: CCupHardwarePart v5 0x1d1db "Cup 7"
                  CBasicInterface v2 0x1d207
                  CVisualisationData v8 0x1d237
                8/8: CCupHardwarePart v5 0x1d310 "Cup 8"
                  CBasicInterface v2 0x1d33c
                  CVisualisationData v8 0x1d36c
                1/3: CChannelHardwarePart v2 0x1d446 "Channel 1"
                  CBasicInterface v2 0x1d482
                  CVisualisationData v8 0x1d4b2
                2/3: CChannelHardwarePart v2 0x1d57a "Channel 2"
                  CBasicInterface v2 0x1d5b6
                  CVisualisationData v8 0x1d5e6
                3/3: CChannelHardwarePart v2 0x1d6ae "Channel 3"
                  CBasicInterface v2 0x1d6ea
                  CVisualisationData v8 0x1d71a
            CBlockData v2 0x1d7ff
          2/30: CIntegrationUnitGasConfPart v2 0x1d827 "IntegrationUnit"
            1-7/7: CChannelGasConfPart v4 0x1d894
          3/30: CDioTransferPart v2 0x1da23 "Resitor Channel 1"
          4/30: CDioTransferPart v2 0x1da99 "Resitor Channel 2"
          5/30: CDioTransferPart v2 0x1db0f "Resitor Channel 3"
          6/30: CDioTransferPart v2 0x1db85 "Resitor Channel 4"
          7/30: CDioTransferPart v2 0x1dbfb "Resitor Channel 5"
          8/30: CDioTransferPart v2 0x1dc71 "Resitor Channel 6"
          9/30: CDioTransferPart v2 0x1dce7 "Resitor Channel 7"
          10/30: CDioTransferPart v2 0x1dd5d "Resitor Channel 8"
          11/30: CDioTransferPart v2 0x1ddd3 "Resitor Channel 9"
          12/30: CDioTransferPart v2 0x1de49 "Resitor Channel 10"
          13/30: CPeakCenterOffset v1 0x1dec1
          14/30: CMagnetCurrentTransferPart v3 0x1def3 "MagnetCurrent"
          15/30: CCalibration v5 0x1df69 "CO2_cal_03022016"
            1/3: CCalibrationPoint v3 0x1dfc5 "0. Point"
            2/3: CCalibrationPoint v3 0x1e01d "1. Point"
            3/3: CCalibrationPoint v3 0x1e075 "2. Point"
          16-17/30: CMolecule v1 0x1fb21
          18/30: CScaleHvTransferPart v2 0x1fb6f "Isotope MS/ScaleHv"
          19/30: CCalculatingDacTransferPart v1 0x1fbdf "Trap"
          20/30: CCalculatingDacTransferPart v1 0x1fc2d "Electron Energy"
          21/30: CCalculatingDacTransferPart v1 0x1fca7 "Emission"
          22/30: CCalculatingDacTransferPart v1 0x1fd05 "Extraction"
          23/30: CCalculatingDacTransferPart v1 0x1fd6b "Shield"
          24/30: CCalculatingDacTransferPart v1 0x1fdc1 "R-Plate"
          25/30: CCalculatingDacTransferPart v1 0x1fe1b "Einzel-Lens"
          26/30: CCalculatingDacTransferPart v1 0x1fe85 "Einzel-Lens Symmetry"
          27/30: CCalculatingDacTransferPart v1 0x1ff13 "X-Focus"
          28/30: CCalculatingDacTransferPart v1 0x1ff6d "X-Focus Symmetry"
          29/30: CCalculatingDacTransferPart v1 0x1ffeb "Y-Deflection"
          30/30: CCalculatingDacTransferPart v1 0x20059 "Y-Deflection Symmetry"
        CBlockData v2 0x200f7
          1/2: CPrimaryStandardMethodPart v2 0x20113 "VSMOW"
            CEvalDataItemListTransferPart v1 0x2017d
              1-3/3: CEvalDataDoubleTransferPart v1 0x20199
          2/2: CPrimaryStandardMethodPart v2 0x20398 "VPDB"
            CEvalDataItemListTransferPart v1 0x203e0
              1-3/3: CEvalDataDoubleTransferPart v1 0x203fc
        CParsedEvaluationStringArray v1 0x20606 "CO2"
          1-2/2: CParsedEvaluationString v1 0x2064e
      4/4: CDualInletStandardizationMethodPart v11 0x20735 "CO2 Standard Method"
        CSecondaryStandardMethodPart v3 0x207c2 "Yellowstone"
          CEvalDataItemListTransferPart v1 0x20854
            1-2/2: CEvalDataSecStdTransferPart v2 0x20870
          CBlockData v2 0x20a55
            1/2: CPrimaryStandardMethodPart v2 0x20a71 "VPDB"
              CEvalDataItemListTransferPart v1 0x20ab9
                1-3/3: CEvalDataDoubleTransferPart v1 0x20ad5
            2/2: CPrimaryStandardMethodPart v2 0x20cb5 "VSMOW"
              CEvalDataItemListTransferPart v1 0x20d01
                1-3/3: CEvalDataDoubleTransferPart v1 0x20d1d
        CGasConfiguration v3 0x20efd "CO2"
          1/30: CBasicScan v4 0x20f1f "Peak Center"
            CScaleHvScanPart v2 0x20f63 "Isotope MS/Integration Unit@Isotope MS/ScaleHv@"
              CScaleHvHardwarePart v3 0x20fd9 "High Voltage"
                CFinniganInterface v6 0x2102d "Delta"
                CVisualisationData v8 0x2107f
            CIntegrationUnitScanPart v3 0x211ed
              CIntegrationUnitHardwarePart v3 0x21205 "Integration Unit"
                CGpibInterface v3 0x21273
                CIntegrationUnitGasConfPart v2 0x21292 "IntegrationUnit"
                  1-3/3: CChannelGasConfPart v4 0x212ff
                CVisualisationData v8 0x213c2
                1/8: CCupHardwarePart v5 0x21517 "Cup 1"
                  CBasicInterface v2 0x21543
                  CVisualisationData v8 0x21573
                2/8: CCupHardwarePart v5 0x2164c "Cup 2"
                  CBasicInterface v2 0x21678
                  CVisualisationData v8 0x216a8
                3/8: CCupHardwarePart v5 0x21781 "Cup 3"
                  CBasicInterface v2 0x217ad
                  CVisualisationData v8 0x217dd
                4/8: CCupHardwarePart v5 0x218b6 "Cup 4"
                  CBasicInterface v2 0x218e2
                  CVisualisationData v8 0x21912
                5/8: CCupHardwarePart v5 0x219eb "Cup 5"
                  CBasicInterface v2 0x21a17
                  CVisualisationData v8 0x21a47
                6/8: CCupHardwarePart v5 0x21b20 "Cup 6"
                  CBasicInterface v2 0x21b4c
                  CVisualisationData v8 0x21b7c
                7/8: CCupHardwarePart v5 0x21c55 "Cup 7"
                  CBasicInterface v2 0x21c81
                  CVisualisationData v8 0x21cb1
                8/8: CCupHardwarePart v5 0x21d8a "Cup 8"
                  CBasicInterface v2 0x21db6
                  CVisualisationData v8 0x21de6
                1/3: CChannelHardwarePart v2 0x21ec0 "Channel 1"
                  CBasicInterface v2 0x21efc
                  CVisualisationData v8 0x21f2c
                2/3: CChannelHardwarePart v2 0x21ff4 "Channel 2"
                  CBasicInterface v2 0x22030
                  CVisualisationData v8 0x22060
                3/3: CChannelHardwarePart v2 0x22128 "Channel 3"
                  CBasicInterface v2 0x22164
                  CVisualisationData v8 0x22194
            CBlockData v2 0x22279
          2/30: CIntegrationUnitGasConfPart v2 0x222a1 "IntegrationUnit"
            1-7/7: CChannelGasConfPart v4 0x2230e
          3/30: CDioTransferPart v2 0x2249d "Resitor Channel 1"
          4/30: CDioTransferPart v2 0x22513 "Resitor Channel 2"
          5/30: CDioTransferPart v2 0x22589 "Resitor Channel 3"
          6/30: CDioTransferPart v2 0x225ff "Resitor Channel 4"
          7/30: CDioTransferPart v2 0x22675 "Resitor Channel 5"
          8/30: CDioTransferPart v2 0x226eb "Resitor Channel 6"
          9/30: CDioTransferPart v2 0x22761 "Resitor Channel 7"
          10/30: CDioTransferPart v2 0x227d7 "Resitor Channel 8"
          11/30: CDioTransferPart v2 0x2284d "Resitor Channel 9"
          12/30: CDioTransferPart v2 0x228c3 "Resitor Channel 10"
          13/30: CPeakCenterOffset v1 0x2293b
          14/30: CMagnetCurrentTransferPart v3 0x2296d "MagnetCurrent"
          15/30: CCalibration v5 0x229e3 "CO2_cal_03022016"
            1/3: CCalibrationPoint v3 0x22a3f "0. Point"
            2/3: CCalibrationPoint v3 0x22a97 "1. Point"
            3/3: CCalibrationPoint v3 0x22aef "2. Point"
          16-17/30: CMolecule v1 0x2459b
          18/30: CScaleHvTransferPart v2 0x245e9 "Isotope MS/ScaleHv"
          19/30: CCalculatingDacTransferPart v1 0x24659 "Trap"
          20/30: CCalculatingDacTransferPart v1 0x246a7 "Electron Energy"
          21/30: CCalculatingDacTransferPart v1 0x24721 "Emission"
          22/30: CCalculatingDacTransferPart v1 0x2477f "Extraction"
          23/30: CCalculatingDacTransferPart v1 0x247e5 "Shield"
          24/30: CCalculatingDacTransferPart v1 0x2483b "R-Plate"
          25/30: CCalculatingDacTransferPart v1 0x24895 "Einzel-Lens"
          26/30: CCalculatingDacTransferPart v1 0x248ff "Einzel-Lens Symmetry"
          27/30: CCalculatingDacTransferPart v1 0x2498d "X-Focus"
          28/30: CCalculatingDacTransferPart v1 0x249e7 "X-Focus Symmetry"
          29/30: CCalculatingDacTransferPart v1 0x24a65 "Y-Deflection"
          30/30: CCalculatingDacTransferPart v1 0x24ad3 "Y-Deflection Symmetry"
    2/2: CDualInletDeviceEvaluationPart v1 0x24ba7
      CBlockData v2 0x24bfd
    1/1: CMethod v10 0x24c29
      1/5: CMolecule v1 0x24c45
      2/5: CPartMirror v0 0x24c71
      3/5: CMethodPrintoutDesc v2 0x24c73
      4/5: CBlockData v2 0x24cf9 "Correction Descriptors"
      5/5: CGasConfiguration v3 0x24d6d "CO2"
        1/30: CBasicScan v4 0x24d8f "Peak Center"
          CScaleHvScanPart v2 0x24dd3 "Isotope MS/Integration Unit@Isotope MS/ScaleHv@"
            CScaleHvHardwarePart v3 0x24e49 "High Voltage"
              CFinniganInterface v6 0x24e9d "Delta"
              CVisualisationData v8 0x24eef
          CIntegrationUnitScanPart v3 0x2505d
            CIntegrationUnitHardwarePart v3 0x25075 "Integration Unit"
              CGpibInterface v3 0x250e3
              CIntegrationUnitGasConfPart v2 0x25102 "IntegrationUnit"
                1-3/3: CChannelGasConfPart v4 0x2516f
              CVisualisationData v8 0x25232
              1/8: CCupHardwarePart v5 0x25387 "Cup 1"
                CBasicInterface v2 0x253b3
                CVisualisationData v8 0x253e3
              2/8: CCupHardwarePart v5 0x254bc "Cup 2"
                CBasicInterface v2 0x254e8
                CVisualisationData v8 0x25518
              3/8: CCupHardwarePart v5 0x255f1 "Cup 3"
                CBasicInterface v2 0x2561d
                CVisualisationData v8 0x2564d
              4/8: CCupHardwarePart v5 0x25726 "Cup 4"
                CBasicInterface v2 0x25752
                CVisualisationData v8 0x25782
              5/8: CCupHardwarePart v5 0x2585b "Cup 5"
                CBasicInterface v2 0x25887
                CVisualisationData v8 0x258b7
              6/8: CCupHardwarePart v5 0x25990 "Cup 6"
                CBasicInterface v2 0x259bc
                CVisualisationData v8 0x259ec
              7/8: CCupHardwarePart v5 0x25ac5 "Cup 7"
                CBasicInterface v2 0x25af1
                CVisualisationData v8 0x25b21
              8/8: CCupHardwarePart v5 0x25bfa "Cup 8"
                CBasicInterface v2 0x25c26
                CVisualisationData v8 0x25c56
              1/3: CChannelHardwarePart v2 0x25d30 "Channel 1"
                CBasicInterface v2 0x25d6c
                CVisualisationData v8 0x25d9c
              2/3: CChannelHardwarePart v2 0x25e64 "Channel 2"
                CBasicInterface v2 0x25ea0
                CVisualisationData v8 0x25ed0
              3/3: CChannelHardwarePart v2 0x25f98 "Channel 3"
                CBasicInterface v2 0x25fd4
                CVisualisationData v8 0x26004
          CBlockData v2 0x260e9
        2/30: CIntegrationUnitGasConfPart v2 0x26111 "IntegrationUnit"
          1-7/7: CChannelGasConfPart v4 0x2617e
        3/30: CDioTransferPart v2 0x2630d "Resitor Channel 1"
        4/30: CDioTransferPart v2 0x26383 "Resitor Channel 2"
        5/30: CDioTransferPart v2 0x263f9 "Resitor Channel 3"
        6/30: CDioTransferPart v2 0x2646f "Resitor Channel 4"
        7/30: CDioTransferPart v2 0x264e5 "Resitor Channel 5"
        8/30: CDioTransferPart v2 0x2655b "Resitor Channel 6"
        9/30: CDioTransferPart v2 0x265d1 "Resitor Channel 7"
        10/30: CDioTransferPart v2 0x26647 "Resitor Channel 8"
        11/30: CDioTransferPart v2 0x266bd "Resitor Channel 9"
        12/30: CDioTransferPart v2 0x26733 "Resitor Channel 10"
        13/30: CPeakCenterOffset v1 0x267ab
        14/30: CMagnetCurrentTransferPart v3 0x267dd "MagnetCurrent"
        15/30: CCalibration v5 0x26853 "CO2_cal_03022016"
          1/3: CCalibrationPoint v3 0x268af "0. Point"
          2/3: CCalibrationPoint v3 0x26907 "1. Point"
          3/3: CCalibrationPoint v3 0x2695f "2. Point"
        16-17/30: CMolecule v1 0x2840b
        18/30: CScaleHvTransferPart v2 0x28459 "Isotope MS/ScaleHv"
        19/30: CCalculatingDacTransferPart v1 0x284c9 "Trap"
        20/30: CCalculatingDacTransferPart v1 0x28517 "Electron Energy"
        21/30: CCalculatingDacTransferPart v1 0x28591 "Emission"
        22/30: CCalculatingDacTransferPart v1 0x285ef "Extraction"
        23/30: CCalculatingDacTransferPart v1 0x28655 "Shield"
        24/30: CCalculatingDacTransferPart v1 0x286ab "R-Plate"
        25/30: CCalculatingDacTransferPart v1 0x28705 "Einzel-Lens"
        26/30: CCalculatingDacTransferPart v1 0x2876f "Einzel-Lens Symmetry"
        27/30: CCalculatingDacTransferPart v1 0x287fd "X-Focus"
        28/30: CCalculatingDacTransferPart v1 0x28857 "X-Focus Symmetry"
        29/30: CCalculatingDacTransferPart v1 0x288d5 "Y-Deflection"
        30/30: CCalculatingDacTransferPart v1 0x28943 "Y-Deflection Symmetry"
      CConfiguration v7 0x289e1 "Dual Inlet"
        1/4: CMsDevice v2 0x28a25 "MAT 253"
          1/2: CActivePort v2 0x28a63 "Source"
            1/1: CDualInletDevice v1 0x28a97 "Dual Inlet System"
              1/7: CStr v2 0x28af7 "Dual Inlet System"
              2/7: CActivePort v2 0x28b4b "COV Connection"
                1/1: CChangeOver2Device v1 0x28b9f "Change Over 2"
                  1/1: CActivePort v2 0x28bef "COV Ext"
              3/7: CActivePort v2 0x28c61 "Direct COV"
              4/7: CActivePort v2 0x28cad "Intern Right"
                1/1: CReferenceRefillDevice v1 0x28cf9 "Reference Refill"
              5/7: CActivePort v2 0x28d91 "Intern Left"
              6/7: CActivePort v2 0x28df1 "Extern Right"
              7/7: CActivePort v2 0x28e55 "Extern Left"
          2/2: CPort v2 0x28ee1 "Capillary"
        2/4: CVisualisationDialogNamesBlockData v1 0x28f4d "Visualisation Dialogs"
          1/4: CData v3 0x28f93 "MS"
          2/4: CData v3 0x28faf "MS State"
          3/4: CData v3 0x28fe3 "Focus 253"
          4/4: CData v3 0x2901b "Dual Inlet"
        3/4: CBlockData v2 0x2905b "Sequence Commands"
        4/4: CInt v2 0x290bb
      1/4: CMsDeviceMethodPart v3 0x2916b
        CBlockData v2 0x2919f
        CActionPeakCenter v1 0x291c4
      2/4: CDualInletDeviceMethodPart v5 0x291fa
        CBlockData v2 0x29230
        CActionPressAdjust v3 0x29254
        CActionBackground v4 0x292a1
      3/4: CActiveDeviceMethodPart v1 0x29312
        CBlockData v2 0x29346
      4/4: CReferenceRefillDeviceMethodPart v1 0x29362
        CBlockData v2 0x29396
      1/2: CMsDeviceEvaluationPart v2 0x293ce
        CBlockData v2 0x293ee
        1/4: COutlierTestMethodPart v2 0x2940a
          CBlockData v2 0x2943e
            1/2: COutlierTestSigma v1 0x2945a
            2/2: COutlierTest v1 0x29468
        2/4: CExtEvaluation v5 0x2947a
        3/4: CICA_BasicMethodPart v12 0x294f2 "Fe Ion Correction Method"
          CEvalDataItemListTransferPart v1 0x29556
          CGasConfiguration v3 0x29582 "CO2"
            1/30: CBasicScan v4 0x295a4 "Peak Center"
              CScaleHvScanPart v2 0x295e8 "Isotope MS/Integration Unit@Isotope MS/ScaleHv@"
                CScaleHvHardwarePart v3 0x2965e "High Voltage"
                  CFinniganInterface v6 0x296b2 "Delta"
                  CVisualisationData v8 0x29704
              CIntegrationUnitScanPart v3 0x29872
                CIntegrationUnitHardwarePart v3 0x2988a "Integration Unit"
                  CGpibInterface v3 0x298f8
                  CIntegrationUnitGasConfPart v2 0x29917 "IntegrationUnit"
                    1-3/3: CChannelGasConfPart v4 0x29984
                  CVisualisationData v8 0x29a47
                  1/8: CCupHardwarePart v5 0x29b9c "Cup 1"
                    CBasicInterface v2 0x29bc8
                    CVisualisationData v8 0x29bf8
                  2/8: CCupHardwarePart v5 0x29cd1 "Cup 2"
                    CBasicInterface v2 0x29cfd
                    CVisualisationData v8 0x29d2d
                  3/8: CCupHardwarePart v5 0x29e06 "Cup 3"
                    CBasicInterface v2 0x29e32
                    CVisualisationData v8 0x29e62
                  4/8: CCupHardwarePart v5 0x29f3b "Cup 4"
                    CBasicInterface v2 0x29f67
                    CVisualisationData v8 0x29f97
                  5/8: CCupHardwarePart v5 0x2a070 "Cup 5"
                    CBasicInterface v2 0x2a09c
                    CVisualisationData v8 0x2a0cc
                  6/8: CCupHardwarePart v5 0x2a1a5 "Cup 6"
                    CBasicInterface v2 0x2a1d1
                    CVisualisationData v8 0x2a201
                  7/8: CCupHardwarePart v5 0x2a2da "Cup 7"
                    CBasicInterface v2 0x2a306
                    CVisualisationData v8 0x2a336
                  8/8: CCupHardwarePart v5 0x2a40f "Cup 8"
                    CBasicInterface v2 0x2a43b
                    CVisualisationData v8 0x2a46b
                  1/3: CChannelHardwarePart v2 0x2a545 "Channel 1"
                    CBasicInterface v2 0x2a581
                    CVisualisationData v8 0x2a5b1
                  2/3: CChannelHardwarePart v2 0x2a679 "Channel 2"
                    CBasicInterface v2 0x2a6b5
                    CVisualisationData v8 0x2a6e5
                  3/3: CChannelHardwarePart v2 0x2a7ad "Channel 3"
                    CBasicInterface v2 0x2a7e9
                    CVisualisationData v8 0x2a819
              CBlockData v2 0x2a8fe
            2/30: CIntegrationUnitGasConfPart v2 0x2a926 "IntegrationUnit"
              1-7/7: CChannelGasConfPart v4 0x2a993
            3/30: CDioTransferPart v2 0x2ab22 "Resitor Channel 1"
            4/30: CDioTransferPart v2 0x2ab98 "Resitor Channel 2"
            5/30: CDioTransferPart v2 0x2ac0e "Resitor Channel 3"
            6/30: CDioTransferPart v2 0x2ac84 "Resitor Channel 4"
            7/30: CDioTransferPart v2 0x2acfa "Resitor Channel 5"
            8/30: CDioTransferPart v2 0x2ad70 "Resitor Channel 6"
            9/30: CDioTransferPart v2 0x2ade6 "Resitor Channel 7"
            10/30: CDioTransferPart v2 0x2ae5c "Resitor Channel 8"
            11/30: CDioTransferPart v2 0x2aed2 "Resitor Channel 9"
            12/30: CDioTransferPart v2 0x2af48 "Resitor Channel 10"
            13/30: CPeakCenterOffset v1 0x2afc0
            14/30: CMagnetCurrentTransferPart v3 0x2aff2 "MagnetCurrent"
            15/30: CCalibration v5 0x2b068 "CO2_cal_03022016"
              1/3: CCalibrationPoint v3 0x2b0c4 "0. Point"
              2/3: CCalibrationPoint v3 0x2b11c "1. Point"
              3/3: CCalibrationPoint v3 0x2b174 "2. Point"
            16-17/30: CMolecule v1 0x2cc20
            18/30: CScaleHvTransferPart v2 0x2cc6e "Isotope MS/ScaleHv"
            19/30: CCalculatingDacTransferPart v1 0x2ccde "Trap"
            20/30: CCalculatingDacTransferPart v1 0x2cd2c "Electron Energy"
            21/30: CCalculatingDacTransferPart v1 0x2cda6 "Emission"
            22/30: CCalculatingDacTransferPart v1 0x2ce04 "Extraction"
            23/30: CCalculatingDacTransferPart v1 0x2ce6a "Shield"
            24/30: CCalculatingDacTransferPart v1 0x2cec0 "R-Plate"
            25/30: CCalculatingDacTransferPart v1 0x2cf1a "Einzel-Lens"
            26/30: CCalculatingDacTransferPart v1 0x2cf84 "Einzel-Lens Symmetry"
            27/30: CCalculatingDacTransferPart v1 0x2d012 "X-Focus"
            28/30: CCalculatingDacTransferPart v1 0x2d06c "X-Focus Symmetry"
            29/30: CCalculatingDacTransferPart v1 0x2d0ea "Y-Deflection"
            30/30: CCalculatingDacTransferPart v1 0x2d158 "Y-Deflection Symmetry"
          CBlockData v2 0x2d1f6 "Dual Inlet Primary Standards"
            1/1: CPrimaryStandardMethodPart v2 0x2d282 "None"
              CEvalDataItemListTransferPart v1 0x2d2ca
          CParsedEvaluationStringArray v1 0x2d31e "baseline"
            1/1: CParsedEvaluationString v1 0x2d35a
        4/4: CDualInletStandardizationMethodPart v11 0x2d3e2 "Fe Standard Method"
          CSecondaryStandardMethodPart v3 0x2d446 "Fe"
            CEvalDataItemListTransferPart v1 0x2d492
              1/1: CEvalDataSecStdTransferPart v2 0x2d4ae
            CBlockData v2 0x2d598
              1/1: CPrimaryStandardMethodPart v2 0x2d5b4 "None"
                CEvalDataItemListTransferPart v1 0x2d5fc
          CGasConfiguration v3 0x2d628 "CO2"
            1/30: CBasicScan v4 0x2d64a "Peak Center"
              CScaleHvScanPart v2 0x2d68e "Isotope MS/Integration Unit@Isotope MS/ScaleHv@"
                CScaleHvHardwarePart v3 0x2d704 "High Voltage"
                  CFinniganInterface v6 0x2d758 "Delta"
                  CVisualisationData v8 0x2d7aa
              CIntegrationUnitScanPart v3 0x2d918
                CIntegrationUnitHardwarePart v3 0x2d930 "Integration Unit"
                  CGpibInterface v3 0x2d99e
                  CIntegrationUnitGasConfPart v2 0x2d9bd "IntegrationUnit"
                    1-3/3: CChannelGasConfPart v4 0x2da2a
                  CVisualisationData v8 0x2daed
                  1/8: CCupHardwarePart v5 0x2dc42 "Cup 1"
                    CBasicInterface v2 0x2dc6e
                    CVisualisationData v8 0x2dc9e
                  2/8: CCupHardwarePart v5 0x2dd77 "Cup 2"
                    CBasicInterface v2 0x2dda3
                    CVisualisationData v8 0x2ddd3
                  3/8: CCupHardwarePart v5 0x2deac "Cup 3"
                    CBasicInterface v2 0x2ded8
                    CVisualisationData v8 0x2df08
                  4/8: CCupHardwarePart v5 0x2dfe1 "Cup 4"
                    CBasicInterface v2 0x2e00d
                    CVisualisationData v8 0x2e03d
                  5/8: CCupHardwarePart v5 0x2e116 "Cup 5"
                    CBasicInterface v2 0x2e142
                    CVisualisationData v8 0x2e172
                  6/8: CCupHardwarePart v5 0x2e24b "Cup 6"
                    CBasicInterface v2 0x2e277
                    CVisualisationData v8 0x2e2a7
                  7/8: CCupHardwarePart v5 0x2e380 "Cup 7"
                    CBasicInterface v2 0x2e3ac
                    CVisualisationData v8 0x2e3dc
                  8/8: CCupHardwarePart v5 0x2e4b5 "Cup 8"
                    CBasicInterface v2 0x2e4e1
                    CVisualisationData v8 0x2e511
                  1/3: CChannelHardwarePart v2 0x2e5eb "Channel 1"
                    CBasicInterface v2 0x2e627
                    CVisualisationData v8 0x2e657
                  2/3: CChannelHardwarePart v2 0x2e71f "Channel 2"
                    CBasicInterface v2 0x2e75b
                    CVisualisationData v8 0x2e78b
                  3/3: CChannelHardwarePart v2 0x2e853 "Channel 3"
                    CBasicInterface v2 0x2e88f
                    CVisualisationData v8 0x2e8bf
              CBlockData v2 0x2e9a4
            2/30: CIntegrationUnitGasConfPart v2 0x2e9cc "IntegrationUnit"
              1-7/7: CChannelGasConfPart v4 0x2ea39
            3/30: CDioTransferPart v2 0x2ebc8 "Resitor Channel 1"
            4/30: CDioTransferPart v2 0x2ec3e "Resitor Channel 2"
            5/30: CDioTransferPart v2 0x2ecb4 "Resitor Channel 3"
            6/30: CDioTransferPart v2 0x2ed2a "Resitor Channel 4"
            7/30: CDioTransferPart v2 0x2eda0 "Resitor Channel 5"
            8/30: CDioTransferPart v2 0x2ee16 "Resitor Channel 6"
            9/30: CDioTransferPart v2 0x2ee8c "Resitor Channel 7"
            10/30: CDioTransferPart v2 0x2ef02 "Resitor Channel 8"
            11/30: CDioTransferPart v2 0x2ef78 "Resitor Channel 9"
            12/30: CDioTransferPart v2 0x2efee "Resitor Channel 10"
            13/30: CPeakCenterOffset v1 0x2f066
            14/30: CMagnetCurrentTransferPart v3 0x2f098 "MagnetCurrent"
            15/30: CCalibration v5 0x2f10e "CO2_cal_03022016"
              1/3: CCalibrationPoint v3 0x2f16a "0. Point"
              2/3: CCalibrationPoint v3 0x2f1c2 "1. Point"
              3/3: CCalibrationPoint v3 0x2f21a "2. Point"
            16-17/30: CMolecule v1 0x30cc6
            18/30: CScaleHvTransferPart v2 0x30d14 "Isotope MS/ScaleHv"
            19/30: CCalculatingDacTransferPart v1 0x30d84 "Trap"
            20/30: CCalculatingDacTransferPart v1 0x30dd2 "Electron Energy"
            21/30: CCalculatingDacTransferPart v1 0x30e4c "Emission"
            22/30: CCalculatingDacTransferPart v1 0x30eaa "Extraction"
            23/30: CCalculatingDacTransferPart v1 0x30f10 "Shield"
            24/30: CCalculatingDacTransferPart v1 0x30f66 "R-Plate"
            25/30: CCalculatingDacTransferPart v1 0x30fc0 "Einzel-Lens"
            26/30: CCalculatingDacTransferPart v1 0x3102a "Einzel-Lens Symmetry"
            27/30: CCalculatingDacTransferPart v1 0x310b8 "X-Focus"
            28/30: CCalculatingDacTransferPart v1 0x31112 "X-Focus Symmetry"
            29/30: CCalculatingDacTransferPart v1 0x31190 "Y-Deflection"
            30/30: CCalculatingDacTransferPart v1 0x311fe "Y-Deflection Symmetry"
      2/2: CDualInletDeviceEvaluationPart v1 0x312d0
        CBlockData v2 0x31304
  10/12: CBlockData v2 0x31358 "Ratio Info Block"
    1-3/3: CParsedEvaluationString v1 0x313b4
  11/12: CBlockData v2 0x3149c "Sequence Line Information"
    1/11: CData v3 0x314ea "Row"
    2/11: CData v3 0x31506 "Peak Center"
    3/11: CData v3 0x31532 "Background"
    4/11: CData v3 0x3155c "Pressadjust"
    5/11: CData v3 0x31588 "Reference Refill"
    6/11: CData v3 0x315be "Identifier 1"
    7/11: CData v3 0x315f2 "Identifier 2"
    8/11: CData v3 0x31630 "Analysis"
    9/11: CData v3 0x3165e "Comment"
    10/11: CData v3 0x3168e "Preparation"
    11/11: CData v3 0x316b8 "Method"
  12/12: CBinary v2 0x3170a
