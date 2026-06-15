#!/usr/bin/env bash
set -euo pipefail

# ============================================================================
# Local CI Pipeline Configurations
# ============================================================================
SOLUTION="HarpyEngine.slnx"
PROJECT_NAME="Sandbox"
CONFIGURATION="Release"
OUTPUT_DIR="./local-ci-artifacts"

echo "=================================================================="
echo " 🚀 Starting Harpy Engine Local Production CI Verification"
echo "=================================================================="

# ----------------------------------------------------------------------------
# ENVIRONMENT FIX: Handle JetBrains Rider Internal Console Path Isolation
# ----------------------------------------------------------------------------
if ! command -v dotnet &> /dev/null; then
    echo "⚠️  Standard 'dotnet' command missing in this terminal context."
    
    # Check your specific local home directory installation path
    LOCAL_DOTNET="$HOME/.dotnet"
    
    if [ -f "$LOCAL_DOTNET/dotnet" ]; then
        echo "✅ Detected local SDK at $LOCAL_DOTNET. Injecting paths..."
        export DOTNET_ROOT="$LOCAL_DOTNET"
        export PATH="${PATH}:${LOCAL_DOTNET}"
    # Fallback to standard Arch system path just in case
    elif [ -f "/usr/share/dotnet/dotnet" ]; then
        echo "✅ Detected system SDK at /usr/share/dotnet. Injecting paths..."
        export DOTNET_ROOT="/usr/share/dotnet"
        export PATH="${PATH}:${DOTNET_ROOT}"
    else
        echo "❌ Error: .NET SDK could not be found anywhere."
        echo "Rider console is isolated. Please run this script in an external terminal."
        exit 1
    fi
fi

# Fresh cleanup of previous artifacts
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR/linux-x64"
mkdir -p "$OUTPUT_DIR/docker-ubuntu-x64"

# ----------------------------------------------------------------------------
# STEP 1: Verify Codebase Inherent Integrity
# ----------------------------------------------------------------------------
echo "👉 [1/4] Running Environment Restore..."
# ADDED: -r linux-x64 tells NuGet to generate the NativeAOT asset targets!
dotnet restore "$SOLUTION" -r linux-x64 -v q

# ----------------------------------------------------------------------------
# STEP 2: Compile Local Platform NativeAOT
# ----------------------------------------------------------------------------
echo "👉 [2/4] Executing Local Host NativeAOT Compilation (linux-x64)..."
dotnet publish "Sandbox/${PROJECT_NAME}.csproj" \
    -c "$CONFIGURATION" \
    -r "linux-x64" \
    -o "$OUTPUT_DIR/linux-x64" \
    --no-restore \
    -v q \
    /p:PublishAot=true

# Copy Assets and strip symbols for local package
mkdir -p "$OUTPUT_DIR/linux-x64/Assets"
cp -r "./Assets/." "$OUTPUT_DIR/linux-x64/Assets/" 2>/dev/null || true
find "$OUTPUT_DIR/linux-x64" -name "*.pdb" -type f -delete || true