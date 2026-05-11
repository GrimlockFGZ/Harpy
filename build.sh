#!/usr/bin/env bash
set -euo pipefail

# ================================
# Configuration
# ================================
SOLUTION="HarpyEngine.slnx"
PROJECT_NAME="Sandbox"
TEST_PROJECT="Engine.Tests/Engine.Tests.csproj"
CONFIGURATION="Release"
RUNTIME="linux-x64"
OUTPUT_DIR="./publish"
ASSET_DIR="./assets"
ENABLE_COVERAGE="true"

echo "=========================================="
echo "  Harpy CI Build Pipeline Starting"
echo "=========================================="

cleanup() {
  if [[ -d "$OUTPUT_DIR" ]]; then
    rm -rf "$OUTPUT_DIR"
  fi
}

# [1/7] Clean
echo "[1/7] Cleaning..."
cleanup
dotnet clean "$SOLUTION" -c "$CONFIGURATION" -v q >/dev/null

# [2/7] Restore
echo "[2/7] Restoring NuGet packages..."
dotnet restore "$SOLUTION" -v q


# [4/7] Build
echo "[4/7] Building solution..."
dotnet build "$SOLUTION" -c "$CONFIGURATION" --no-restore -v q

# [5/7] Publish (NativeAOT)
echo "[6/7] Publishing NativeAOT..."
dotnet publish "Sandbox/${PROJECT_NAME}.csproj" \
    -c "$CONFIGURATION" \
    -r "$RUNTIME" \
    -o "$OUTPUT_DIR" \
    --no-build \
    -v q \
    /p:PublishAot=true

echo "[6.5/7] Copying Assets..."
mkdir -p "$OUTPUT_DIR/Assets"
cp -r "./Assets/." "$OUTPUT_DIR/Assets/"

# [7/7] Cleanup
echo "[7/7] Finalizing..."
find "$OUTPUT_DIR" -name "*.pdb" -type f -delete

echo "=========================================="
echo "  BUILD SUCCESS"
echo "=========================================="