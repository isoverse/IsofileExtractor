# Qtegra `.imexp` Archive Structure

`.imexp` files (or `.imexp.zip` exports) are notebooks produced by **Qtegra** software (Thermo Fisher Scientific), which manages ICP-OES, ICP-MS, and IRMS instrument systems. The underlying storage engine is **SolFS** (a virtual filesystem embedded in a single file); the `.imexp.zip` export is a ZIP dump of the entire SolFS volume.

This document focuses on data from **IRMS systems**, which appear under the plugin name **`TFS253Plus`** throughout the Qtegra file structure. Some structural conventions (versioned timestamp directories, SolFS layout, BinaryFormatter serialization) are likely shared with ICP-OES and ICP-MS notebooks, but the plugin-specific data formats described here are IRMS-specific.

---

## Archive Layout

The archive contains two kinds of top-level entries:

```
#NotVersionedData#/                   ← global non-versioned settings (not extracted)
<timestamp>/                          ← one or more versioned session snapshots
```

### Timestamp Directories

Each versioned snapshot is named with a millisecond-precision UTC timestamp: `YYYYMMDD-HHMMSS-mmm` (e.g. `20260226-001349-777`). Multiple timestamps accumulate as the notebook is saved repeatedly. Lexicographic ordering equals chronological ordering, so the **latest** timestamp directory contains the most recent version of session-level files.

A full-featured timestamp directory contains:

```
<timestamp>/
  Header.xml
  comment.xml
  ProjectSettings.xml
  SampleVariables.xml
  softwareversions/
  SampleList/
    TFS253Plus.samples          ← BinaryFormatter: sample list for TFS plugin
    VogonCalc.samples           ← BinaryFormatter: sample list for VogonCalc plugin
  SampleDefinition/
    BlockDefinition/            ← sample list templates (body, header, footer)
    FooterDefinition/
    HeaderDefinition/
  TFS253Plus/                   ← TFS 253 Plus plugin data
    CapturedSettings_<datetime>/
      StoredSettings.bin        ← BinaryFormatter: amplifier + gas configuration
    ConFlowMethod.bin
    PeripheralMethods/
    Peripherals/
  VogonCalc/                    ← VogonCalc plugin data
  Export/
  PrintLimits/
  _Application Data/
    PluginData/TFS253Plus/
      TFS253Plus.SampleList.xml
      TFS253Plus.SampleDefinitionBlock.xml
      TFS253Plus.SampleDefinitionHeader.xml
      TFS253Plus.SampleDefinitionFooter.xml
  Entry_<uuid>/
    TFS253Plus/
      MeasureData.bin                 ← raw trace data (custom binary)
      MeasureDataIndexLines.bin       ← segment index for MeasureData.bin
      CapturedSettingsFolderName.bin  ← BinaryFormatter: link to settings snapshot
      AdditionalData                  ← BinaryFormatter: post-run results (no extension)
      SampleMetadata                  ← BinaryFormatter: dilution metadata (conflo only)
```

Not all timestamp directories contain all subdirectories. Incremental saves may omit unchanged sections. The `Entry_<uuid>/` folders only appear in the snapshot directory that was current when the acquisition ran.

---

## Binary Serialization Formats

### .NET BinaryFormatter streams

Several files are serialized with .NET's `BinaryFormatter`. Qtegra uses a custom **Imhotep serialization framework** on top of BinaryFormatter that prefixes every field name with the declaring type's GUID (the `TypeId`), producing keys like `4147e638-93e4-4fcb-a361-494646ba0152ChannelSettings`. The key types involved:

| Type | TypeId | Used in |
|------|--------|---------|
| `StoredSettingsSerializable` | `4147e638` | `StoredSettings.bin` root |
| `AmplifierChannelCollectionSerializable` | `afef1e4a` | amplifier channel container |
| `AmplifierChannelSerializable` | `ac85586c` | per-channel wrapper |
| `AmplifierSerializable` | `58b1614a` | individual amplifier |
| `StoredInstrumentModeDataSerializable` | `0e07d289` | instrument mode data |
| `GasConfigurationSerializable` | `fb7fe803` | gas configuration |
| `CupConfigurationSerializable` | `cdcecc54` | per-cup configuration |
| `MoleculeOrMassSerializable` | `94207bd6` | mass value |
| `VersionableDictionary` | `02ee5699` | keyed pairs (field: `pairs`) |
| `VersionableKeyedCollection` | `e2307afa` | ordered collection (field: `Items`) |
| `ImhotepArrayList` | `266ce50d` | dynamic array (field: `m_itemsFromSerialization`) |
| `VersionableList` | `23576c9d` | typed list (field: `items`) |
| `SampleListColumnStorage` | `dd22eb03` | one column of a sample list |
| `SettingsFolderNameContainer` | `1c6dfa86` | settings folder name link |
| `AdditionalSampleDataSerializable` | `e3aa035d` | wrapper for additional data items |
| `LinearityCorrectionResultSerializable` | `c811c53a` | linearity correction results |
| `LinearityCorrectionRatioResultSerializable` | `479d130a` | per-ratio correction result |
| `PeakCenterResultSerializable` | `5774c788` | peak center results |
| `PeakCenterResultDetailSerializable` | `642f745c` | per-tune-book peak center result |

### `StoredSettings.bin` — Instrument Configuration

Root object is `StoredSettingsSerializable`. Contains:

- **ChannelSettings** (`AmplifierChannelCollectionSerializable`): amplifier configuration. Two serialization versions exist:
  - **v2+**: per-channel objects (`AmplifierChannelSerializable`) each holding an array of `AmplifierSerializable` objects.
  - **v1 legacy**: flat list of `AmplifierSerializable` objects directly in the collection.
- **m_instrumentModesData** (`VersionableDictionary`): maps instrument mode keys to `StoredInstrumentModeDataSerializable` objects, each holding a `VersionableKeyedCollection` of `GasConfigurationSerializable` objects.

### `TFS253Plus.samples` (and `VogonCalc.samples`) — Sample Lists

Root object is `VersionableList<SampleListColumnStorage>`. Data is stored **column-by-column**: each `SampleListColumnStorage` has a plugin-prefixed column name (e.g. `TFS253Plus.RunId`) and an `ImhotepArrayList` of cell values. Cell values may be strings, booleans, integers, longs, doubles, `System.Guid` objects, or `__string` objects. `$`-prefixed 37-character strings are GUID references (strip the leading `$` for the GUID value).

### `MeasureDataIndexLines.bin` — Segment Index

Custom fixed-width binary format (all little-endian):

```
Header (8 bytes):
  int32  version   = 1
  int32  blockSize = 21

Records (21 bytes each, until end of file):
  uint8  recordVersion
  int32  setIndex         ← -1 for sentinel (end) record
  int32  lineIndex
  int32  integrationIndex
  int64  position         ← byte offset in MeasureData.bin
```

The last record is a sentinel with all indices = -1 and `position` = total size of the corresponding `MeasureData.bin`. Non-sentinel records mark the start of each integration segment.

### `MeasureData.bin` — Raw Trace Data

Custom binary format (all little-endian):

```
Header (4 bytes):
  int32  version = 1

Segments (one per non-sentinel index record):
  TraceSet × N (fixed size within a segment):
    uint16  version
    int64   timestamp      ← DateTime.ToBinary(): bits 62-63 = DateTimeKind, bits 0-61 = .NET ticks
    int32   channelCount
    channelCount × TracePoint:
      uint16  version
      double  mass
      bool    hasAnalog    ← 1 byte
      [double analogIntensity]    ← present only if hasAnalog
      bool    hasCounter   ← 1 byte
      [double counterIntensity]   ← present only if hasCounter
```

The `TraceSet` size is fixed within a segment (channel layout is constant per segment). The time axis is derived from the `.NET ticks` component of the timestamp (divide by 1 × 10⁷ to get seconds); time is stored as seconds elapsed since the first time point in the segment.

### `CapturedSettingsFolderName.bin` — Settings Cross-Reference

BinaryFormatter stream whose root is `SettingsFolderNameContainer` (TypeId `1c6dfa86`). Contains a single string field `m_folderName` that gives the name of the `CapturedSettings_<datetime>` folder (without path prefix) used for this entry — i.e. the settings snapshot that was active when the acquisition ran.

### `AdditionalData` — Post-Run Results

BinaryFormatter stream whose root is an `ImhotepArrayList`. Each item is an `AdditionalSampleDataSerializable` with a `Key` string and a typed `Data` object. Known key/type pairs:

| Key | Data type | Fields |
|-----|-----------|--------|
| `Linearity_Correction` | `LinearityCorrectionResultSerializable` | `state` (string enum surrogate), `RatioResults` (array of `LinearityCorrectionRatioResultSerializable`) |
| `Peak_Center` | `PeakCenterResultSerializable` | `m_results` (array of `PeakCenterResultDetailSerializable`) |

`LinearityCorrectionResultState` and `PeakCenterResultState` are stored as Imhotep enum surrogates (a BfObj with `_Version` int and `_Value` string).

### `SampleMetadata` — Dilution Metadata *(conflo-based only)*

BinaryFormatter stream whose root is a `VersionableDictionary<string, VersionableList<ValueTuple<Time, Fraction>>>`. Maps string keys (e.g. `SampleDilution`) to lists of (time, fraction) tuples. This file only exists for ConFlo-based dilution experiments and is not extracted by default.

---

## Versioning and the "Latest Snapshot" Rule

Because the archive accumulates multiple timestamp snapshots, the same logical file (e.g. `TFS253Plus.samples`) may appear under several timestamp directories. The **newest** copy is the definitive version. Timestamp strings sort lexicographically in chronological order (e.g. `20260226-001349-777` is newer than `20260225-150422-957`), so the latest version is found by taking the lexicographically maximum top-level timestamp for each filename.

This rule applies to sample lists. Entry data (`Entry_<uuid>/TFS253Plus/`) is tied to a specific timestamp directory and does not repeat across snapshots.

---

## JSON Output Structure

`isoextract` converts each `.imexp.zip` archive to a single `.imexp.json` file (the `.zip` extension is dropped) with the following top-level structure:

```json
{
  "meta": { "isoextract_version": "…", "file_type": "imexp", … },
  "settings": [ … ],
  "sample_lists": [ … ],
  "entries": [ … ]
}
```

### `settings[]`

One object per `StoredSettings.bin` found across all `CapturedSettings_<datetime>/` folders in the archive.

```json
{
  "source": "20260226-001349-777/TFS253Plus/CapturedSettings_2026-02-26-06-13-52-752/StoredSettings.bin",
  "settings_id": "CapturedSettings_2026-02-26-06-13-52-752",
  "amplifiers": [
    {
      "identifier": "VC_CUP0_AMP1",
      "display_name": "Amp 1",
      "channel_number": 1,
      "max_voltage_v": 50,
      "min_voltage_v": 0,
      "resistor_ohm": 1000000000,
      "is_high_gain": false
    }
  ],
  "gas_configurations": [
    {
      "display_name": "N2",
      "calibration_mass": 29,
      "cup_configurations": [
        {
          "display_name": "CUP 2",
          "mass": 28,
          "amplifier_identifier": "VC_CUP1_AMP1"
        },
        {
          "display_name": "CUP 3",
          "mass": 29,
          "amplifier_identifier": "VC_CUP2_AMP1"
        }
      ]
    }
  ]
}
```

`amplifiers[]` fields:

| Field | Source | Notes |
|-------|--------|-------|
| `identifier` | `Identifier` / `m_identifier` | Internal amplifier ID (e.g. `VC_CUP0_AMP1`) |
| `display_name` | `DisplayName` / `m_displayName` | Human-readable label |
| `channel_number` | `ChannelNumber` | Detector channel (v2+ only; null in v1 legacy) |
| `max_voltage_v` | `MaximumVoltage` / `m_maximumVoltage` | Maximum output voltage (V) |
| `min_voltage_v` | `MinimumVoltage` / `m_minimumVoltage` | Minimum output voltage (V) |
| `resistor_ohm` | `ResistorValue` / `m_resistorValue` | Feedback resistor value (Ω) |
| `is_high_gain` | `IsHighGain` | v3/v4 only; absent in legacy versions |

`cup_configurations[]` contains only cups where `IsUsable = true`. `mass` is extracted from the nested `MoleculeOrMassSerializable` object. `amplifier_identifier` cross-references the `identifier` field of the corresponding amplifier.

### `sample_lists[]`

One object per unique `.samples` filename, sourced from the **newest** timestamp directory containing that filename.

```json
{
  "source": "20260226-001338-997/SampleList/TFS253Plus.samples",
  "rows": [
    {
      "sample_line_region": "Body",
      "guid": "a2c367be-2738-4638-ae44-d933167acf36",
      "mark_to_pause": false,
      "identifier": "WICST112",
      "comment": "SA_D1",
      "run_id": 1,
      "analysis_number": "N/A",
      "dilution_pattern": "0/94",
      "con_flo_method": "ConFlo Method 1"
    }
  ]
}
```

Column names are normalized from the plugin-prefixed PascalCase originals: the plugin prefix (everything up to and including the last `.`) is stripped, then PascalCase is converted to snake_case (underscores inserted before uppercase letters that follow a lowercase letter), and spaces are replaced by underscores. Example: `TFS253Plus.RunId` → `run_id`, `ConFlo Method` → `con_flo_method`.

Cell values retain their native types: booleans, integers, doubles, and strings. `System.Guid` values are formatted as standard UUID strings. `$`-prefixed GUID reference strings have the leading `$` stripped. Cells with no value in a column are `null`.

### `entries[]`

One object per `Entry_<uuid>/` directory. Entries are sourced from all timestamp directories (each entry appears in exactly one timestamp directory).

```json
{
  "source": "20260210-114429-570/Entry_0138d3a5-63a9-49bd-880a-60f4e45b35d1/TFS253Plus",
  "entry_id": "0138d3a5-63a9-49bd-880a-60f4e45b35d1",
  "type": "TFS253Plus",
  "settings_id": "CapturedSettings_2026-02-10-18-44-33-067",
  "segments": [ … ],
  "additional_data": { … }
}
```

| Field | Source | Notes |
|-------|--------|-------|
| `entry_id` | Directory name (`Entry_<uuid>`) | UUID stripped of the `Entry_` prefix |
| `type` | Subdirectory name inside `Entry_<uuid>/` | Typically `TFS253Plus` |
| `settings_id` | `CapturedSettingsFolderName.bin` | Name of the `CapturedSettings_<datetime>` folder used for this acquisition; cross-references `settings[].settings_id` |

#### `segments[]`

Each segment corresponds to one non-sentinel record in `MeasureDataIndexLines.bin` and covers a contiguous block of `TraceSet` records in `MeasureData.bin`. A single entry typically has one segment but may have more if the acquisition was interrupted or restarted.

```json
{
  "integration_line_set_index": 0,
  "line_index": 0,
  "integration_index": 0,
  "n_timepoints": 977,
  "time_s": [0.0, 0.1, 0.2, …],
  "channels": [
    {
      "mass": 28.0,
      "detector": "analog",
      "intensity": [1.275e9, 1.274e9, …]
    },
    {
      "mass": 29.0,
      "detector": "analog",
      "intensity": [1.23e7, 1.22e7, …]
    }
  ]
}
```

`time_s` is seconds elapsed since the first time point of the segment (derived from the `.NET ticks` component of the BinaryFormatter timestamp). `detector` is `"analog"`, `"counter"`, or `"none"`. `intensity` units are raw ADC counts for analog detectors (not converted to Amperes).

Index fields from `MeasureDataIndexLines.bin`:

| Field | Meaning |
|-------|---------|
| `integration_line_set_index` | Identifies the integration line set (method block) |
| `line_index` | Line index within the set (e.g. reference vs. sample peak) |
| `integration_index` | Integration pass index within the line |

#### `additional_data`

Keyed object parsed from the `AdditionalData` file. Keys are lowercased versions of the `Key` field in `AdditionalSampleDataSerializable`. Present only when the file exists and is non-empty.

For a CO₂ measurement:

```json
{
  "linearity_correction": {
    "state": "Successful",
    "ratio_results": [
      {
        "name": "45C.O.O/44C.O.O",
        "conversion_factor": 3000,
        "linearity_correction_factor": -8.527e-05,
        "is_default": false
      }
    ]
  },
  "peak_center": {
    "results": [
      {
        "tune_book_name": "Carbon Dioxide",
        "high_voltage_diff_v": -14.893,
        "state": "Successful"
      }
    ]
  }
}
```

For an H₂ measurement, the `linearity_correction_factor` for the 3H.H/2H.H ratio is the **H3⁺ correction factor** — the contribution of the trihydrogen cation (H₃⁺) to the mass-3 beam, which must be subtracted to obtain the true δD:

```json
{
  "linearity_correction": {
    "state": "Successful",
    "ratio_results": [
      {
        "name": "3H.H/2H.H",
        "conversion_factor": 1000,
        "linearity_correction_factor": 0.003460697092696616,
        "is_default": false
      }
    ]
  },
  "peak_center": {
    "results": [
      {
        "tune_book_name": "Hydrogen",
        "high_voltage_diff_v": -3.193,
        "state": "Successful"
      }
    ]
  }
}
```

---

## Data Not Extracted

`isoextract` is focused on **raw measurement data and instrument metadata** — beam intensities, detector configuration, resistor values, gas configurations, and the sample list. Data processing information (computed ratios, delta values, corrections, calibrations) is intentionally excluded: it is calculated by Qtegra from the raw data and is not stored in the archive in a stable, format-independent way.

| Path pattern | Content | Reason not extracted |
|--------------|---------|----------------------|
| `#NotVersionedData#/` | Global ordering and grid settings | Non-analytical UI state |
| `<timestamp>/Header.xml` | Session header XML | Metadata duplicated elsewhere |
| `<timestamp>/comment.xml` | User comment | Not yet integrated |
| `<timestamp>/ProjectSettings.xml` | Project-level settings | Not yet integrated |
| `<timestamp>/SampleVariables.xml` | Sample variable definitions | Not yet integrated |
| `<timestamp>/softwareversions/` | Software version info | Informational only |
| `<timestamp>/SampleDefinition/` | Sample list templates (header/body/footer) | Template data, not measurement data |
| `<timestamp>/TFS253Plus/ConFlowMethod.bin` | ConFlo interface method | Not yet decoded |
| `<timestamp>/TFS253Plus/PeripheralMethods/` | Peripheral device methods | Not yet decoded |
| `<timestamp>/_Application Data/` | Plugin XML definitions | UI configuration |
| `Entry_<uuid>/TFS253Plus/SampleMetadata` | ConFlo dilution time series | Only present for conflo-based dilution experiments |
