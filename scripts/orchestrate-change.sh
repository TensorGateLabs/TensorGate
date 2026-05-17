#!/usr/bin/env bash

set -euo pipefail

ISSUE_ID="${1:-}"

if [ -z "${ISSUE_ID}" ]; then
  echo "Usage: $0 <issue-number>"
  echo "Example: $0 7"
  exit 1
fi

echo "== TensorGate orchestrated change flow =="
echo "Issue: #${ISSUE_ID}"

echo ""
echo "[1/6] Load issue context"
gh issue view "${ISSUE_ID}" --repo syed-dawood/TensorGate --json number,title,labels,milestone,url --jq '.'

echo ""
echo "[2/6] Run local preflight"
./scripts/preflight.sh

echo ""
echo "[3/6] Confirm branch naming convention"
BRANCH="$(git branch --show-current)"
if [[ "${BRANCH}" =~ ^feature/[0-9]+-[a-z0-9-]+$ ]] || \
   [[ "${BRANCH}" =~ ^fix/[0-9]+-[a-z0-9-]+$ ]] || \
   [[ "${BRANCH}" =~ ^refactor/[a-z0-9-]+$ ]] || \
   [[ "${BRANCH}" =~ ^docs/[a-z0-9-]+$ ]] || \
   [[ "${BRANCH}" =~ ^bench/[a-z0-9-]+$ ]]; then
  echo "Branch '${BRANCH}' is valid."
else
  echo "ERROR: Branch '${BRANCH}' does not match project convention."
  exit 1
fi

echo ""
echo "[4/6] Push branch (if upstream configured)"
git push -u origin "${BRANCH}"

echo ""
echo "[5/6] Display PR command hint"
echo "Create PR with:"
echo "gh pr create --repo syed-dawood/TensorGate --fill --body \"Closes #${ISSUE_ID}\""

echo ""
echo "[6/6] Post-push check hint"
echo "Monitor checks with:"
echo "gh pr checks --watch --fail-fast"

echo ""
echo "Flow completed."
