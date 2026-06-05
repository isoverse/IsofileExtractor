# SerCon Callisto CF-IRMS `.bch` Archive Structure

A `.bch` batch is a **directory**, not a single file. The directory name is the batch name (e.g. `MONTANA CARBON SAMPLES.bch`). All content lives in subdirectories:

```
<BatchName>.bch/
├── Results/
│   ├── <BatchName>.rec          # raw scan data (required)
│   ├── ReprocessedData.prn      # processed results table (required)
│   ├── ReprocessedData.MPC      # binary mirror of .prn (not parsed)
│   ├── ReprocessedExtra.prn     # extra columns table (not parsed)
│   └── Result1.dat              # additional output (not parsed)
└── Method/
    ├── Setups/                  # *.set per-analysis method definitions
    ├── Parameters/              # *.par analysis timing / peak schedules
    ├── Events/                  # *.evt valve/event sequences (+ *.spr sprite files)
    ├── Source/                  # *.src gas source configuration per gas species (not parsed)
    ├── Refs/                    # *.ref reference material definitions (not parsed)
    ├── Stds/                    # *.std standard definitions (not parsed)
    ├── Samples/                 # *.lst sample lists (not parsed)
    ├── PeakDetect/              # peak detection params (not parsed)
    ├── Graph/                   # graph display settings (not parsed)
    ├── XY/                      # window position/size files (not parsed)
    ├── Originals/               # backup copies of all Method files at run start
    ├── Definition.mth           # batch-level settings (not parsed)
    └── GasCorrections.def       # gas correction definitions (not parsed)
```

Each `.bch` folder also contains a top-level `<BatchName>.prn` (and sometimes `<BatchName>Extra.prn`) alongside the `Results/` directory. These are the original on-the-fly outputs written by Callisto during or immediately after acquisition, and their timestamps match the run date. `Results/ReprocessedData.prn` is a later replay/reprocessing output — produced by running Callisto's "Replay" function, possibly on a different machine or newer software version, sometimes years after the original run. The two files have identical column structure but differ slightly in numeric values (last few significant digits of floating-point results) and in how invalid rows are handled: the original tends to carry a carryover ratio value from the previous peak into blank/failed-acquisition rows, while `ReprocessedData.prn` zeros those fields out. `Results/ReprocessedData.prn` is the version used by `isoextract` — it lives alongside the `.rec` file in the structured `Results/` directory and its zeroing behaviour for invalid rows is more conservative.

`Method/Originals/` mirrors the full `Method/` tree and captures the state at run time. The files in `Method/` may be edited after acquisition.

---

## `Results/<BatchName>.rec` — Raw Scan Data

Plain-text file. One file per batch; contains all scan blocks for all analyses in sequence.

### File header (line 0)

Version tag. Callisto writes `v5.0>N~` or `v3.0` where `N` is an internal counter. The `>N~` suffix is static padding from the Callisto source (`InStr()` substring match is used to read it back); strip at `>` to get the clean version string.

### Block structure

Each block starts with a marker line and a fixed header, then raw scan data:

```
<type>           # "S" = Sample, "R" = Reference, "B" = Blank/background
<name>           # analysis name (may encode dataset_id as _P_<id>_G_)
<weight>         # float, mg (may be 0 if not entered)
<method>         # method name, e.g. "NCS" or "NCS.set" (strip .set if present)
<row_width>      # number of data columns per scan
<scan_count>     # number of scans expected
[~<scan_id>]     # v5.0 only: scan identifier line beginning with "~"
<scan data …>
```

### v3.0 scan data

One value per line. Columns repeat in order `[beam1, beam2, …, beamN, time_s]` — the **last** column is time in seconds. Values for all columns of one scan are laid out consecutively; `row_width` includes the time column so `n_beams = row_width - 1`. Blocks end when a block-marker line or EOF is encountered.

### v5.0 scan data

One scan per line, tab-separated. Columns are `[beam1_a, beam2_a, …, beamN_a, extra0, extra1, extra2, extra3, time_counter, …]`. All leading float columns are beam currents (amperes); integer columns follow. `extra[4]` (0-indexed from the first extra column, i.e. index `n_beams + 4`) is a hardware time counter.

The counter is a 10 Hz absolute clock shared across all blocks in the run (1 tick = 0.1 s). At 1 Hz data collection the counter increments by 10 per scan; at 10 Hz by 1 per scan. The counter is stored as a **uint16** and wraps at 65536 (~109 minutes at 1 Hz); `isoextract` unwraps it before computing time.

The hardware discards the first 2 scans of each block (stabilisation delay) before writing to the file, so `counter[0]` is already 2 × `dt_ticks` past block start. To produce block-relative elapsed time starting at 2 × dt — matching the convention in v3.0 files where `time_s[0] ≈ 2 × dt` — `isoextract` adds that offset back:

```
dt_ticks  = counter[1] − counter[0]      (scan period in counter ticks)
time_s[i] = (counter_unwrapped[i] − counter_unwrapped[0] + 2 × dt_ticks) / 10.0
```

Both `time_s` (computed, block-relative seconds) and `time_counter_raw` (the original uint16 values as stored in the file, before unwrapping) are emitted in `traces` for v5.0 blocks.

---

## `Results/ReprocessedData.prn` — Processed Results Table

Plain-text, tab-separated. Produced by Callisto's post-processing ("replay") step.

### File layout

```
line 0:   system description  (e.g. "SerCon 'Callisto CF-IRMS' system : IN1215 Gulf Bio")
line 1:   timestamp           (HH:MM:SS\tMM-DD-YYYY)
line 2:   blank
line 3:   "Data from file : <absolute Windows path>"   (not used in parsing)
line 4:   section label       (e.g. "Un-Drift Corrected")
line 5:   column headers      (tab-separated)
line 6:   column units        (tab-separated; leading tabs align with structural cols)
lines 7…: data rows           (tab-separated, first field = N)
```

When drift correction is enabled a second section follows immediately after the first, with a label line (e.g. "Drift Corrected"), its own headers/units lines, and data rows in the same format.

### Structural columns (always present)

| Column | Content |
|--------|---------|
| `N` | sequential integer row index |
| `Type` | block type (S / R / B) |
| `Name` | analysis name |
| `Weight/Vol` | weight in µg (may be 0) |
| `Status` | quality flag; leading `#` is stripped |

### Data columns (measurement results)

All remaining columns are measurement outputs. Each gas species produces a group of columns. For an NCS (N + C + S) batch the column pattern is:

```
Beam Area   [no unit]    total beam area for the gas species
N (Sam)     [ug]         nitrogen amount
15N (Sam)   [DeltaAir]   δ15N value
None (Sam)  [blank]      second isotope slot (empty if only one delta)
Ratio 1                  raw ratio beam2/beam1
Ratio 2                  raw ratio beam3/beam1
Beam Area   …            next gas species
C (Sam)     [ug]         …
13C (Sam)   [DeltaPDB]
…
```

The `*` prefix on a units string (e.g. `*DeltaCDT`) indicates the measurement did not pass a quality check in Callisto.

---

## `Method/Setups/*.set` — Method Setup

One `.set` file per analysis method. 15-line plain text (lines[11..14] present only in newer format):

| Line (0-indexed) | Field | Notes |
|---------|-------|-------|
| 0  | `description` | free text label |
| 1  | `reference_file` | basename of `.ref` used for this method |
| 2  | `analysis_timing_file` | basename of `.par` (without extension) |
| 3  | `run_mode` | `Normal`, `Linearity`, etc. |
| 4  | `event_sequence_file` | basename of `.evt`; `NONE` / `NONE.evt` means no events |
| 5  | `auto_sampler_sequence_file` | basename of `.spr` autosampler file |
| 6  | `multicollector_file` | basename of `.mcp`; `Default.mcp` uses the shared Setups file |
| 7  | `peak_centre_file` | basename of `.pcn` |
| 8  | `line_9` | unknown |
| 9  | `data_rate_hz` | `0` = 1 Hz, `1` = 10 Hz |
| 10 | `line_11` | unknown |
| 11 | `line_12` | unknown (optional) |
| 12 | `line_13` | unknown (optional) |
| 13 | `element_by_tcd` | `True` / `False` |
| 14 | `line_15` | unknown (optional) |

---

## `Method/Parameters/*.par` — Analysis Timing

Defines the peak schedule for one analysis run. The file is divided into three sections:

### Section 1 — file header (lines 0–19)

| Line | Field |
|------|-------|
| 0 | `description` |
| 1–13 | various (not decoded) |
| 14 | `total_time_s` — total run duration in seconds |
| 15–17 | unknown |
| 18 | `n_peaks` — number of peak entries |
| 19 | `n_peaks_total` (usually equals `n_peaks`) |

### Section 2 — per-peak records (lines 20 + p×20 for each peak p)

Each peak occupies up to 20 lines (6 timing + 14 data fields + 1 optional):

| Offset | Field |
|--------|-------|
| +0 | `base1_start_s` |
| +1 | `base1_end_s` |
| +2 | `integrate_start_s` |
| +3 | `integrate_end_s` |
| +4 | `base2_start_s` |
| +5 | `base2_end_s` |
| +6 | `active` — `True` / `False` |
| +7 | `type` — `Sample` / `Reference` |
| +8 | `isotope1_idx` — integer index; maps to isotope name (see below) |
| +9 | gas source name (empty; populated in section 3 instead) |
| +10 | unknown integer |
| +11 | unknown integer |
| +12 | `gas_idx` (skip) |
| +13 | `isotope2_idx` — same mapping |
| +14 | `mode1` — delta reference scale, e.g. `DeltaAir`, `DeltaPDB`, `DeltaCDT` |
| +15 | `mode2` — second isotope scale, or `0` if absent |
| +16 | unknown integer |
| +17 | unknown integer |
| +18 | `linear_regression` — `True` / `False` |
| +19 | `group` — integer grouping index |
| +20 | `auto_dilute` — `True` / `False` (optional; present in newer format) |

#### Isotope index mapping

| Index | Isotope |
|-------|---------|
| 0 | 15N |
| 1 | 13C |
| 2 | 18O |
| 7 | 34S |
| 8 | 2H |

### Section 3 — global section (48 fixed lines then n_peaks × 2 gas/time pairs)

After the per-peak block, 48 lines of unknown global settings are followed by one pair of lines per peak:

```
<gas_species>    # e.g. "N2", "CO2", "SO2", "HD"
<at_time_s>      # integer; time offset in seconds to align this peak when stitching analyses
```

`gas_species` is the gas name that links this peak to a `.src` file. It indicates which gas species was measured for the peak (e.g. `N2`, `CO2`, `SO2`, `HD`).

---

## `Method/Events/*.evt` — Valve Event Sequence

Defines the valve open/close schedule for a run. Two format versions:

**Version 1** (no version header):
```
<description>
<total_run_time_s>
<n_events>
[for each event:]
  <time_s>
  <event_command>   # e.g. "E 8 VALVE ON"
  <side>            # "L" or "R"
```

**Version 2** (begins with `Version 2`):
```
Version 2
<description>
<total_run_time_s>
<n_events>
[for each event:]
  <time_s>
  <event_command>
  <side>
  <comment>         # optional label
```

Valve commands reference hardware valve numbers (e.g. `E 8 VALVE ON` opens valve 8). The `!` prefix on the first event is a Callisto convention marking the initial state.

---

## `Method/Setups/MultiCollector_A.mcp` — Collector Configuration

Defines which Faraday cups are active and their resistor/amplifier configuration. One shared `.mcp` file per batch; individual `.set` files reference it via `multicollector_file`.

### File header (lines 0–4)

```
1
False/True
Default
1
<format>          # 3 or 4
```

### Format 3 — each beam: mass token + 16 tokens

Layout per beam (17 lines total):

```
<mass>            # calibration-gas mass (integer); not a stable assignment — omitted from output
<enabled>
<beam_num>
<slot>
<res_type>
<denominator_beam_num>   # 0 = self (reference beam)
<res_val1>
<res_val2>
<color_fg>
<color_bg>
<flag2>           # True if res_val2 is active
<usage_type>      # I-Ratio / P-Ratio / P-only / Spare
<label1>…<label4>
```

### Format 4 — each beam: 15 tokens, optional trailing mass token

```
<enabled>         # True/False — beam start marker
<beam_num>
<slot>
<res_type>
<denominator_beam_num>
<res_val1>
<res_val2>
<color_fg>
<color_bg>
<flag2>
<usage_type>
<label1>…<label4>
[<mass>]          # optional; present for some beams, absent for others
```

### Resistor types

| `res_type` | Meaning |
|-----------|---------|
| 1 | Standard-gain I-type resistor |
| 2 | P-type (high-gain) |
| 3 | High-ohm I-type |
| 4 | P-only (no ratio output) |

`res_val1` and `res_val2` are stored in units of 1/1,000,000 Ω (i.e. multiply by 10⁶ to get ohms). `flag2 = True` means `res_val2` is the active resistance.

### Note on beam mass assignments

The `.mcp` file contains a `mass` field in format 3 and optionally in format 4. This field reflects the calibration-gas mass used during the most recent peak-centre calibration — it is **not** a stable per-beam property and is inconsistent across batches and formats. **The mass field is therefore not included in the JSON output.**

The actual mass monitored by each beam is determined implicitly by the gas species and the delta isotope being calculated:

| Delta value | Gas | Beam ratio | Mass ratio |
|-------------|-----|------------|------------|
| δ¹⁵N | N₂ | beam2 / beam1 | 29 / 28 |
| δ¹³C | CO₂ | beam2 / beam1 | 45 / 44 |
| δ¹⁸O | CO₂ | beam3 / beam1 | 46 / 44 |
| δ³⁴S | SO₂ | beam2 / beam1 | 66 / 64 |
| δ²H | H₂/HD | beam2 / beam1 | 3 / 2 |

These ratios are fixed by the SerCon Callisto instrument design (3-cup Faraday array with beam1 always on the major isotope). The `Ratio 1` and `Ratio 2` columns in the `.prn` output correspond to beam2/beam1 and beam3/beam1 respectively.

For N₂ and CO₂ the cups sit on consecutive masses (Δm = 1). For SO₂ the magnetic field is tuned so that beam1 lands on mass 64 (³²SO₂) and beam2 lands on mass 66 (³⁴SO₂), skipping mass 65 (³³SO₂). This was confirmed empirically: raw `Ratio 1` values for SO₂ scale correctly with δ³⁴S and have the right order of magnitude for a 66/64 beam-current ratio (~0.045 for CDT-equivalent samples). The `Ratio 2` column for SO₂ does not correspond to a clean isotope ratio and the Callisto software marks the second-isotope delta slot as `None` in its output.

---

## Linking the files together

```
.rec blocks   →  method field   →  .set file      (Method/Setups/<method>.set)
.set          →  analysis_timing_file  →  .par    (Method/Parameters/<name>.par)
.set          →  event_sequence_file   →  .evt    (Method/Events/<name>.evt)
.set          →  multicollector_file   →  .mcp    (Method/Setups/MultiCollector_A.mcp)
.prn rows     →  N column              →  .rec block index (1-based)
```

The `.rec` and `.prn` row order must be aligned: the N-th data row in the `.prn` corresponds to the N-th scan block in the `.rec`.

---

## `isoextract` JSON output structure for `.bch`

`isoextract` writes one `<BatchName>.bch.json` file beside the `.bch` directory. Top-level keys, in order:

```
meta
header
collectors          (omitted if no .mcp found)
methods             (omitted if no .set files found)
timings             (omitted if no .par files found)
events              (omitted if no .evt files found)
data
results
```

### `meta`

| Field | Type | Content |
|-------|------|---------|
| `isoextract_version` | string | tool version |
| `file_type` | string | `"bch"` |
| `file_size_bytes` | integer | combined size of all files in the `.bch` directory |
| `complete` | bool | `true` if parsing completed without error |

### `header`

| Field | Content |
|-------|---------|
| `source` | relative path to `Results/ReprocessedData.prn` |
| `system_description` | line 0 of the `.prn` (e.g. `"SerCon 'Callisto CF-IRMS' system : IN1215 Gulf Bio"`) |
| `timestamp` | line 1 of the `.prn` (`HH:MM:SS\tMM-DD-YYYY`) |

### `collectors`

Parsed from `Method/Setups/MultiCollector_A.mcp`.

| Field | Content |
|-------|---------|
| `source` | relative path to the `.mcp` file |
| `format` | integer MCP format version (3 or 4) |
| `beams` | array of beam objects (see below) |

Each beam object:

| Field | Type | Content |
|-------|------|---------|
| `beam_num` | int | beam number (1-based) |
| `enabled` | bool | whether this collector is active |
| `slot` | int | physical slot in the collector array |
| `res_type` | int | resistor type (1=I-type, 2=P-type, 3=high-ohm I-type, 4=P-only) |
| `resistance_1_ohm` | int | primary resistor in ohms |
| `resistance_2_ohm` | int | secondary resistor in ohms |
| `active_resistance_ohm` | int | whichever resistance is currently active (`flag2` selects) |
| `usage_type` | string | omitted if `"Spare"`; otherwise e.g. `"I-Ratio"`, `"P-Ratio"` |
| `denominator_beam_num` | int | omitted if 0 (self = reference beam) |

### `methods`

Array; one entry per distinct method name used by any `.rec` block. Each element:

| Field | Content |
|-------|---------|
| `method` | method name without `.set` extension (e.g. `"NCS"`) |
| `source` | relative path to the `.set` file |
| `description` | free-text label |
| `reference_file` | `.ref` basename |
| `analysis_timing_file` | `.par` basename (without extension) |
| `run_mode` | e.g. `"Normal"`, `"Linearity"` |
| `event_sequence_file` | `.evt` basename; `"NONE"` if absent |
| `auto_sampler_sequence_file` | `.spr` basename |
| `multicollector_file` | `.mcp` basename |
| `peak_centre_file` | `.pcn` basename |
| `data_rate_hz` | integer: `1` or `10` |
| `element_by_tcd` | bool (newer format only) |

### `timings`

Array; one entry per distinct `.par` file referenced by the methods. Each element:

| Field | Content |
|-------|---------|
| `timing` | timing name without `.par` extension (e.g. `"NCS"`) |
| `source` | relative path to the `.par` file |
| `description` | free-text label |
| `total_time_s` | total run duration in seconds |
| `peaks` | array of peak objects (see below) |

Each peak object contains timing windows, isotope info, and gas species:

| Field | Type | Content |
|-------|------|---------|
| `gas_species` | string | gas measured (e.g. `"N2"`, `"CO2"`, `"SO2"`, `"HD"`) |
| `active` | bool | whether this peak is enabled |
| `type` | string | `"Sample"` or `"Reference"` |
| `base1_start_s` … `base2_end_s` | int | baseline and integration window boundaries |
| `mode1` | string | delta reference scale (e.g. `"DeltaAir"`, `"DeltaPDB"`, `"DeltaCDT"`) |
| `isotope1` | string | e.g. `"15N"`, `"13C"`, `"34S"` (omitted if index unrecognised) |
| `isotope2` | string | second isotope (omitted if absent) |
| `mode2` | string | second isotope scale (omitted if absent or `"0"`) |
| `linear_regression` | bool | |
| `group` | int | grouping index |
| `at_time_s` | int | time offset for stitching analyses |
| `auto_dilute` | bool | optional; present in newer format only |

### `events`

Array; one entry per distinct `.evt` file referenced by methods. Each element:

| Field | Content |
|-------|---------|
| `event` | event sequence name without `.evt` extension (e.g. `"NCS"`) |
| `source` | relative path to the `.evt` file |
| `total_run_time_s` | total run duration in seconds |
| `description` | optional free-text label |
| `events` | array of `{ time_s, event, side, [comment] }` objects |

### `data`

Raw scan traces from the `.rec` file.

| Field | Content |
|-------|---------|
| `source` | relative path to the `.rec` file |
| `version` | version string from the `.rec` header (e.g. `"v5.0"`, `"v3.0"`) |
| `blocks` | array of block objects (see below) |

Each block object:

| Field | Type | Content |
|-------|------|---------|
| `type` | string | `"S"` / `"R"` / `"B"` |
| `name` | string | analysis name from the `.rec` header |
| `method` | string | method name (`.set` suffix stripped) |
| `n_scans` | int | number of scans actually read |
| `weight` | float | sample weight in mg (omitted if 0 / absent) |
| `scan_id` | string | v5.0 scan identifier line (omitted for v3.0) |
| `acquisition_duration_s` | float | `max(time_s) − min(time_s)` |
| `traces` | object | per-beam and time arrays (see below) |

`traces` keys:
- `time_s` — block-relative elapsed seconds (float array); present for both v3.0 and v5.0
- `time_counter_raw` — original uint16 hardware counter values as stored in the `.rec` file, before overflow unwrapping (integer array; v5.0 only)
- `beam1_a` … `beamN_a` — beam currents in amperes

For v3.0 files `time_s` is stored directly in the `.rec` file (first scan ≈ 2 × dt). For v5.0 files it is derived from the hardware counter with overflow unwrapping and the 2-scan offset added back, so the first scan is also at 2 × dt (see the `.rec` v5.0 section above).

### `results`

Columnar measurement results from `Results/ReprocessedData.prn`.

| Field | Content |
|-------|---------|
| `source` | relative path to `ReprocessedData.prn` |
| `columns` | array of column objects |

Each column object:

| Field | Content |
|-------|---------|
| `label` | column name from the `.prn` header |
| `units` | units string (omitted if blank); `*`-prefixed means quality check failed |
| `values` | array of values (number or string; `null` for missing) |
| `values_drift_corrected` | array from the second `.prn` section (omitted if no drift section) |

Structural columns (`id`, `name`, `type`, `dataset_id`, `weight`, `status`) always appear first, followed by all measurement columns in `.prn` order. Because multiple gas sections repeat column names like `"Ratio 1"` and `"Beam Area"`, columns are preserved in full — duplicate names appear as separate column objects.

---

## Files not parsed by `isoextract`

| Path pattern | Purpose |
|-------------|---------|
| `Method/Source/*.src` | Ion source settings per gas species (type code, pressures, voltages) |
| `Method/Refs/*.ref` | Reference material δ-values used for normalisation |
| `Method/Stds/*.std` | Working standard definitions |
| `Method/Samples/*.lst` | Sample list / run queue |
| `Method/PeakDetect/*.def` | Automatic peak detection parameters |
| `Method/Graph/*.def` | Graph axis and display settings |
| `Method/XY/*` | GUI window positions |
| `Method/Definition.mth` | Batch-level method envelope |
| `Method/GasCorrections.def` | Gas-specific isotope correction factors |
| `Method/Originals/` | Pre-run backup of all method files |
| `Results/ReprocessedData.MPC` | Binary mirror of `.prn` (proprietary format) |
| `Results/ReprocessedExtra.prn` | Additional computed columns |
