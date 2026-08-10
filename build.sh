#!/usr/bin/env bash
set -euo pipefail
# ============================================================================
# Harpy Engine — Build Script
#
# Usage:
#   ./build.sh [config]
#
# Configurations:
#   debug      — Framework-dependent debug build (fast, no AOT)
#   release    — Framework-dependent release build (optimised, no AOT)
#   dist       — NativeAOT publish for linux-x64 (slow, self-contained)
#   test       — Run Engine.Tests only (debug, no publish)
#
# Default: release
# ============================================================================
CONFIG="${1:-release}"
SOLUTION="HarpyEngine.slnx"
PROJECT_NAME="Sandbox"
OUTPUT_DIR="./local-ci-artifacts"
RESTORE_STAMP_DIR="./.build-cache"
RESTORE_STAMP="${RESTORE_STAMP_DIR}/restore.sha256"

# Detect available cores
if command -v nproc &> /dev/null; then
    CPU_CORES=$(nproc)
elif command -v sysctl &> /dev/null; then
    CPU_CORES=$(sysctl -n hw.ncpu)
else
    CPU_CORES=4
fi
MSBUILD_PARALLEL_ARGS=(-m:"$CPU_CORES" -p:UseSharedCompilation=true)

# ----------------------------------------------------------------------------
# dotnet PATH fallback (Rider terminal isolation)
# ----------------------------------------------------------------------------
if ! command -v dotnet &> /dev/null; then
    LOCAL_DOTNET="$HOME/.dotnet"
    if [ -f "$LOCAL_DOTNET/dotnet" ]; then
        export DOTNET_ROOT="$LOCAL_DOTNET"
        export PATH="${PATH}:${LOCAL_DOTNET}"
    elif [ -f "/usr/share/dotnet/dotnet" ]; then
        export DOTNET_ROOT="/usr/share/dotnet"
        export PATH="${PATH}:${DOTNET_ROOT}"
    else
        echo "Error: .NET SDK could not be found anywhere."
        exit 1
    fi
fi

# ----------------------------------------------------------------------------
# Shared: smart restore (skip if nothing changed)
# ----------------------------------------------------------------------------
do_restore() {
    local runtime_flag="${1:-}"
    mkdir -p "$RESTORE_STAMP_DIR"
    mapfile -t RESTORE_INPUTS < <(find . \
        \( -name "*.csproj" -o -name "*.slnx" -o -name "Directory.Build.props" -o -name "packages.lock.json" \) \
        -not -path "*/bin/*" -not -path "*/obj/*" -not -path "${OUTPUT_DIR}/*" | sort)

    CURRENT_HASH=$(cat "${RESTORE_INPUTS[@]}" 2>/dev/null | sha256sum | cut -d' ' -f1)
    PREVIOUS_HASH=""
    [ -f "$RESTORE_STAMP" ] && PREVIOUS_HASH=$(cat "$RESTORE_STAMP")

    if [ "$CURRENT_HASH" = "$PREVIOUS_HASH" ]; then
        echo "Restore: no changes detected, skipping."
    else
        echo "Restore: project files changed, restoring..."
        dotnet restore "$SOLUTION" ${runtime_flag:+-r "$runtime_flag"} -v q "${MSBUILD_PARALLEL_ARGS[@]}"
        echo "$CURRENT_HASH" > "$RESTORE_STAMP"
    fi
}

# ============================================================================
case "$CONFIG" in

# ----------------------------------------------------------------------------
# debug — fast framework-dependent debug build, launches the editor
# ----------------------------------------------------------------------------
debug)
    echo "=================================================================="
    echo " Harpy — DEBUG build"
    echo "=================================================================="
    do_restore

    OUT="$OUTPUT_DIR/debug"
    rm -rf "$OUT" && mkdir -p "$OUT"

    echo "[1/2] Build (Debug, ${CPU_CORES} core(s))"
    dotnet build "Sandbox/${PROJECT_NAME}.csproj" \
        -c Debug \
        -o "$OUT" \
        --no-restore \
        -v q \
        "${MSBUILD_PARALLEL_ARGS[@]}"

    mkdir -p "$OUT/Assets"
    cp -r "./Assets/." "$OUT/Assets/" 2>/dev/null || true

    echo "=================================================================="
    echo " Build complete: $OUT"
    echo "=================================================================="
    "$OUT/Sandbox"
    ;;

# ----------------------------------------------------------------------------
# release — optimised framework-dependent build, launches the editor
# ----------------------------------------------------------------------------
release)
    echo "=================================================================="
    echo " Harpy — RELEASE build"
    echo "=================================================================="
    do_restore

    OUT="$OUTPUT_DIR/release"
    rm -rf "$OUT" && mkdir -p "$OUT"

    echo "[1/2] Build (Release, ${CPU_CORES} core(s))"
    dotnet build "Sandbox/${PROJECT_NAME}.csproj" \
        -c Release \
        -o "$OUT" \
        --no-restore \
        -v q \
        "${MSBUILD_PARALLEL_ARGS[@]}"

    mkdir -p "$OUT/Assets"
    cp -r "./Assets/." "$OUT/Assets/" 2>/dev/null || true

    echo "=================================================================="
    echo " Build complete: $OUT"
    echo "=================================================================="
    "$OUT/Sandbox"
    ;;

# ----------------------------------------------------------------------------
# dist — NativeAOT self-contained publish for linux-x64 (slow)
# ----------------------------------------------------------------------------
dist)
    echo "=================================================================="
    echo " Harpy — DIST (NativeAOT) publish"
    echo "=================================================================="
    do_restore "linux-x64"

    OUT="$OUTPUT_DIR/linux-x64"
    rm -rf "$OUT" && mkdir -p "$OUT"

    echo "[1/2] NativeAOT publish (Release, ${CPU_CORES} core(s))"
    dotnet publish "Sandbox/${PROJECT_NAME}.csproj" \
        -c Release \
        -r linux-x64 \
        -o "$OUT" \
        --no-restore \
        -v q \
        "${MSBUILD_PARALLEL_ARGS[@]}" \
        /p:PublishAot=true

    mkdir -p "$OUT/Assets"
    cp -r "./Assets/." "$OUT/Assets/" 2>/dev/null || true
    find "$OUT" -name "*.pdb" -type f -delete || true

    echo "=================================================================="
    echo " Publish complete: $OUT"
    echo "=================================================================="
    "$OUT/Sandbox"
    ;;

# ----------------------------------------------------------------------------
# test — build and run Engine.Tests, no publish
# ----------------------------------------------------------------------------
test)
    echo "=================================================================="
    echo " Harpy — TEST run"
    echo "=================================================================="
    do_restore

    echo "[1/1] dotnet test (Debug, ${CPU_CORES} core(s))"
    dotnet test "Engine.Tests/Engine.Tests.csproj" \
        -c Debug \
        --no-restore \
        -v normal \
        "${MSBUILD_PARALLEL_ARGS[@]}"

    echo "=================================================================="
    echo " Tests complete."
    echo "=================================================================="
    ;;

# ----------------------------------------------------------------------------
# unknown
# ----------------------------------------------------------------------------
*)
    echo "Unknown configuration: '$CONFIG'"
    echo ""
    echo "Usage: ./build.sh [debug|release|dist|test]"
    exit 1
    ;;
esac
