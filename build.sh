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
dotnet restore "$SOLUTION" -v q

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

# ----------------------------------------------------------------------------
# STEP 3: Sandbox/Container Isolation Test
# ----------------------------------------------------------------------------
echo "👉 [3/4] Spin up Pristine Container to Verify Toolchain Dependencies..."

docker run --rm -i \
  -v "$(pwd):/src" \
  -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0-noble \
  bash <<'EOF'
    set -euo pipefail
    
    SOLUTION="HarpyEngine.slnx"
    PROJECT_NAME="Sandbox"
    CONFIGURATION="Release"
    TMP_BUILD_DIR="/tmp/docker-native-build"

    echo "   📦 Container: Installing NativeAOT dependencies..."
    apt-get update -qq && apt-get install -y -qq clang zlib1g-dev > /dev/null
    
    echo "   📦 Container: Restoring dependencies..."
    dotnet restore "$SOLUTION" -v q
    
    echo "   📦 Container: Compiling standalone NativeAOT binary..."
    dotnet publish "Sandbox/${PROJECT_NAME}.csproj" \
        -c "$CONFIGURATION" \
        -r linux-x64 \
        -o "$TMP_BUILD_DIR" \
        --no-restore \
        -v q \
        /p:PublishAot=true
        
    echo "   📦 Container: Exporting production artifacts back to host..."
    mkdir -p "./local-ci-artifacts/docker-ubuntu-x64"
    cp -r "$TMP_BUILD_DIR/." "./local-ci-artifacts/docker-ubuntu-x64/"
EOF

# Strip symbols out of the docker artifact on the host side
find "$OUTPUT_DIR/docker-ubuntu-x64" -name "*.pdb" -type f -delete || true

# ----------------------------------------------------------------------------
# STEP 4: Summary Analysis
# ----------------------------------------------------------------------------
echo "=================================================================="
echo " 🎉 LOCAL CI SUCCESSFUL"
echo "=================================================================="
echo "Generated Binaries:"
echo " 📦 Host Native Build:   $(du -sh "$OUTPUT_DIR/linux-x64/${PROJECT_NAME}" | cut -f1)"
echo " 📦 Isolated Docker:     $(du -sh "$OUTPUT_DIR/docker-ubuntu-x64/${PROJECT_NAME}" | cut -f1)"
echo "=================================================================="