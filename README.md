[![isoextract](https://github.com/isoverse/IsofileExtractor/actions/workflows/assembly.yaml/badge.svg?branch=main)](https://github.com/isoverse/IsofileExtractor/actions/workflows/assembly.yaml)

# isoextract

A self-contained command-line tool for extracting data from Thermo Fisher isodat binary files. Each input file is parsed and the extracted data is written to a JSON output file in the same folder.

## Supported file formats

| Extension | Instrument type |
|-----------|----------------|
| `.dxf`    | Continuous flow (GC-IRMS) |
| `.cf`     | Continuous flow (legacy) |
| `.did`    | Dual inlet |
| `.caf`    | Dual inlet (legacy) |
| `.scn`    | Scan |

## Usage

```
isoextract [options] <file|dir> [...]
```

One or more files or directories can be provided. Directories are searched recursively for files with supported extensions. Files are processed in parallel.

### Options

| Option | Description |
|--------|-------------|
| `--version`, `-v` | Print the version and exit |
| `--prettyJSON` | Pretty-print JSON output (number arrays are kept on one line) |
| `--unabridged` | Include verbose fields normally omitted: schema version numbers, app IDs, raw flags, etc. |
| `--log [path]` | Write a CSV summary of all processed files. Defaults to `isoextract.log` in the current directory; an explicit path can be provided: `--log results/run.log` |
| `--file-list <path>` | Read additional file/directory paths from a text file (one per line; lines starting with `#` are ignored) |
| `--objects` | Write a `.objects.csv` output file for each input file, listing every deserialized C++ object with its byte offset, class name, schema version, and parent–child relationships |
| `--tree` | Write a `.tree.txt` output file for each input file showing the object hierarchy as an indented tree |
| `--dry-run` | Parse files without writing the JSON output. All other output (`--log`, `--objects`, `--tree`, issues logs) is still written normally |

### Exit code

`0` if all files were processed without errors, `1` if any file failed or was not found.

## Output files

For each input file `foo.dxf` the following files are written:

| File | Always? | Description |
|------|---------|-------------|
| `foo.dxf.json` | yes | Extracted data |
| `foo.dxf.issues.log` | only on warnings/errors | Plain-text list of warnings and the error message (if any) |
| `foo.dxf.objects.csv` | with `--objects` | Per-object log (offset, class, version, hierarchy) |
| `foo.dxf.tree.txt` | with `--tree` | Indented object tree |

### JSON structure

Every output file has a `meta` block at the top:

```json
{
  "meta": {
    "reader_version": "0.1.0.0",
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
data/example.dxf,true,134,
data/broken.dxf,false,12,No reader registered for class 'CUnknown'
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
