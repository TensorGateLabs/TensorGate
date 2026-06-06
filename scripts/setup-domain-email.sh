#!/usr/bin/env bash

# Configure free custom-domain email on Cloudflare for tensorgate.com.
#
# What this does (all via the Cloudflare API, no clicking required):
#   1. Resolves the Cloudflare zone + account for your domain.
#   2. Enables Email Routing and auto-creates the required MX/TXT records.
#   3. Merges Google into the SPF record so Gmail "Send mail as" passes SPF.
#   4. Publishes a relaxed DMARC record (p=none) so send-as mail is not blocked.
#   5. Registers your Gmail as a verified destination (triggers a confirm email).
#   6. Creates forwarding rules (e.g. founder@tensorgate.com -> your Gmail)
#      and, optionally, a catch-all rule for every other address.
#
# What this CANNOT do for you (manual, one-time):
#   - Register the domain (do this once at the Cloudflare Registrar checkout).
#   - Click the verification link Cloudflare emails to your Gmail.
#   - Configure Gmail "Send mail as" (needs your Google App Password in the
#     Gmail UI). See docs/domain-and-email-setup.md for the exact steps.
#
# Usage:
#   export CF_API_TOKEN="..."             # required (scoped token, see docs)
#   export ZONE_NAME="tensorgate.com"     # required
#   export FORWARD_TO="you@gmail.com"     # required (destination inbox)
#   export ADDRESSES="founder,hello,info" # optional, comma-separated local parts
#   export CATCH_ALL="true"               # optional, forward everything else too
#   export CF_ACCOUNT_ID="..."            # optional, auto-detected if omitted
#   ./scripts/setup-domain-email.sh

set -euo pipefail

API="https://api.cloudflare.com/client/v4"

: "${CF_API_TOKEN:?Set CF_API_TOKEN (Cloudflare API token). See docs/domain-and-email-setup.md}"
: "${ZONE_NAME:?Set ZONE_NAME, e.g. tensorgate.com}"
: "${FORWARD_TO:?Set FORWARD_TO, e.g. you@gmail.com}"
ADDRESSES="${ADDRESSES:-founder}"
CATCH_ALL="${CATCH_ALL:-false}"

if ! command -v jq >/dev/null 2>&1; then
  echo "ERROR: jq is required (sudo apt-get install jq)." >&2
  exit 1
fi

# cf METHOD PATH [JSON_BODY] -> prints response body, fails on API error.
cf() {
  local method="$1" path="$2" body="${3:-}"
  local resp
  if [ -n "$body" ]; then
    resp=$(curl -fsS -X "$method" "${API}${path}" \
      -H "Authorization: Bearer ${CF_API_TOKEN}" \
      -H "Content-Type: application/json" \
      --data "$body")
  else
    resp=$(curl -fsS -X "$method" "${API}${path}" \
      -H "Authorization: Bearer ${CF_API_TOKEN}" \
      -H "Content-Type: application/json")
  fi
  if [ "$(echo "$resp" | jq -r '.success')" != "true" ]; then
    echo "API call failed: $method $path" >&2
    echo "$resp" | jq -r '.errors' >&2
    exit 1
  fi
  echo "$resp"
}

echo "== TensorGate domain email setup =="
echo "Zone: ${ZONE_NAME}  ->  Forward to: ${FORWARD_TO}"

echo "1) Resolve zone id"
ZONE_ID=$(cf GET "/zones?name=${ZONE_NAME}" | jq -r '.result[0].id // empty')
if [ -z "$ZONE_ID" ]; then
  echo "ERROR: zone '${ZONE_NAME}' not found on this Cloudflare account." >&2
  echo "Register/add the domain first, then re-run." >&2
  exit 1
fi
CF_ACCOUNT_ID="${CF_ACCOUNT_ID:-$(cf GET "/zones/${ZONE_ID}" | jq -r '.result.account.id')}"
echo "   zone_id=${ZONE_ID}  account_id=${CF_ACCOUNT_ID}"

echo "2) Enable Email Routing + create required MX/TXT records"
# Adds the Cloudflare MX records and the base SPF TXT, and turns routing on.
cf POST "/zones/${ZONE_ID}/email/routing/dns" >/dev/null || true
cf POST "/zones/${ZONE_ID}/email/routing/enable" '{}' >/dev/null 2>&1 || true
echo "   routing enabled"

echo "3) Merge Google into SPF (so Gmail send-as passes SPF)"
SPF_VALUE="v=spf1 include:_spf.mx.cloudflare.net include:_spf.google.com ~all"
SPF_ID=$(cf GET "/zones/${ZONE_ID}/dns_records?type=TXT&name=${ZONE_NAME}" \
  | jq -r '.result[] | select(.content | test("v=spf1")) | .id' | head -n1)
if [ -n "$SPF_ID" ]; then
  cf PATCH "/zones/${ZONE_ID}/dns_records/${SPF_ID}" \
    "$(jq -n --arg c "$SPF_VALUE" '{type:"TXT",name:"'"${ZONE_NAME}"'",content:$c}')" >/dev/null
else
  cf POST "/zones/${ZONE_ID}/dns_records" \
    "$(jq -n --arg c "$SPF_VALUE" '{type:"TXT",name:"'"${ZONE_NAME}"'",content:$c}')" >/dev/null
fi
echo "   SPF = ${SPF_VALUE}"

echo "4) Publish DMARC (p=none, so send-as mail is not blocked)"
DMARC_NAME="_dmarc.${ZONE_NAME}"
DMARC_VALUE="v=DMARC1; p=none; rua=mailto:${FORWARD_TO}"
if [ -z "$(cf GET "/zones/${ZONE_ID}/dns_records?type=TXT&name=${DMARC_NAME}" | jq -r '.result[0].id // empty')" ]; then
  cf POST "/zones/${ZONE_ID}/dns_records" \
    "$(jq -n --arg n "$DMARC_NAME" --arg c "$DMARC_VALUE" '{type:"TXT",name:$n,content:$c}')" >/dev/null
  echo "   DMARC published"
else
  echo "   DMARC already present, skipping"
fi

echo "5) Register destination address: ${FORWARD_TO}"
cf POST "/accounts/${CF_ACCOUNT_ID}/email/routing/addresses" \
  "$(jq -n --arg e "$FORWARD_TO" '{email:$e}')" >/dev/null 2>&1 || true
echo "   -> Cloudflare emailed a verification link to ${FORWARD_TO}. CLICK IT."

echo "6) Create forwarding rules"
IFS=',' read -ra PARTS <<< "$ADDRESSES"
for part in "${PARTS[@]}"; do
  part="$(echo "$part" | tr -d '[:space:]')"
  [ -z "$part" ] && continue
  addr="${part}@${ZONE_NAME}"
  body=$(jq -n --arg name "forward-${part}" --arg addr "$addr" --arg dest "$FORWARD_TO" '{
    name: $name,
    enabled: true,
    matchers: [ { type: "literal", field: "to", value: $addr } ],
    actions: [ { type: "forward", value: [ $dest ] } ]
  }')
  cf POST "/zones/${ZONE_ID}/email/routing/rules" "$body" >/dev/null 2>&1 || true
  echo "   ${addr} -> ${FORWARD_TO}"
done

if [ "$CATCH_ALL" = "true" ]; then
  echo "7) Enable catch-all -> ${FORWARD_TO}"
  body=$(jq -n --arg dest "$FORWARD_TO" '{
    enabled: true,
    matchers: [ { type: "all" } ],
    actions: [ { type: "forward", value: [ $dest ] } ]
  }')
  cf PUT "/zones/${ZONE_ID}/email/routing/rules/catch_all" "$body" >/dev/null
  echo "   catch-all on"
fi

echo
echo "== Done on the Cloudflare side. =="
echo "Remaining manual steps (see docs/domain-and-email-setup.md):"
echo "  A. Click the Cloudflare verification email in ${FORWARD_TO}."
echo "  B. In Gmail: Settings > Accounts > 'Send mail as' > add ${PARTS[0]}@${ZONE_NAME}"
echo "     SMTP smtp.gmail.com : 587 (TLS), username = your Gmail, password = App Password."
