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

# [1/6] Clean
echo "[1/6] Cleaning..."
cleanup
dotnet clean "$SOLUTION" -c "$CONFIGURATION" -v q >/dev/null

# [2/6] Restore
echo "[2/6] Restoring NuGet packages..."
dotnet restore "$SOLUTION" -v q

# [3/6] Build
echo "[3/6] Building solution..."
dotnet build "$SOLUTION" -c "$CONFIGURATION" --no-restore -v q

# [4/6] Test
echo "[4/6] Running tests..."
dotnet test "$TEST_PROJECT" -c "$CONFIGURATION" --no-build -v q --nologo

# [5/6] Publish (NativeAOT)
echo "[5/6] Publishing NativeAOT..."
dotnet publish "Sandbox/${PROJECT_NAME}.csproj" \
    -c "$CONFIGURATION" \
    -r "$RUNTIME" \
    -o "$OUTPUT_DIR" \
    --no-build \
    -v q \
    /p:PublishAot=true

echo "[5.5/6] Copying Assets..."
mkdir -p "$OUTPUT_DIR/Assets"
cp -r "./Assets/." "$OUTPUT_DIR/Assets/"

# [6/6] Cleanup
echo "[6/6] Finalizing..."
find "$OUTPUT_DIR" -name "*.pdb" -type f -delete

echo "=========================================="
echo "  BUILD SUCCESS"
echo "=========================================="