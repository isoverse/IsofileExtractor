PROJECT := src/IsodatReader.csproj
#TEST_FILE := tests/data/continuous_flow_example.dxf
TEST_FILE := tests/data/continuous_flow_example.cf
#TEST_FILE := tests/data/BF22430__MethodEdit_F8_cHEX_130ng_DB5HT_He_10ng@ul.dxf
#TEST_FILE := tests/data/BF22430__F8_cHEX_130ng_DB5HT_He_10ng@ul.dxf
#TEST_FILE := tests/data/full_scan_example.scn

DOCKER := mcr.microsoft.com/dotnet/sdk:8.0
UNAME  := $(shell uname -s)
ifeq ($(UNAME), Darwin)
  RUNTIME    := osx-x64
  EXECUTABLE := out/IsodatReader-$(RUNTIME)
else ifeq ($(UNAME), Linux)
  RUNTIME    := linux-x64
  EXECUTABLE := out/IsodatReader-$(RUNTIME)
else
  RUNTIME    := win-x64
  EXECUTABLE := out/IsodatReader-$(RUNTIME).exe
endif

.PHONY: dev build run clean publish version check-docker build-docker build-all

# Watch for file changes and rerun automatically
dev:
#	dotnet watch --project $(PROJECT) run -- $(TEST_FILE) --objects --tree --prettyJSON
	dotnet watch --project $(PROJECT) run -- $(TEST_FILE) --objects --tree --unabridged --prettyJSON

# Build in release mode
build:
	dotnet build $(PROJECT) -c Release -o bin/release

# Run against the test file
run: build
	dotnet bin/release/IsodatReader.dll $(TEST_FILE)

# Print assembly version
version: build
	dotnet bin/release/IsodatReader.dll --version

# Remove build artifacts
clean:
	dotnet clean $(PROJECT)
	rm -rf bin obj src/bin src/obj

# Publish self-contained single-file binary (local runtime only)
publish:
	dotnet publish $(PROJECT) -c Release -o out --self-contained true

# Check that the dotnet docker image is available, pull if missing
check-docker:
	@docker image inspect $(DOCKER) > /dev/null 2>&1 || \
	  (echo "Image '$(DOCKER)' not found. Pulling..." && docker pull $(DOCKER))

# Build for the current OS runtime via docker → out/IsodatReader-<runtime>[.exe]
build-docker: check-docker
	docker run --rm -v $(CURDIR):/app -w /app $(DOCKER) \
	  /app/build.sh project=/app output=/app/out runtime=$(RUNTIME)

# Build all runtimes (linux-x64, osx-x64, win-x64) via docker
build-all: check-docker
	docker run --rm -v $(CURDIR):/app -w /app $(DOCKER) \
	  /app/build.sh project=/app output=/app/out
