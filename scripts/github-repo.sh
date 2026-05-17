#!/usr/bin/env bash
# Resolve owner/repo for gh CLI (Actions, local clone, or default org repo).
set -euo pipefail

if [ -n "${GITHUB_REPOSITORY:-}" ]; then
  echo "${GITHUB_REPOSITORY}"
  exit 0
fi

if command -v git >/dev/null 2>&1 && git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  origin="$(git remote get-url origin 2>/dev/null || true)"
  if [ -n "${origin}" ]; then
    if [[ "${origin}" =~ github\.com[:/]([^/]+/[^/.]+) ]]; then
      echo "${BASH_REMATCH[1]}"
      exit 0
    fi
  fi
fi

echo "TensorGateLabs/TensorGate"
