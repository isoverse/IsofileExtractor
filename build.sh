#!/bin/bash
# Build IsofileExtractor as a self-contained single-file binary for one or more runtimes.
# Designed to work both as a local call and via the dotnet docker image.
#
# Local usage:
#   bash build.sh
#   bash build.sh runtime=osx-x64
#
# Docker usage:
#   docker pull mcr.microsoft.com/dotnet/sdk:8.0
#   docker run --rm -v $PWD:/app -w /app mcr.microsoft.com/dotnet/sdk:8.0 \
#     /app/build.sh project=/app output=/app/dist runtime=osx-x64

set -euo pipefail
SECONDS=0
echo ""
echo "--- STARTING BUILD SCRIPT ---"

# Defaults
project_folder="$PWD"
output_folder="$PWD/dist"
runtimes="linux-x64 linux-arm64 osx-x64 osx-arm64 win-x64 win-arm64"

for arg in "$@"; do
  case $arg in
    project=*)  project_folder="${arg#*=}" ;;
    output=*)   output_folder="${arg#*=}"  ;;
    runtime=*)  runtimes="${arg#*=}"       ;;
    *) echo "Unknown option: $arg"; exit 1 ;;
  esac
done

if [[ -z "$project_folder" || -z "$output_folder" ]]; then
  echo "Error: project and output are required."
  echo "Usage: $0 project=<folder> output=<folder> [runtime=<runtime>]"
  exit 1
fi

# Build
cd "$project_folder/src"
for runtime in $runtimes; do
  echo "Compiling $runtime..."
  dotnet publish IsofileExtractor.csproj --runtime "$runtime" -c Release
  echo "Finished compiling $runtime"
done

# Export: flat output folder with per-RID isoextract binaries (isoextract-<rid>[.exe]).
# The isosolfs helper is distributed separately (it lives in its own GitHub release) and is
# NOT bundled here. To enable .imexp extraction, drop a matching isosolfs-<rid>[.exe] next to
# isoextract, or point isoextract at it with --isosolfs-path.
mkdir -p "$output_folder"
for runtime in $runtimes; do
  echo "Packaging $runtime..."
  source_path="bin/Release/net8.0/$runtime/publish"
  suffix=""
  if [[ "$runtime" == win-* ]]; then
    suffix=".exe"
  fi

  cp "$source_path/isoextract$suffix" "$output_folder/isoextract-$runtime$suffix"

  if [[ "$runtime" == osx-* || "$runtime" == linux-* ]]; then
    chmod +x "$output_folder/isoextract-$runtime$suffix"
  fi
done

echo "--- COMPLETED IN ${SECONDS} SECONDS ---"
