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
#     /app/build.sh project=/app output=/app/out runtime=osx-x64

set -euo pipefail
SECONDS=0
echo ""
echo "--- STARTING BUILD SCRIPT ---"

# Defaults
project_folder="$PWD"
output_folder="$PWD/out"
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

# Export: flat output folder with per-RID isoextract + isosolfs binaries side-by-side.
# Names carry the RID (isoextract-<rid>, isosolfs-<rid>), so isoextract resolves its matching
# helper by mirroring its own filename. The proprietary isosolfs binaries are build inputs
# expected at assets/isosolfs/<rid>/ (git-ignored); isoextract builds fine without them.
mkdir -p "$output_folder"
for runtime in $runtimes; do
  echo "Packaging $runtime..."
  source_path="bin/Release/net8.0/$runtime/publish"
  suffix=""
  if [[ "$runtime" == win-* ]]; then
    suffix=".exe"
  fi

  cp "$source_path/isoextract$suffix" "$output_folder/isoextract-$runtime$suffix"

  isosolfs_src="$project_folder/assets/isosolfs/$runtime/isosolfs$suffix"
  if [[ -f "$isosolfs_src" ]]; then
    cp "$isosolfs_src" "$output_folder/isosolfs-$runtime$suffix"
  else
    echo "WARNING: isosolfs helper missing for $runtime ($isosolfs_src); .imexp extraction will not work in this build"
  fi

  if [[ "$runtime" == osx-* || "$runtime" == linux-* ]]; then
    chmod +x "$output_folder/isoextract-$runtime$suffix"
    [[ -f "$output_folder/isosolfs-$runtime$suffix" ]] && chmod +x "$output_folder/isosolfs-$runtime$suffix"
  fi
done

echo "--- COMPLETED IN ${SECONDS} SECONDS ---"
