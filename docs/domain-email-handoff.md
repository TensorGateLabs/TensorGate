# TensorGate — Domain & Org Email Setup (Handoff)

Self-contained handoff. Everything needed to finish registering email for
**tensorgate.dev** and set up **free custom-domain email** forwarding to
**sd.syeddawood@gmail.com**, plus Gmail "send as".

---

## Decisions made

- **Domain:** `tensorgate.dev` — registered at the **Cloudflare Registrar**
  (~$12.20/yr, at cost, free WHOIS privacy).
  - `tensorgate.com` was already taken.
  - `.ai` rejected: premium (~$80–95/yr) with a registry-forced **2-year
    minimum** (~$160 up front). Can be added later as a redirect.
  - `.dev` = clean unhyphenated brand, Google-operated, enforced HTTPS, signals
    "developer/technical tool" — fits AI-safety middleware.
- **Email:** **Cloudflare Email Routing** (free receive + forward) +
  **Gmail "Send mail as"** (free send). Total cost ≈ **$12/yr**.
- **Destination inbox:** `sd.syeddawood@gmail.com`
- **Addresses:** `founder@`, `hello@`, `info@` + catch-all (everything else).

### Limitations of this free setup (be aware)
- It's **forwarding**, not real mailboxes — mail lands in your Gmail.
- Your Gmail address is visible in **raw headers** of sent mail. Fine for a
  founder/small team.
- Gmail consumer **send limits** (~500/day). For bulk/transactional mail later,
  add Amazon SES / Mailgun / Resend on a subdomain (`mail.tensorgate.dev`) — it
  won't affect this inbox.

---

## Status

- [x] Domain `tensorgate.dev` registered (Cloudflare account `sd.syeddawood@gmail.com`)
- [x] Cloudflare API token created (Zone:Read, DNS:Edit, Email Routing Rules:Edit,
      Account Email Routing Addresses:Edit), scoped to `tensorgate.dev`
- [ ] Run `setup-domain-email.sh` (DNS + routing + rules)  ← **next**
- [ ] Click Cloudflare destination-verification email
- [ ] Gmail App Password + "Send mail as"
- [ ] Rotate/delete the API token

> The setup script must run somewhere with internet access to
> `api.cloudflare.com` (e.g. your Mac). The web sandbox is firewalled from it.

---

## Step 1 — Run the setup script (your Mac)

```bash
# Get the script
git clone https://github.com/TensorGateLabs/TensorGate.git
cd TensorGate
git checkout claude/tensorgate-domain-email-8Z8iC

# Prereq (skip if you already have jq)
brew install jq

# Configure + run
export CF_API_TOKEN="<paste-your-cloudflare-token>"   # the cfut_... token you created
export ZONE_NAME="tensorgate.dev"
export FORWARD_TO="sd.syeddawood@gmail.com"
export ADDRESSES="founder,hello,info"
export CATCH_ALL="true"
./scripts/setup-domain-email.sh
```

This enables Email Routing, creates MX records, sets SPF (with Google included),
publishes DMARC (p=none), registers the destination, and creates the forwarders
+ catch-all. It's idempotent — safe to re-run.

> ⚠️ Rotate the token after setup: it's been shared in chat. Delete it in the
> Cloudflare dashboard once done — the config persists without it.

## Step 2 — Verify the destination (10 sec)

Cloudflare emails a "Verify your email address" link to
`sd.syeddawood@gmail.com`. **Click it.** Forwarding doesn't work until verified.

## Step 3 — Gmail "Send mail as" (so you can send *from* the domain)

1. Enable **2-Step Verification** on the Google account.
2. Create an **App Password**: <https://myaccount.google.com/apppasswords>
   (name it "TensorGate Mail"; copy the 16-char code).
3. Gmail → **Settings → Accounts and Import → "Send mail as" → Add another
   email address**:
   - Name: `TensorGate` · Email: `founder@tensorgate.dev` · uncheck "alias".
   - SMTP: `smtp.gmail.com` · Port: `587` · TLS.
   - Username: your full Gmail · Password: the **App Password**.
4. Enter the confirmation code Gmail sends (arrives in your inbox via the
   forwarder).
5. Optional: set as default + "reply from the same address the message was sent to".

Done — you receive at and send from `founder@tensorgate.dev` for ~$12/yr.

---

## The script (for reference — `scripts/setup-domain-email.sh`)

```bash
#!/usr/bin/env bash
# Configure free custom-domain email on Cloudflare for tensorgate.dev.
# Usage: set CF_API_TOKEN, ZONE_NAME, FORWARD_TO, (ADDRESSES, CATCH_ALL) then run.
set -euo pipefail
API="https://api.cloudflare.com/client/v4"
: "${CF_API_TOKEN:?Set CF_API_TOKEN}"
: "${ZONE_NAME:?Set ZONE_NAME, e.g. tensorgate.dev}"
: "${FORWARD_TO:?Set FORWARD_TO, e.g. you@gmail.com}"
ADDRESSES="${ADDRESSES:-founder}"
CATCH_ALL="${CATCH_ALL:-false}"
command -v jq >/dev/null || { echo "jq required"; exit 1; }

cf() {
  local method="$1" path="$2" body="${3:-}" resp
  if [ -n "$body" ]; then
    resp=$(curl -fsS -X "$method" "${API}${path}" -H "Authorization: Bearer ${CF_API_TOKEN}" -H "Content-Type: application/json" --data "$body")
  else
    resp=$(curl -fsS -X "$method" "${API}${path}" -H "Authorization: Bearer ${CF_API_TOKEN}" -H "Content-Type: application/json")
  fi
  [ "$(echo "$resp" | jq -r '.success')" = "true" ] || { echo "FAIL $method $path" >&2; echo "$resp" | jq -r '.errors' >&2; exit 1; }
  echo "$resp"
}

ZONE_ID=$(cf GET "/zones?name=${ZONE_NAME}" | jq -r '.result[0].id // empty')
[ -n "$ZONE_ID" ] || { echo "zone not found"; exit 1; }
CF_ACCOUNT_ID="${CF_ACCOUNT_ID:-$(cf GET "/zones/${ZONE_ID}" | jq -r '.result.account.id')}"

cf POST "/zones/${ZONE_ID}/email/routing/dns" >/dev/null || true
cf POST "/zones/${ZONE_ID}/email/routing/enable" '{}' >/dev/null 2>&1 || true

SPF_VALUE="v=spf1 include:_spf.mx.cloudflare.net include:_spf.google.com ~all"
SPF_ID=$(cf GET "/zones/${ZONE_ID}/dns_records?type=TXT&name=${ZONE_NAME}" | jq -r '.result[] | select(.content | test("v=spf1")) | .id' | head -n1)
if [ -n "$SPF_ID" ]; then
  cf PATCH "/zones/${ZONE_ID}/dns_records/${SPF_ID}" "$(jq -n --arg c "$SPF_VALUE" '{type:"TXT",name:"'"${ZONE_NAME}"'",content:$c}')" >/dev/null
else
  cf POST "/zones/${ZONE_ID}/dns_records" "$(jq -n --arg c "$SPF_VALUE" '{type:"TXT",name:"'"${ZONE_NAME}"'",content:$c}')" >/dev/null
fi

DMARC_NAME="_dmarc.${ZONE_NAME}"
DMARC_VALUE="v=DMARC1; p=none; rua=mailto:${FORWARD_TO}"
if [ -z "$(cf GET "/zones/${ZONE_ID}/dns_records?type=TXT&name=${DMARC_NAME}" | jq -r '.result[0].id // empty')" ]; then
  cf POST "/zones/${ZONE_ID}/dns_records" "$(jq -n --arg n "$DMARC_NAME" --arg c "$DMARC_VALUE" '{type:"TXT",name:$n,content:$c}')" >/dev/null
fi

cf POST "/accounts/${CF_ACCOUNT_ID}/email/routing/addresses" "$(jq -n --arg e "$FORWARD_TO" '{email:$e}')" >/dev/null 2>&1 || true

IFS=',' read -ra PARTS <<< "$ADDRESSES"
for part in "${PARTS[@]}"; do
  part="$(echo "$part" | tr -d '[:space:]')"; [ -z "$part" ] && continue
  addr="${part}@${ZONE_NAME}"
  body=$(jq -n --arg name "forward-${part}" --arg addr "$addr" --arg dest "$FORWARD_TO" '{name:$name,enabled:true,matchers:[{type:"literal",field:"to",value:$addr}],actions:[{type:"forward",value:[$dest]}]}')
  cf POST "/zones/${ZONE_ID}/email/routing/rules" "$body" >/dev/null 2>&1 || true
  echo "${addr} -> ${FORWARD_TO}"
done

if [ "$CATCH_ALL" = "true" ]; then
  body=$(jq -n --arg dest "$FORWARD_TO" '{enabled:true,matchers:[{type:"all"}],actions:[{type:"forward",value:[$dest]}]}')
  cf PUT "/zones/${ZONE_ID}/email/routing/rules/catch_all" "$body" >/dev/null
fi
echo "Done. Verify the Cloudflare email, then set up Gmail 'Send mail as'."
```

---

## Open / later

- Optional: register `tensorgate.ai` and 301-redirect it to `tensorgate.dev`.
- Optional: real mailboxes (Zoho free 5-user, or Google Workspace ~$7/user/mo).
- Optional: dedicated sending (SES/Mailgun/Resend) on `mail.tensorgate.dev` for
  high-volume/transactional email.

## Files on branch `claude/tensorgate-domain-email-8Z8iC`
- `scripts/setup-domain-email.sh` — the automation
- `docs/domain-and-email-setup.md` — full walkthrough
- `docs/domain-email-handoff.md` — this file
