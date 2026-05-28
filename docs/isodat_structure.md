# Isodat File Structure

Isodat files (`.dxf`, `.cf`, `.did`, `.caf`, `.scn`) are binary archives produced by Thermo Fisher's Isodat software. They store instrument data as a flat stream of serialized Microsoft Foundation Classes (MFC) C++ objects written by `CArchive`. Every valid isodat file begins with the two bytes `FF FF`, which is the CRuntimeClass header for the first object.

## MFC CArchive Serialization

MFC's `CArchive` serializes a C++ object graph by writing each object's class identity followed by the object's data fields. The identity is encoded as a **CRuntimeClass header** — a compact descriptor that either introduces a new class by name or references a class already seen earlier in the stream.

### CRuntimeClass Headers

| First 2 bytes (uint16 LE) | Meaning |
|---------------------------|---------|
| `FF FF` | New class: followed by a uint16 schema version, a uint16 class-name length, then the class name as ASCII bytes |
| `7F FF` | Long back-reference: followed by a uint32 that is the MFC map index of the previously registered class (it's unlikely that there are too many objects for shorts though) |
| `00 00` | NULL object pointer (no object follows) |
| Any value with high bit set (0x8000–0xBFFF, excluding 0xFFFF) | Short back-reference: the 15 lower bits are the MFC map index of the previously registered class |

After the class header (new or back-reference), the archive writes the object's serialized payload — the bytes produced by calling `CObject::Serialize(CArchive&)` on that instance.

### The MFC Map Counter

MFC maintains an internal counter (the "map") to assign stable indices to classes as they appear in the stream. The counter starts at **1**:

- Each **new-class header** (`FF FF`) consumes **two** map slots: one for the class descriptor itself and one for the first object instance of that class.
- Each **back-reference header** (short or long) consumes **one** map slot: one for the new object instance that reuses the already-registered class.
- A **NULL header** (`00 00`) consumes no slot.

Back-reference values encode the map index at the time the class was first registered. To resolve a back-reference, a reader must replay the entire counter sequence from the beginning — there is no random-access index.

## Sequential Reading Requirement

Isodat files **cannot be parsed in any order other than start-to-finish**. Two properties enforce this:

1. **Class resolution via map counter.** A back-reference like `0x8003` means "the class whose map index is 3." That index only has meaning if the reader has already processed all preceding objects and incremented the counter accordingly. Skipping any object or jumping to an arbitrary offset makes all subsequent back-references unresolvable.

2. **No skip table or offset index.** The file contains no table of contents, no per-object length prefix, and no byte-offset index. There is no way to compute where a given object ends without fully deserializing its payload, because object payloads may embed arbitrarily nested child objects.

As a consequence, the reader must process every object in document order, even objects whose data it does not care about.

## Object Payload Structure

Once the class header is consumed, the archive writes the object's payload in a fixed order determined by the C++ `Serialize` method:

1. **Parent class payload first.** Every `Serialize` override calls `ParentClass::Serialize(ar)` before writing its own fields. This means the fields of base classes appear in the stream before the fields of derived classes, and the entire inheritance chain is traversed recursively from the most-derived class up to the root.

2. **Schema version.** Immediately after the parent call (or at the very start if there is no parent call), the class writes a `DWORD` (4 bytes, uint32) schema version. This version gates which optional fields follow: a field introduced in version N is only present if `version >= N`.

3. **Own fields.** The class's fields are written in source-code declaration order. Field types follow standard C++ sizes: `DWORD`/`LONG` = 4 bytes, `double` = 8 bytes, `WORD` = 2 bytes, `BYTE` = 1 byte.

4. **Embedded child objects.** A class may write child objects inline by calling `ar.WriteObject(child)` or `ar << child`. Each child emits its own CRuntimeClass header followed by its own payload, producing recursive nesting.

## CBlockData Child-Object Lists

`CBlockData` subclasses (e.g. `CDualInletBlockData`, `CContiniousFlowBlockData`, `CScanStorage`) store variable-length lists of heterogeneous child objects. Each list is preceded by a `DWORD` count, followed by that many back-to-back object records (each with its own CRuntimeClass header and payload). These lists are the primary mechanism by which instrument-specific data sections (scan parts, hardware parts, gas configurations, etc.) are attached to a measurement.

## CString Encoding

MFC `CString` objects written by `CArchive` use a three-byte prefix:

```
FF FE FF
```

followed by a variable-length encoded character count (stored as a WORD if < 0x8000, or as `FF FF` + DWORD for longer strings), then the string data in **UTF-16 LE** (2 bytes per character). An empty string writes `FF FE FF 00 00`.

## File-Type Entry Points

Each file extension uses a fixed set of top-level objects as its entry point:

| Extension | Top-level objects (in order) |
|-----------|------------------------------|
| `.dxf`    | `CFileHeader`, `CContiniousFlowBlockData` |
| `.cf`     | `CFileHeader`, `CMethod`, `CPlotSettings`, `CBlockData` ×4 |
| `.did`    | `CFileHeader`, `CDualInletBlockData` |
| `.caf`    | `CFileHeader`, `CLong`, `CBlockDataContext` |
| `.scn`    | `CScanStorage` |

After the last expected object, the file must be fully read. Any remaining bytes indicate a parse error or an unimplemented trailing object.

# Data Locations

## Raw Data

Raw measurement data is stored in different container objects depending on file type. For `.scn`, `.cf`, and `.dxf` files the data is decoded from a flat binary buffer into two parallel arrays: `x` (float, the scan-axis values) and `traces` (double, one array per detector, one value per point). For `.did` and `.caf` files the raw voltages are held in `CDualInletRawData` using a different structure (`CIntensityData` blocks organised by acquisition cycle).

### Trace masses

For all file types, the m/z mass assigned to each detector trace is stored in `CChannelGasConfPart[N].mass` inside the `CGasConfiguration`. The index `N` corresponds to the trace index in the `traces` array (0-based). The path to these values depends on file type:

| Extension | Path to `CChannelGasConfPart[N].mass` |
|-----------|---------------------------------------|
| `.scn` | `CScanStorage.CGasConfiguration.p.objects.CIntegrationUnitGasConfPart.CChannelGasConfPart[N].mass` |
| `.cf`  | `CMethod.p.objects.CGasConfiguration.p.objects.CIntegrationUnitGasConfPart.CChannelGasConfPart[N].mass` |
| `.dxf` | `CContiniousFlowBlockData.p.objects.CBlockData[5].objects.CMethod.p.objects.CGasConfiguration.p.objects.CIntegrationUnitGasConfPart.CChannelGasConfPart[N].mass` |
| `.did` | `CDualInletBlockData.p.objects.CMethod.p.objects.CGasConfiguration.p.objects.CIntegrationUnitGasConfPart.CChannelGasConfPart[N].mass` |
| `.caf` | `CBlockDataContext.p.objects.CMethod.p.objects.CGasConfiguration.p.objects.CIntegrationUnitGasConfPart.CChannelGasConfPart[N].mass` |

For `.scn` and `.cf` files, a human-readable `trace_labels` string array (e.g. `["Mass 44", "Mass 45", "Mass 46"]`) is also available directly on the data container (`CScanStorage` and `CRawDataScanStorage` respectively). The numeric `CChannelGasConfPart.mass` values are the authoritative source.

### `.scn` — `CScanStorage.CBinary`

```
CScanStorage
  ├── n_points          number of scan points
  ├── n_traces          number of detector traces
  ├── trace_labels      string[n_traces]           human-readable labels e.g. "Mass 44.00 [C1]"
  ├── CGasConfiguration.p.objects
  │     └── CIntegrationUnitGasConfPart
  │           └── CChannelGasConfPart[N].mass      integer m/z for trace N
  └── CBinary
        ├── x           float[n_points]            scan-axis values (e.g. high-voltage steps)
        └── traces      double[n_traces][n_points]  detector intensities
```

### `.cf` — `CRawDataScanStorage.CBinary`

Each gas block (`CBlockDataContext`) contains one `CRawDataScanStorage` with the same `CBinary` layout as `.scn`. The mass assignments live in the top-level `CMethod.CGasConfiguration`:

```
CBlockData[0].objects
  └── CBlockDataContext[N].p.objects
        └── CBlockData.objects
              └── CRawDataScanStorage
                    ├── n_points
                    ├── n_traces
                    ├── trace_labels      string[n_traces]
                    └── CBinary
                          ├── x       float[n_points]
                          └── traces  double[n_traces][n_points]

CMethod.p.objects.CGasConfiguration.p.objects
  └── CIntegrationUnitGasConfPart.CChannelGasConfPart[N].mass
```

### `.dxf` — `CRawData[N].p.CEvalGCData`

One or more `CRawData` objects live inside each data `CBlockData`. Their `CEvalGCData` child carries the decoded arrays in a nested parent node. Mass assignments are in the method `CGasConfiguration`:

```
CContiniousFlowBlockData.p.objects
  └── CBlockData[N].objects
        └── CRawData[M].p
              └── CEvalGCData.p.p
                    ├── x       float[n_points]
                    └── traces  double[n_traces][n_points]

CContiniousFlowBlockData.p.objects.CBlockData[5].objects
  └── CMethod.p.objects.CGasConfiguration.p.objects
        └── CIntegrationUnitGasConfPart.CChannelGasConfPart[N].mass
```

### `.did` and `.caf` — `CDualInletRawData`

Dual-inlet files do not use the `x`/`traces` layout. Raw voltages are stored in `CDualInletRawData` as `CIntensityData` blocks (one per detector) organised by acquisition cycle. Mass assignments follow the same `CGasConfiguration` pattern:

| Extension | Path to `CDualInletRawData` | Path to `CChannelGasConfPart[N].mass` |
|-----------|----------------------------|---------------------------------------|
| `.did` | `CDualInletBlockData.p.objects.CDualInletRawData` | `CDualInletBlockData.p.objects.CMethod.p.objects.CGasConfiguration.p.objects.CIntegrationUnitGasConfPart.CChannelGasConfPart[N].mass` |
| `.caf` | `CBlockDataContext.p.objects.CDualInletRawData` | `CBlockDataContext.p.objects.CMethod.p.objects.CGasConfiguration.p.objects.CIntegrationUnitGasConfPart.CChannelGasConfPart[N].mass` |

## Resistor Values

Faraday cup feedback resistor values appear in two distinct locations in the JSON output, serving different purposes.

### Cup resistors — `CCupHardwarePart[N].resistor`

The configured (nominal) resistance for each detector cup is stored as an integer (in ohms) inside `CCupHardwarePart`. This array always covers **all physical cups** in the instrument collector array (typically 8–10 entries). Cups that are not installed or not connected carry the sentinel value `200` (200 Ω), which is far below any real feedback resistor and can be used to identify inactive cups. Values of `200000` (200 kΩ) appear for the H cup in hydrogen-measurement configurations, which requires a lower resistance to handle the large H₂⁺ beam current. Active cups carry round integer values (e.g. 300,000,000 = 300 MΩ; 30,000,000,000 = 30 GΩ).

The path to reach them runs through `CIntegrationUnitScanPart → CIntegrationUnitHardwarePart → CCupHardwarePart[N].resistor`. The root of that path depends on file type:

| Extension | Path prefix |
|-----------|-------------|
| `.scn` | `CScanStorage.CIntegrationUnitScanPart.p.CIntegrationUnitHardwarePart` |
| `.dxf` | `CContiniousFlowBlockData.p.objects.CBlockData[5].objects.CMethod.p.objects.CGasConfiguration.p.objects.CBasicScan.CIntegrationUnitScanPart.p.CIntegrationUnitHardwarePart` |
| `.did` | `CDualInletBlockData.p.objects.CMethod.p.objects.CGasConfiguration.p.objects.CBasicScan.CIntegrationUnitScanPart.p.CIntegrationUnitHardwarePart` |
| `.caf` | `CBlockDataContext.p.objects.CMethod.p.objects.CGasConfiguration.p.objects.CBasicScan.CIntegrationUnitScanPart.p.CIntegrationUnitHardwarePart` |
| `.cf` | `CMethod.p.objects.CGasConfiguration.p.objects.CBasicScan.CIntegrationUnitScanPart.p.CIntegrationUnitHardwarePart` |

The same `CCupHardwarePart` array is repeated inside several embedded method-part subtrees (e.g. `CICA_BasicMethodPart`, `CContiniousFlowStandardizationListMethodPart`, `CPeakFindMethodPart`) — these are copies of the same hardware configuration embedded within each method component and carry identical values.

### Calibrated cup resistors — `CEvalIntegrationUnitHWInfo[N].resistor`

The gain-calibrated resistance for each cup **used in this specific measurement** is stored as a float (in ohms) inside `CEvalIntegrationUnitHWInfo`. Each entry also carries `mass` (the m/z measured on that cup), `channel` (the integration-unit input channel number), and `cup` (the physical cup position number). These are instrument-specific hardware indices and do **not** directly correspond to the 0-based position in the `CCupHardwarePart` array.

The calibrated values are derived from the instrument's gain calibration routine with all DIO resistor switches already in their measurement position, so they reflect the actual resistance of whichever resistor is physically connected at measurement time. If a cup uses an alternate resistor bank (DIO switch in position 1), the calibrated value already reflects that alternate resistor — there is no separate stored calibration for each switch position. The calibrated values typically deviate from the nominal by up to ~1% (e.g. 297 MΩ calibrated vs. 300 MΩ nominal); in some configurations the deviation is larger.

The count of `CEvalIntegrationUnitHWInfo` entries does **not** match the number of active `CCupHardwarePart` entries — it covers only the cups gain-calibrated for the measurement in question. This number varies by gas and measurement type (e.g. 2 cups for H₂, 3 for CO₂, 7 for clumped-isotope CO₂). These values are present in all file types except `.scn`.

For accurate conversion of raw voltages to ion currents, use the calibrated `CEvalIntegrationUnitHWInfo.resistor` values rather than the nominal `CCupHardwarePart.resistor` values. The `mass`, `channel`, and `cup` fields on each entry identify which detector the value applies to.

The path runs through `CEvalIntegrationUnitHWInfoStore → CEvalIntegrationUnitHWInfoList → CEvalIntegrationUnitHWInfo[N].resistor`:

| Extension | Path prefix |
|-----------|-------------|
| `.dxf` | `CContiniousFlowBlockData.p.objects.CBlockData[5].objects.CMethod.p.objects.CEvalIntegrationUnitHWInfoStore.p.objects.CEvalIntegrationUnitHWInfoList.p.objects` |
| `.did` | `CDualInletBlockData.p.objects.CMethod.p.objects.CNumericValue.CEvalIntegrationUnitHWInfoStore.p.objects.CEvalIntegrationUnitHWInfoList.p.objects` |
| `.caf` | `CBlockDataContext.p.objects.CMethod.p.objects.CEvalIntegrationUnitHWInfoStore.p.objects.CEvalIntegrationUnitHWInfoList.p.objects` |
| `.cf` | `CMethod.p.objects.CEvalIntegrationUnitHWInfoStore.p.objects.CEvalIntegrationUnitHWInfoList.p.objects` |

### Resistor channel state — `CDioTransferPart[N].raw_value`

In `.scn` files the `CGasConfiguration` object contains a list of `CDioTransferPart` objects whose `p.p.v` name follows the pattern `"Resitor Channel N"` (note: Isodat's own typo). The `raw_value` field (0 or 1) is a digital I/O state flag indicating which resistor bank is switched in for each channel, not the resistance value itself.

# Related Documentation

- [Inheritance Hierarchy](isodat_inheritance_hierarchy.md) — full listing of all known C++ classes, their base classes, and which classes share a `Serialize` implementation.
- [Example: `.scn` structure tree](isodat_scn_structure_tree.md) — annotated object tree for a scan file, showing how `CScanStorage` nests hardware parts and gas configurations.
- [Example: `.dxf` structure tree](isodat_dxf_structure_tree.md) — annotated object tree for a continuous-flow file, showing the `CContiniousFlowBlockData` layout.
- [Example: `.did` structure tree](isodat_did_structure_tree.md) — annotated object tree for a dual-inlet file, showing the `CDualInletBlockData` layout.
