#!/usr/bin/env bash
set -euo pipefail
# ============================================================================
# Local CI Pipeline Configurations
# ============================================================================
SOLUTION="HarpyEngine.slnx"
PROJECT_NAME="Sandbox"
CONFIGURATION="Release"
OUTPUT_DIR="./local-ci-artifacts"
RESTORE_STAMP_DIR="./.build-cache"
RESTORE_STAMP="${RESTORE_STAMP_DIR}/restore.sha256"

# Detect available cores so restore/build/publish all fan out across them
# instead of running single-threaded (works on Linux, macOS, and WSL).
if command -v nproc &> /dev/null; then
    CPU_CORES=$(nproc)
elif command -v sysctl &> /dev/null; then
    CPU_CORES=$(sysctl -n hw.ncpu)
else
    CPU_CORES=4
fi
MSBUILD_PARALLEL_ARGS=(-m:"$CPU_CORES" -p:UseSharedCompilation=true)

echo "=================================================================="
echo "Harpy build"
echo "=================================================================="

# ----------------------------------------------------------------------------
# Rider's bundled terminal doesn't inherit the shell PATH, so dotnet
# often isn't found even when it's installed. Fall back to known locations.
# ----------------------------------------------------------------------------
if ! command -v dotnet &> /dev/null; then
    echo "dotnet not found on PATH."

    # Check your specific local home directory installation path
    LOCAL_DOTNET="$HOME/.dotnet"

    if [ -f "$LOCAL_DOTNET/dotnet" ]; then
        echo "Detected local SDK at $LOCAL_DOTNET. Injecting paths..."
        export DOTNET_ROOT="$LOCAL_DOTNET"
        export PATH="${PATH}:${LOCAL_DOTNET}"
    # Fallback to standard Arch system path just in case
    elif [ -f "/usr/share/dotnet/dotnet" ]; then
        echo "Detected system SDK at /usr/share/dotnet. Injecting paths..."
        export DOTNET_ROOT="/usr/share/dotnet"
        export PATH="${PATH}:${DOTNET_ROOT}"
    else
        echo "Error: .NET SDK could not be found anywhere."
        echo "Rider console is isolated. Please run this script in an external terminal."
        exit 1
    fi
fi

# Fresh cleanup of previous artifacts (restore cache is kept on purpose)
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR/linux-x64"
mkdir -p "$RESTORE_STAMP_DIR"

# ----------------------------------------------------------------------------
# Step 1: restore, skipped if csproj/slnx/props haven't changed since last run
# ----------------------------------------------------------------------------
echo "[1/2] Dependency restore, ${CPU_CORES} core(s)"
mapfile -t RESTORE_INPUTS < <(find . \
    \( -name "*.csproj" -o -name "*.slnx" -o -name "Directory.Build.props" -o -name "packages.lock.json" \) \
    -not -path "*/bin/*" -not -path "*/obj/*" -not -path "${OUTPUT_DIR}/*" | sort)

CURRENT_HASH=$(cat "${RESTORE_INPUTS[@]}" 2>/dev/null | sha256sum | cut -d' ' -f1)
PREVIOUS_HASH=""
[ -f "$RESTORE_STAMP" ] && PREVIOUS_HASH=$(cat "$RESTORE_STAMP")

if [ "$CURRENT_HASH" = "$PREVIOUS_HASH" ]; then
    echo "No changes to project/solution files since last restore, skipping."
else
    echo "Project files changed (or first run), restoring..."
    # ADDED: -r linux-x64 tells NuGet to generate the NativeAOT asset targets!
    dotnet restore "$SOLUTION" -r linux-x64 -v q "${MSBUILD_PARALLEL_ARGS[@]}"
    echo "$CURRENT_HASH" > "$RESTORE_STAMP"
fi

# ----------------------------------------------------------------------------
# Step 2: NativeAOT publish
# ----------------------------------------------------------------------------
echo "[2/2] NativeAOT publish, ${CPU_CORES} core(s)"
dotnet publish "Sandbox/${PROJECT_NAME}.csproj" \
    -c "$CONFIGURATION" \
    -r "linux-x64" \
    -o "$OUTPUT_DIR/linux-x64" \
    --no-restore \
    -v q \
    "${MSBUILD_PARALLEL_ARGS[@]}" \
    /p:PublishAot=true

# Copy Assets and strip symbols for local package
mkdir -p "$OUTPUT_DIR/linux-x64/Assets"
cp -r "./Assets/." "$OUTPUT_DIR/linux-x64/Assets/" 2>/dev/null || true
find "$OUTPUT_DIR/linux-x64" -name "*.pdb" -type f -delete || true

echo "=================================================================="
echo "Build complete: $OUTPUT_DIR/linux-x64"
echo "=================================================================="

./local-ci-artifacts/linux-x64/Sandbox