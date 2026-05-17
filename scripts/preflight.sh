#!/usr/bin/env bash

set -euo pipefail

echo "== TensorGate local preflight =="

if [ ! -f "global.json" ]; then
  echo "ERROR: run from repository root (global.json not found)."
  exit 1
fi

echo "1) Verify .NET SDK"
dotnet --info > /dev/null

if [ -n "$(find . -type f \( -name "*.sln" -o -name "*.csproj" \) -print -quit)" ]; then
  echo "2) Restore"
  dotnet restore

  echo "3) Build"
  dotnet build --no-restore --configuration Release /p:TreatWarningsAsErrors=true

  echo "4) Test"
  dotnet test --no-build --configuration Release

  echo "5) Format check"
  dotnet format --verify-no-changes --verbosity minimal
else
  echo "No .sln/.csproj found yet; build/test/format checks are skipped."
fi

echo "6) Markdown lint (optional local equivalent)"
echo "Use CI workflow 'Docs Quality' for canonical docs checks."

echo "Preflight completed."
