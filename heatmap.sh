#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BROWSER_DIR="$SCRIPT_DIR/Browser"
HEATMAPS_DIR="$SCRIPT_DIR/heatmaps"

# Create heatmaps output folder if it doesn't exist
mkdir -p "$HEATMAPS_DIR"

# Generate timestamped output filename
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
OUTPUT_FILE="$HEATMAPS_DIR/trace_$TIMESTAMP.speedscope.json"

echo "==> Building project in $BROWSER_DIR..."
dotnet build "$BROWSER_DIR" -c Release

# Detect the project name and target framework from the .csproj
CSPROJ=$(find "$BROWSER_DIR" -maxdepth 1 -name "*.csproj" | head -n 1)
if [ -z "$CSPROJ" ]; then
  echo "ERROR: No .csproj found in $BROWSER_DIR"
  exit 1
fi

PROJECT_NAME=$(basename "$CSPROJ" .csproj)
TARGET_FRAMEWORK=$(grep -oPm1 '(?<=<TargetFramework>)[^<]+' "$CSPROJ")

if [ -z "$TARGET_FRAMEWORK" ]; then
  echo "ERROR: Could not determine TargetFramework from $CSPROJ"
  exit 1
fi

APP_DLL="$BROWSER_DIR/bin/Release/$TARGET_FRAMEWORK/$PROJECT_NAME.dll"

if [ ! -f "$APP_DLL" ]; then
  echo "ERROR: Built DLL not found at $APP_DLL"
  exit 1
fi

echo "==> Starting trace -> $OUTPUT_FILE"
echo "    Project : $PROJECT_NAME"
echo "    Framework: $TARGET_FRAMEWORK"
echo "    Output  : $OUTPUT_FILE"
echo ""

~/.dotnet/tools/dotnet-trace collect \
  --format speedscope \
  --output "$OUTPUT_FILE" \
  -- dotnet "$APP_DLL"

echo ""
echo "==> Trace saved to: $OUTPUT_FILE"