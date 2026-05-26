#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

INPUT_DIR="tests/data"
OUTPUT_DIR="tests/output"
LOG_FILE="isoextract.log"
DLL="bin/release/isoextract.dll"

if [[ ! -f "$DLL" ]]; then
    echo "Error: $DLL not found — run 'make build' first" >&2
    exit 1
fi

mkdir -p "$OUTPUT_DIR"

# Run extractor; don't abort on non-zero — failures are detected via the log
dotnet "$DLL" "$INPUT_DIR" --tree --objects --prettyJSON --log || true

# Move all generated sidecar files preserving subdirectory structure
find "$INPUT_DIR" -type f \( \
    -name "*.json" -o \
    -name "*.objects.csv" -o \
    -name "*.tree.txt" -o \
    -name "*.issues.log" \
\) | while read -r f; do
    rel="${f#"$INPUT_DIR"/}"
    dest="$OUTPUT_DIR/$rel"
    mkdir -p "$(dirname "$dest")"
    mv "$f" "$dest"
done

# Move run log
[[ -f "$LOG_FILE" ]] && mv "$LOG_FILE" "$OUTPUT_DIR/$LOG_FILE"

# Report results
n_total=$(( $(wc -l < "$OUTPUT_DIR/$LOG_FILE") - 1 ))  # subtract header
n_fail=$(grep -c ",false," "$OUTPUT_DIR/$LOG_FILE" || true)
n_ok=$(( n_total - n_fail ))

echo "$n_ok/$n_total files parsed successfully. Output in $OUTPUT_DIR/"

if [[ "$n_fail" -gt 0 ]]; then
    echo "" >&2
    echo "ERROR: $n_fail file(s) failed:" >&2
    awk -F, 'NR > 1 && $2 == "false" { print "  FAIL: " $1 " — " $4 }' "$OUTPUT_DIR/$LOG_FILE" >&2
    exit 1
fi
