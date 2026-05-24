#!/bin/bash
# Build IsodatReader as a self-contained single-file binary for one or more runtimes.
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
runtimes="linux-x64 osx-x64 win-x64"

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
  dotnet publish IsodatReader.csproj --runtime "$runtime" -c Release
  echo "Finished compiling $runtime"
done

# Export
mkdir -p "$output_folder"
for runtime in $runtimes; do
  echo "Moving executable for $runtime to output folder"
  source_path="bin/Release/net8.0/$runtime/publish"
  suffix=""
  if [[ "$runtime" == win-* ]]; then
    suffix=".exe"
  fi
  cp "$source_path/IsodatReader$suffix" "$output_folder/IsodatReader-$runtime$suffix"
  if [[ "$runtime" == osx-* || "$runtime" == linux-* ]]; then
    chmod +x "$output_folder/IsodatReader-$runtime$suffix"
  fi
done

echo "--- COMPLETED IN ${SECONDS} SECONDS ---"
