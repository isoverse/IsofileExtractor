PROJECT := src/IsofileExtractor.csproj
TEST_FILE := tests/data/scn

DOCKER    := mcr.microsoft.com/dotnet/sdk:8.0
UNAME     := $(shell uname -s)
ifeq ($(UNAME), Darwin)
  RUNTIME    := osx-x64
  EXECUTABLE := out/isoextract-$(RUNTIME)
else ifeq ($(UNAME), Linux)
  RUNTIME    := linux-x64
  EXECUTABLE := out/isoextract-$(RUNTIME)
else
  RUNTIME    := win-x64
  EXECUTABLE := out/isoextract-$(RUNTIME).exe
endif

.PHONY: dev build run version clean publish test check-docker build-docker build-all

# ── Development ───────────────────────────────────────────────────────────────

# Rebuild and rerun on every file save
dev:
	dotnet watch --project $(PROJECT) run -- $(TEST_FILE) --objects --tree --prettyJSON --log

# Build in release mode
build:
	dotnet build $(PROJECT) -c Release -o bin/release

# Run against the test file
run: build
	dotnet bin/release/isoextract.dll $(TEST_FILE)

# Print the assembly version
version: build
	dotnet bin/release/isoextract.dll --version

# Remove build artifacts
clean:
	dotnet clean $(PROJECT)
	rm -rf bin obj src/bin src/obj

# ── Testing ───────────────────────────────────────────────────────────────────

# Run all test files and move output to tests/output
test: build
	bash test.sh

# ── Release builds ────────────────────────────────────────────────────────────

# Self-contained binaries for all runtimes via Docker → out/isoextract-{linux,osx,win}-x64[.exe]
build-all: check-docker
	docker run --rm -v $(CURDIR):/app -w /app $(DOCKER) \
	  /app/build.sh project=/app output=/app/out

# Same as build-all but for the current OS runtime only
build-docker: check-docker
	docker run --rm -v $(CURDIR):/app -w /app $(DOCKER) \
	  /app/build.sh project=/app output=/app/out runtime=$(RUNTIME)

check-docker:
	@docker image inspect $(DOCKER) > /dev/null 2>&1 || \
	  (echo "Image '$(DOCKER)' not found. Pulling..." && docker pull $(DOCKER))
