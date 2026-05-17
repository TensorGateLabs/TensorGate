#!/usr/bin/env bash

set -euo pipefail

REPO="${1:-syed-dawood/TensorGate}"
LIMIT="${2:-200}"

echo "== CI cost audit =="
echo "Repo: ${REPO}"
echo "Runs sampled: ${LIMIT}"
echo

gh run list --repo "${REPO}" --limit "${LIMIT}" \
  --json workflowName,event,status,conclusion,createdAt,startedAt,updatedAt \
  --jq '
    def minutes:
      if .startedAt == null or .updatedAt == null then 0
      else ((.updatedAt | fromdateiso8601) - (.startedAt | fromdateiso8601)) / 60
      end;
    group_by(.workflowName) |
    map({
      workflow: .[0].workflowName,
      runs: length,
      total_minutes: (map(minutes) | add),
      avg_minutes: ((map(minutes) | add) / length),
      push_runs: (map(select(.event=="push")) | length),
      pr_runs: (map(select(.event=="pull_request" or .event=="pull_request_target")) | length),
      schedule_runs: (map(select(.event=="schedule")) | length)
    }) |
    sort_by(-.total_minutes)
  '
