#!/usr/bin/env bash
# Move a TensorGate issue on the org project board by Status name.
set -euo pipefail

ISSUE="${1:-}"
STATUS="${2:-}"
ORG="TensorGateLabs"
PROJECT_NUMBER=1

if [ -z "$ISSUE" ] || [ -z "$STATUS" ]; then
  echo "Usage: $0 <issue-number> <status-name>"
  echo "Statuses: Todo | In Progress | Peer Review | Ready For QA | QA Complete | Done"
  exit 1
fi

case "$STATUS" in
  "Todo") OPTION_ID="3b83d2de" ;;
  "In Progress") OPTION_ID="17848eb0" ;;
  "Peer Review") OPTION_ID="4fa58416" ;;
  "Ready For QA") OPTION_ID="332da095" ;;
  "QA Complete") OPTION_ID="a0b02b13" ;;
  "Done") OPTION_ID="de1c4c4f" ;;
  *)
    echo "Unknown status: $STATUS"
    exit 1
    ;;
esac

ISSUE_NODE=$(gh issue view "$ISSUE" --repo "$ORG/TensorGate" --json id -q .id)
PROJECT_ID="PVT_kwDOEQI5Wc4BX8Dg"
FIELD_ID="PVTSSF_lADOEQI5Wc4BX8DgzhTFZnE"

ITEM_ID=$(gh api graphql -f query='
query($org: String!, $proj: Int!) {
  organization(login: $org) {
    projectV2(number: $proj) {
      items(first: 100) {
        nodes {
          id
          content { ... on Issue { number } }
        }
      }
    }
  }
}' -f org="$ORG" -F proj="$PROJECT_NUMBER" \
  --jq ".data.organization.projectV2.items.nodes[] | select(.content.number==$ISSUE) | .id" | head -1)

if [ -z "$ITEM_ID" ]; then
  echo "Issue #$ISSUE not on project board; adding..."
  gh project item-add "$PROJECT_NUMBER" --owner "$ORG" --url "https://github.com/$ORG/TensorGate/issues/$ISSUE" >/dev/null
  ITEM_ID=$(gh api graphql -f query='
query($org: String!, $proj: Int!) {
  organization(login: $org) {
    projectV2(number: $proj) {
      items(first: 100) {
        nodes {
          id
          content { ... on Issue { number } }
        }
      }
    }
  }
}' -f org="$ORG" -F proj="$PROJECT_NUMBER" \
    --jq ".data.organization.projectV2.items.nodes[] | select(.content.number==$ISSUE) | .id" | head -1)
fi

gh api graphql -f query='
mutation($project: ID!, $item: ID!, $field: ID!, $option: String!) {
  updateProjectV2ItemFieldValue(
    input: {
      projectId: $project
      itemId: $item
      fieldId: $field
      value: { singleSelectOptionId: $option }
    }
  ) {
    projectV2Item { id }
  }
}' -f project="$PROJECT_ID" -f item="$ITEM_ID" -f field="$FIELD_ID" -f option="$OPTION_ID" >/dev/null

echo "Issue #$ISSUE → Status: $STATUS"
