PROJECT := src/IsodatReader.csproj
TEST_FILE := tests/data/continuous_flow_example.dxf

.PHONY: dev build run clean publish version

# Watch for file changes and rerun automatically
dev:
	dotnet watch --project $(PROJECT) run -- $(TEST_FILE) --objects --tree --unabridged

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

# Publish self-contained single-file binary
publish:
	dotnet publish $(PROJECT) -c Release -o out --self-contained true
