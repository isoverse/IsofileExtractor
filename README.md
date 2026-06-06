[![isoextract](https://github.com/isoverse/IsofileExtractor/actions/workflows/assembly.yaml/badge.svg?branch=main)](https://github.com/isoverse/IsofileExtractor/actions/workflows/assembly.yaml)

# isoextract

A self-contained command-line tool for extracting data from stable isotope ratio mass spectrometry (IRMS) binary data files. Supports multiple vendor software formats. Each input file is parsed and the extracted data is written to a JSON output file in the same folder.

## Supported file formats

| Extension | Measurement type | Produced by | Format docs |
|-----------|-----------------|-------------|-------------|
| `.dxf`    | Continuous flow          | Thermo Fisher Isodat      | [isodat_structure.md](docs/isodat_structure.md) |
| `.cf`     | Continuous flow (legacy) | Thermo Fisher Isodat      | [isodat_structure.md](docs/isodat_structure.md) |
| `.bch`    | Continuous flow          | SerCon Callisto           | [bch_structure.md](docs/bch_structure.md) |
| `.iarc`   | Continuous flow / dual inlet | Elementar IonOS (v2/v3)   | [iarc_larc_structure.md](docs/iarc_larc_structure.md) |
| `.larc`   | Continuous flow / dual inlet | Elementar LyticOS (v4)    | [iarc_larc_structure.md](docs/iarc_larc_structure.md) |
| `.imexp`  | Continuous flow / dual inlet | Thermo Fisher Qtegra  | [imexp_structure.md](docs/imexp_structure.md) |
| `.did`    | Dual inlet               | Thermo Fisher Isodat      | [isodat_structure.md](docs/isodat_structure.md) |
| `.caf`    | Dual inlet (legacy)      | Thermo Fisher Isodat      | [isodat_structure.md](docs/isodat_structure.md) |
| `.scn`    | Scan                     | Thermo Fisher Isodat      | [isodat_structure.md](docs/isodat_structure.md) |

## Usage

```
isoextract <file|dir> [...] [options]
```

One or more files or directories can be provided. Directories are searched recursively for files with supported extensions. Files are processed in parallel.

### Options

| Option | Description |
|--------|-------------|
| `--version`, `-v` | Print the version and exit |
| `--prettyJSON` | Pretty-print JSON output (number arrays are kept on one line) |
| `--log [path]` | Write a CSV summary of all processed files. Defaults to `isoextract.log` in the current directory; an explicit path can be provided: `--log results/run.log` |
| `--file-list <path>` | Read additional file/directory paths from a text file (one per line; lines starting with `#` are ignored) |
| `--dry-run` | Parse files without writing any JSON or issues log output. The `--log` CSV is still written normally, making `--dry-run --log` the recommended way to evaluate a batch before committing to a full run |

> **Recommended workflow:** run with `--dry-run --log` first to parse all files and capture any warnings or errors in the log, without touching the output `.json` or `.issues.log` files. Once satisfied, re-run without `--dry-run` to write the final output.

### Advanced options

| Option | Description |
|--------|-------------|
| `--unabridged` | Include verbose fields normally omitted: schema version numbers, app IDs, raw flags, etc. |
| `--objects` | (Isodat only) Write a `.objects.csv` output file for each input file, listing every deserialized C++ object with its byte offset, class name, schema version, and parent–child relationships |
| `--tree` | (Isodat only) Write a `.tree.txt` output file for each input file showing the object hierarchy as an indented tree |

### Exit code

`0` if all files were processed without errors, `1` if any file failed or was not found.

## Output files

For each input file `foo.dxf` the following files are written:

| File | Always? | Description |
|------|---------|-------------|
| `foo.dxf.json` | yes | Extracted data |
| `foo.dxf.issues.log` | only on warnings/errors | Plain-text list of warnings and the error message (if any) |
| `foo.dxf.objects.csv` | with `--objects` (Isodat only) | Per-object log (offset, class, version, hierarchy) |
| `foo.dxf.tree.txt` | with `--tree` (Isodat only) | Indented object tree |

### JSON structure

Every output file has a `meta` block at the top:

```json
{
  "meta": {
    "isoextract_version": "0.1.0.0",
    "file_type": "dxf",
    "file_size_bytes": 123456,
    "complete": true
  },
  ...
}
```

`complete: false` means parsing stopped early due to an error; the rest of the JSON contains whatever was extracted before the failure.

### Log file format

The CSV written by `--log` has one row per file:

```
file,success,duration_ms,error
"data/example.dxf",true,134,
"data/broken.dxf",false,12,"No reader registered for class 'CUnknown'"
```

## Examples

Process a single file:
```sh
isoextract sample.dxf
```

Process all files in a directory tree, pretty-printing the JSON:
```sh
isoextract /data/irms --prettyJSON
```

Process a directory and write a log:
```sh
isoextract /data/irms --log run.log
```

Process a hand-picked list:
```sh
isoextract --file-list batch.txt
```

Full diagnostic output (objects + tree + log):
```sh
isoextract /data/irms --objects --tree --prettyJSON --log
```

## Building from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```sh
# Development build (produces bin/release/isoextract.dll, run with dotnet)
make build

# Release build for all three runtimes (linux-x64, osx-x64, win-x64) via Docker
make build-all
```

Live-reload during development (rebuilds and reruns on every save):
```sh
make dev
```
