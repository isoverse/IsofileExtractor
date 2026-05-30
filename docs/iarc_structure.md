# IARC File Structure

`.iarc` files are ZIP archives produced by Elementar's **IonOS** and **LyticOS** software for the isoprime visION IRMS instrument series. Each archive bundles together all metadata, instrument state snapshots, and raw measurement data for one run sequence (a list of tasks/samples).

## Archive Version Variants

Two major structural variants exist, distinguished by archive version and directory layout.

### V2 / V3-flat

Archive version ≤ 2 (or early v3 without nested task directories). All entry files sit at the archive root:

```
Info
ProcessingList_<id>
Method_<id>            ← one or more
System_<id>            ← zero or more
Task_<uuid>            ← one per sample/task
<dataset_id>.hdf5      ← one per dataset that has raw signal data
```

### V3-nested

Archive version 3 with the full nested directory structure used by current IonOS/LyticOS versions:

```
Info
ProcessingList_<id>
ProcessingLists/<id>/                    ← directory stub (no files inside)
Snapshot/<method_id>/snapshot.xml        ← one per method
Snapshot/<method_id>/Extensions/IRMSAcquisitionDisplaySettings/<species>.xml
System_<id>                              ← one per task (hardware snapshot)
AcquisitionTask/Task_<uuid>/AcquisitionTask.xml  ← one per task
AcquisitionTask/Task_<uuid>/<dataset_id>/AcquisitionDataSet.hdf5
ReadbacksDisplaySettings/DisplaySettings.xml
Snapshot/<method_id>/B2273D87-…txt      ← UI persistence (not data-relevant)
```

### Version Detection

Open `Info` and read `<Version>`. An integer value of `2` indicates V2/V3-flat layout; `3` typically means V3-nested, though the presence of `AcquisitionTask/` directory entries is a reliable structural indicator regardless of the declared version.

---

## XML Encoding Quirk

All XML entry files in `.iarc` archives declare `encoding="utf-16"` in their XML prolog:

```xml
<?xml version="1.0" encoding="utf-16"?>
```

However, the bytes on disk are physically **UTF-8** (or ASCII-compatible). Parsers that respect the declared encoding will fail. The declaration must be stripped or ignored before parsing. In practice, opening the entry stream as UTF-8 produces correct results for all observed archives.

---

## `Info` — Archive Manifest

A single `Info` entry at the archive root. Minimal XML structure:

```xml
<SerialisedArchive>
  <Version>2</Version>
  <CreatedDate>2015-04-16T09:24:13.2333399+01:00</CreatedDate>
  <ProcessingLists>
    <SerialisedProcessingListMetaData>
      <DefinitionUniqueIdentifier>{BC89E456-57C0-4FF1-9110-90C8E6AE1B69}</DefinitionUniqueIdentifier>
      <Name>EA CN Demo</Name>
      <Id>1</Id>
      <NumberOfTasks>46</NumberOfTasks>
    </SerialisedProcessingListMetaData>
  </ProcessingLists>
</SerialisedArchive>
```

Key fields:

| Field | XML tag | Notes |
|-------|---------|-------|
| Archive version | `Version` | Integer; governs layout variant |
| Creation date | `CreatedDate` | ISO 8601 with timezone |
| Processing list GUID | `DefinitionUniqueIdentifier` | Curly-brace GUID; matches `ProcessingListTypeIdentifier` in tasks and methods |
| Processing list name | `Name` | Human-readable label |
| Processing list integer ID | `Id` | Matches the suffix of `ProcessingList_<id>` |
| Task count | `NumberOfTasks` | Total tasks under this processing list |

---

## `ProcessingList_<id>` — Species and Ratio Definitions

One file per processing list. Contains the species (gas) names, the beam channel assigned to each species for detection, and the isotope ratio definitions (numerator beam, denominator beam, delta label). In V3-nested archives this file is also mirrored as `ProcessingLists/<id>/` but the root-level file is the authoritative one.

The inner structure is a `SerialisablePropertyBag` hierarchy keyed by opaque GUIDs. Two GUIDs are **fixed** and stable across all archives observed:

| GUID | Role |
|------|------|
| `4CBF5188-0ECA-46D3-9A8E-F913A4164934` | Per-species bag; carries `SpeciesName`, `DetectionBeam` |
| `7440D4F0-2E31-40FF-BF19-5BC24A3227F9` | Per-cup beam bag; carries `BeamChannel`, `UseLowGain` |

Ratio definitions live inside the species bag under a child bag identified by a ratio-list GUID, which itself contains per-ratio bags carrying `Label`, `NumeratorBeamChannel`, `DenominatorBeamChannel`, `DeltaLabel`. The IRMS device (visION) is identified by the presence of `4CBF5188` bags; other device names vary across IonOS versions.

### SerialisablePropertyBag Layout

All property bags share the same XML structure regardless of their content:

```xml
<SerialisablePropertyBag>
  <Identifier>…GUID…</Identifier>
  <SerialisedChildPropertyBags>
    <SerialisablePropertyBag> … </SerialisablePropertyBag>
  </SerialisedChildPropertyBags>
  <SerialisedPropertyBagProperties>
    <PersistedPropertyBagProperty>
      <Identifier>PropertyName</Identifier>
      <Value>PropertyValue</Value>
    </PersistedPropertyBagProperty>
  </SerialisedPropertyBagProperties>
</SerialisablePropertyBag>
```

---

## Method Files — Instrument Method Definitions

### V2/V3-flat: `Method_<id>`

Root tag: `SerialisedMethodSnapshotProxy`. The actual method content is inside a child `<Snapshot>` element.

```xml
<SerialisedMethodSnapshotProxy>
  <Snapshot>
    <Name>EA Analysis NC</Name>
    <GlobalIdentifier>fe49ee74-…</GlobalIdentifier>
    <ProcessingListTypeIdentifier>{BC89E456-…}</ProcessingListTypeIdentifier>
    <SerialisedHierarchicalFlow>…</SerialisedHierarchicalFlow>
    <SerialisedMethodParameters>
      <SerialisedMethodParameter>
        <FlowParameterId>4df9279a-…</FlowParameterId>
        <StringValue>2mg70sIRMS_fast</StringValue>
        <ColumnId>12</ColumnId>
        <ColumnName>EA Method</ColumnName>
      </SerialisedMethodParameter>
    </SerialisedMethodParameters>
    <FlowParameters>
      <SerialisedFlowParameter>
        <Id>4df9279a-…</Id>
        <Name>EAMethod</Name>
        <DisplayName>EA Method</DisplayName>
        <TypeIdentifier>String</TypeIdentifier>
      </SerialisedFlowParameter>
    </FlowParameters>
  </Snapshot>
</SerialisedMethodSnapshotProxy>
```

### V3-nested: `Snapshot/<method_id>/snapshot.xml`

Root tag: `SerialisedMethodSnapshot`. Same content structure as the `<Snapshot>` element in V2/V3-flat, but **`SerialisedMethodParameter` entries lack `ColumnName`** (only `ColumnId` is present). The `SerialisedFlowParameter.DisplayName` serves as the parameter display label in this variant.

### Parameter Name Resolution

The definitive display name for a flow parameter is resolved using the following precedence:

1. `SerialisedMethodParameter.ColumnName` — present in V2/V3-flat archives; reflects the column label shown in the Isodat processing list UI. Take this when non-empty and not `"(none)"`.
2. `SerialisedFlowParameter.DisplayName` — present in all archive versions; the flow-level label used in the instrument software. Use as fallback when `ColumnName` is absent.

Note: `ColumnName` and `DisplayName` can differ for the same parameter across archive versions (e.g. `"Sample Type"` vs. `"Dilution Type"` for GUID `961293e6`). The stable identity across all versions is `FlowParameterId` / `SerialisedFlowParameter.Id` — these GUIDs are fixed by the Elementar flow definition and do not change between IonOS versions.

### Key XML Elements

| Element | Meaning |
|---------|---------|
| `Name` | Human-readable method name |
| `GlobalIdentifier` | UUID identifying this method version |
| `ProcessingListTypeIdentifier` | Curly-brace GUID linking to the processing list |
| `SerialisedHierarchicalFlow/Name` | Ordered list of named flow steps (e.g. "EA 2 Species", "Monitoring Gas Pulse") |
| `SerialisedMethodParameter/FlowParameterId` | UUID of the parameter (matches `SerialisableTaskValue.ParameterIdentifier` in tasks) |
| `SerialisedMethodParameter/ColumnName` | Processing-list column label (V2/V3-flat only) |
| `SerialisedMethodParameter/StringValue` | Default value stored in the method |
| `SerialisedFlowParameter/Id` | Same UUID as `FlowParameterId` |
| `SerialisedFlowParameter/DisplayName` | Display label (all versions) |

---

## `System_*` — Hardware State Snapshots

One `System_<id>` file per snapshot of the instrument hardware configuration at the time of acquisition. In current IonOS archives there is one snapshot per task (tasks reference them via `SystemSnapshotId`); in older archives multiple tasks may share one snapshot.

### Double-wrapped XML

Each `System_*` file is a two-layer XML document. The outer layer:

```xml
<SerialisedSnapshot>
  <Id>409</Id>
  <Name>KB007</Name>
  <SerialisedContent>…inner XML string…</SerialisedContent>
</SerialisedSnapshot>
```

`<SerialisedContent>` holds a second XML document as an **escaped string** (the inner content must be parsed as XML after extraction). The inner document (`SerialisedSystemSnapshot`) contains `SerialisablePropertyBag` nodes for every device connected to the instrument.

### IRMS Device Identification

The isoprime visION IRMS device bag is identified by the presence of child bags with identifier `4CBF5188-0ECA-46D3-9A8E-F913A4164934` (the same per-species GUID used in the ProcessingList). The device name itself varies across IonOS versions ("VisION", "isoprime visION") and should not be used for detection.

### Per-Beam Cup Data (Fixed GUID `7440D4F0`)

Within the IRMS device bag, child bags with identifier `7440D4F0-2E31-40FF-BF19-5BC24A3227F9` carry per-cup information:

| Property | Value |
|----------|-------|
| `BeamChannel` | Beam name (e.g. `Beam1`, `Beam3`) |
| `UseLowGain` | `True` = ~1 GΩ resistor active; `False` = ~100 GΩ resistor active |

From `UseLowGain` the nominal resistance is derived: `True` → 1 × 10⁹ Ω, `False` → 1 × 10¹¹ Ω.

### Conductance Calibration Sets

The IRMS device bag contains multiple `SerialisablePropertyBag` nodes that each hold beam conductance values (unit: Siemens = 1/Ω). Keys are `Beam1` … `Beam10`; values are floating-point numbers in scientific notation. These sets are classified by the fraction of beams using the low-gain resistor (conductance > 5 × 10⁻¹⁰ S, equivalent to resistance < 2 GΩ):

| Fraction of beams with conductance > 5 × 10⁻¹⁰ S | Classification |
|--------------------------------------------------|----------------|
| > 0.75 | `all_low_gain` — cross-calibration with all ~1 GΩ resistors |
| < 0.25 | `all_high_gain` — cross-calibration with all ~100 GΩ resistors |
| 0.25 – 0.75 | `mixed_gain` — in-use calibration matching the measurement configuration |

The `mixed_gain` set gives the calibrated conductance during normal operation; `all_low_gain` and `all_high_gain` are cross-calibration references. The best calibrated resistance for each beam is `1 / conductance` from the `mixed_gain` set.

Conductance sets are zero-valued for beams that are not installed or not used in the measurement; these should be excluded (zero conductance ↔ infinite resistance).

### Tuning Bags

The IRMS device bag also contains tuning-specific bags that describe the instrument configuration (species, tuning name) for each acquisition. These bags are identified by the presence of a `TuningName` property rather than by a fixed GUID (the GUID for tuning bags varies between archive generations).

---

## Task Files — Per-Sample Measurement Records

### V2/V3-flat: `Task_<uuid>`

Root tag: `SerialisableTask` or `SerialisedTask`. The UUID matches the task's `GlobalIdentifier`.

### V3-nested: `AcquisitionTask/Task_<uuid>/AcquisitionTask.xml`

Same XML structure as V2/V3-flat. The directory path encodes the task UUID; this UUID is needed to construct HDF5 file paths (see below).

### Task XML Fields

```xml
<SerialisableTask>
  <Name>USGS41</Name>
  <Id>6605</Id>
  <GlobalIdentifier>0e08e8b4-…</GlobalIdentifier>
  <AcquisitionStartDate>2014-10-08T14:14:35…</AcquisitionStartDate>
  <AcquisitionEndDate>2014-10-08T14:24:49…</AcquisitionEndDate>
  <CompletionState>Success</CompletionState>
  <MethodId>77</MethodId>
  <ProcessingListTypeIdentifier>{BC89E456-…}</ProcessingListTypeIdentifier>
  <SystemSnapshotId>409</SystemSnapshotId>
  <SampleType>Sample</SampleType>
  <TaskListName>200131 H Novak</TaskListName>
  <SystemDescription>KB007</SystemDescription>
  <Values>
    <SerialisableTaskValue>
      <ParameterIdentifier>4df9279a-…</ParameterIdentifier>
      <Value>2mg70sIRMS_fast</Value>
    </SerialisableTaskValue>
  </Values>
  <DataSets>
    <SerialisableDataSet>
      <Id>40</Id>
      <TypeIdentifier>Acquire</TypeIdentifier>
      <AcquireDataStatus>Completed</AcquireDataStatus>
      <AcquireStartDate>2014-10-08T14:15:24…</AcquireStartDate>
      <AcquireEndDate>2014-10-08T14:19:25…</AcquireEndDate>
    </SerialisableDataSet>
  </DataSets>
</SerialisableTask>
```

### Cross-References

| Field | Links to |
|-------|----------|
| `MethodId` | Integer ID of the method (`Method_<id>` or `Snapshot/<id>/snapshot.xml`) |
| `SystemSnapshotId` | Integer ID of the hardware snapshot (`System_<id>`) |
| `ProcessingListTypeIdentifier` | Curly-brace GUID matching `DefinitionUniqueIdentifier` in `Info` |
| `SerialisableTaskValue.ParameterIdentifier` | `SerialisedFlowParameter.Id` (= `SerialisedMethodParameter.FlowParameterId`) in the linked method |

### Task Values

`<Values>` holds one `SerialisableTaskValue` per instrument/method parameter that was set for this task. Each entry contains a `ParameterIdentifier` (the flow parameter UUID) and a `Value` string. To resolve the parameter name, look up the UUID in the method's `SerialisedFlowParameter` entries (see [Parameter Name Resolution](#parameter-name-resolution) above).

Some archives or task types have `<Values />` (empty element) — these tasks carry no per-task parameter values. Some tasks have `ParameterIdentifier` entries without a `<Value>` child element — these represent parameters with no value set.

### Dataset Types

| `TypeIdentifier` | Content | Has HDF5 file |
|-----------------|---------|--------------|
| `Acquire` | IRMS beam currents or TCD signal | Yes — see HDF5 section |
| `Vario EA Results` | Elemental analysis percentages (C, N, …) | Yes in newer archives; absent in some older ones |
| `Vario TCD` | Thermal conductivity trace | Only in newer archives; 0 rows when present but unused |
| UUID-format string | Internal IonOS dataset (e.g. calibration) | No |
| `Scan Acquire` | Scan-mode acquisition | Yes |

---

## HDF5 Data Files

Each dataset that carries raw signal data has a corresponding HDF5 file containing a single dataset named `DataSet`. The HDF5 file is a compound dataset (one row per time point), with attributes on the `DataSet` object.

### File Path Conventions

| Archive variant | HDF5 path |
|----------------|-----------|
| V2/V3-flat | `<dataset_id>.hdf5` (at archive root) |
| V3-nested | `AcquisitionTask/Task_<uuid>/<dataset_id>/AcquisitionDataSet.hdf5` |

The `<dataset_id>` is the integer `Id` from the task's `SerialisableDataSet`. For V3-nested, `<uuid>` is the task UUID encoded in the `AcquisitionTask/Task_<uuid>/` directory.

### DataSet Attributes

| Attribute | Type | Content |
|-----------|------|---------|
| `Species` | ASCII string | Gas/species name (e.g. `N2`, `CO2`); empty for TCD and EA Results |
| `Tuning` | ASCII string | Tuning name (e.g. `Normal`, `CO`); empty for TCD and EA Results |

### Compound Column Layouts

Three distinct column schemas appear in practice:

**IRMS beam data** — one row per integration point:

| Column | Type | Description |
|--------|------|-------------|
| `Scan` | float64 | Scan index (0-based integer stored as double) |
| `Beam1` … `Beam10` | float64 | Ion beam current (Amperes); only the beams active for this measurement are present. Columns are not guaranteed to be in numeric order (e.g. `Beam1, Beam2, Beam4, Beam3`). |

**TCD trace data** — often 0 rows (dataset exists but is empty) in many archives:

| Column | Type | Description |
|--------|------|-------------|
| `Scan` | float64 | Scan index |
| `TCD` | float64 | Thermal conductivity detector signal |

**Elemental analysis results** — typically 2 rows (C and N):

| Column | Type | Description |
|--------|------|-------------|
| `Element` | Fixed-length UTF-16 LE string (20 bytes) | Element symbol (`C`, `N`, `S`, …) |
| `PerCent` | float64 | Weight percent of the element in the sample |
| `Area` | float64 | Chromatographic peak area |

---

## `IRMSAcquisitionDisplaySettings/<species>.xml` — Beam-to-Mass Mapping

Present only in V3-nested archives, under `Snapshot/<method_id>/Extensions/IRMSAcquisitionDisplaySettings/`. One file per species (filename without extension = species name, e.g. `CO2.xml`, `N2.xml`). Despite the `.xml` extension, the content is **JSON** in the following schema:

```json
{
  "V": 2,
  "I": "<outer-guid>",
  "B": [
    {
      "I": "<beam-list-guid>",
      "B": [
        {
          "I": "<per-beam-guid>",
          "B": [],
          "P": [
            { "V": "Beam1",  "I": "BeamChannel"  },
            { "V": "44",     "I": "Label"         },
            { "V": "44",     "I": "MassNumber"    },
            { "V": "true",   "I": "Visibility"    }
          ]
        }
      ]
    }
  ]
}
```

The keys are abbreviated: `V` = value, `I` = identifier, `B` = children array, `P` = properties array.

Each node in `B` that has a `P` array containing both `BeamChannel` and `MassNumber` properties defines an explicit beam-to-mass assignment for this species. The `MassNumber` is an integer m/z. Nodes with `Label` but no `BeamChannel` are ratio definitions (numerator/denominator mass labels) and do not carry beam assignments.

This provides the explicit mapping of each `Beam*` column in the HDF5 data to an m/z value for a given species and method. This information is not available from the processing list or HDF5 attributes alone.

---

## JSON Output Structure

`isoextract` converts each `.iarc` archive to a single `.iarc.json` file with the following top-level structure:

```json
{
  "meta": {
    "isoextract_version": "0.1.3.0",
    "file_type": "iarc",
    "file_size_bytes": 662269,
    "complete": true
  },
  "archive_version": 2,
  "created_date": "2015-04-16T09:24:13.2333399+01:00",
  "processing_lists": [ … ],
  "methods": [ … ],
  "systems": [ … ],
  "tasks": [ … ]
}
```

### `processing_lists[]`

```json
{
  "id": 1,
  "name": "EA CN Demo",
  "guid": "{BC89E456-57C0-4FF1-9110-90C8E6AE1B69}",
  "n_tasks": 46,
  "species": [
    {
      "name": "CO2",
      "detection_beam": "Beam1",
      "ratios": [
        { "label": "45/44", "numerator_beam": "Beam2", "denominator_beam": "Beam1", "delta_label": "RAW δ45" }
      ]
    }
  ]
}
```

### `methods[]`

```json
{
  "id": 77,
  "name": "EA Analysis NC",
  "global_id": "fe49ee74-…",
  "processing_list_guid": "{BC89E456-…}",
  "flows": ["EA 2 Species", "Close Monitoring Gas With Delay", "Monitoring Gas Pulse"],
  "params": [
    { "name": "EA Method", "value": "2mg70sIRMS_fast" },
    { "name": "EA Sample Weight", "value": "1" }
  ],
  "beam_masses": [
    {
      "species": "CO2",
      "beams": [
        { "beam": "Beam1", "mass": 44 },
        { "beam": "Beam2", "mass": 45 },
        { "beam": "Beam3", "mass": 46 }
      ]
    }
  ]
}
```

`beam_masses` is only present for methods from V3-nested archives that have `IRMSAcquisitionDisplaySettings` files. `params` shows the method-level defaults; per-task overrides are in `tasks[].values`.

### `systems[]`

```json
{
  "id": 409,
  "name": "KB007",
  "global_id": "…",
  "species": ["N2", "CO2"],
  "tunings": ["Normal"],
  "beams": [
    {
      "beam": "Beam1",
      "low_gain": false,
      "nominal_R_ohm": 1e11,
      "inuse_conductance_S": 9.95e-12,
      "inuse_R_ohm": 1.005e11,
      "low_gain_conductance_S": 9.87e-10,
      "low_gain_R_ohm": 1.013e9,
      "high_gain_conductance_S": 9.95e-12,
      "high_gain_R_ohm": 1.005e11
    }
  ]
}
```

`inuse_*` values come from the `mixed_gain` conductance set (calibrated values during the actual measurement). `low_gain_*` and `high_gain_*` come from cross-calibration sets. Fields absent when the corresponding calibration set was not found in the archive. `nominal_R_ohm` is derived from `UseLowGain`: 1 × 10⁹ Ω (low-gain) or 1 × 10¹¹ Ω (high-gain).

### `tasks[]`

```json
{
  "name": "USGS41",
  "id": 6605,
  "global_id": "0e08e8b4-…",
  "acquisition_start": "2014-10-08T14:14:35…",
  "acquisition_end": "2014-10-08T14:24:49…",
  "completion_state": "Success",
  "method_id": 77,
  "system_snapshot_id": 409,
  "processing_list_guid": "{BC89E456-…}",
  "sample_type": "Sample",
  "task_list_name": "200131 H Novak",
  "system_description": "KB007",
  "values": {
    "EA Method": "05mg70sIRMS_fast",
    "EA Sample Weight": "1",
    "Sample Type": "Glutamic Acid - 1mg"
  },
  "datasets": [
    {
      "id": 40,
      "type": "Acquire",
      "status": "Completed",
      "start": "2014-10-08T14:15:24…",
      "end": "2014-10-08T14:19:25…",
      "data": {
        "species": "N2",
        "tuning": "Normal",
        "Scan": [0.0, 1.0, 2.0, …],
        "Beam1": [1.09e-9, …],
        "Beam2": [1.08e-11, …],
        "Beam4": [1.00e-9, …],
        "Beam3": [1.05e-11, …]
      }
    },
    {
      "id": 42,
      "type": "Vario EA Results",
      "status": "Completed",
      "data": {
        "ea_results": [
          { "element": "C", "percent": 39.74, "area": 59265.0 },
          { "element": "N", "percent": 4.23,  "area":  6321.0 }
        ]
      }
    }
  ]
}
```

Fields present only in V3-nested tasks: `sample_type`, `task_list_name`, `system_description`, `system_snapshot_id`. The `values` object uses resolved parameter display names as keys; if a `ParameterIdentifier` cannot be matched to a known parameter in the linked method, the raw GUID is used as the key. Datasets with no corresponding HDF5 file (some TCD and EA Results entries in older archives) or empty HDF5 files (zero-row datasets) do not include a `data` field.

---

## Data Not Extracted

The following entries are present in some `.iarc` archives but are not included in the JSON output:

| Entry pattern | Content | Reason not extracted |
|---------------|---------|----------------------|
| `TaskMiscFile_*FILE_Readbacks` | Hardware telemetry JSON (pressure, temperature, flow readings during acquisition) | Opaque device-specific monitoring data; not analytically relevant |
| `Snapshot/*.txt` (`XamTileManagerPersistenceInfo`) | UI tile layout persistence | Display state only |
| `ReadbacksDisplaySettings/DisplaySettings.xml` | Instrument display configuration | Display state only |
| `ProcessingLists/<id>/` | Empty directory stubs | No files inside |

The HDF5 files contain all raw signal data. Higher-level processed results (delta values, ratios, working gas comparisons) are computed downstream in IonOS/lyticOS and are not stored in the archive.
