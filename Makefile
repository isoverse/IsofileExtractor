PROJECT := src/IsofileExtractor.csproj

DOCKER    := mcr.microsoft.com/dotnet/sdk:8.0
UNAME     := $(shell uname -s)
ifeq ($(UNAME), Darwin)
  RUNTIME    := osx-x64
  EXECUTABLE := dist/isoextract-$(RUNTIME)
else ifeq ($(UNAME), Linux)
  RUNTIME    := linux-x64
  EXECUTABLE := dist/isoextract-$(RUNTIME)
else
  RUNTIME    := win-x64
  EXECUTABLE := dist/isoextract-$(RUNTIME).exe
endif

.PHONY: dev build run version clean publish test check-docker build-docker build-all

# ── Development ───────────────────────────────────────────────────────────────

# Rebuild and rerun on src file save
TEST_FILE := tests/data/cdos
dev:
	dotnet watch --project $(PROJECT) run -- $(TEST_FILE) --objects --tree --unabridged --prettyJSON --log

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
	rm -rf bin obj src/bin src/obj dist out

# ── Testing ───────────────────────────────────────────────────────────────────

# Run all test files and move output to tests/output
test: build
	bash test.sh

# ── Release builds ────────────────────────────────────────────────────────────

# Self-contained, single-file isoextract binaries for every runtime via Docker, into
# dist/isoextract-<rid>[.exe]. The isosolfs helper is NOT bundled — it ships in its own
# release; drop a matching isosolfs-<rid> next to isoextract (or use --isosolfs-path).
build-all: check-docker
	docker run --rm -v $(CURDIR):/app -w /app $(DOCKER) \
	  /app/build.sh project=/app output=/app/dist

# Same as build-all but for the current OS runtime only
build-docker: check-docker
	docker run --rm -v $(CURDIR):/app -w /app $(DOCKER) \
	  /app/build.sh project=/app output=/app/dist runtime=$(RUNTIME)

check-docker:
	@docker image inspect $(DOCKER) > /dev/null 2>&1 || \
	  (echo "Image '$(DOCKER)' not found. Pulling..." && docker pull $(DOCKER))
