# Domain & Org Email Setup (tensorgate.com)

How TensorGate gets a real domain and free custom-domain email
(`founder@tensorgate.com` → your Gmail), the cheap way.

Decisions made: register **tensorgate.com** (~$10/yr) at the **Cloudflare
Registrar** (at-cost, free WHOIS privacy), and use **Cloudflare Email Routing +
Gmail "Send mail as"** for free send/receive.

> Why `.com` over `.ai`: `.ai` is a premium TLD at ~$80–95/yr with a registry-
> mandated **2-year minimum** (~$160 up front). `.com` is ~$10/yr, passes the
> radio test, and carries universal credibility. You can always add `.ai` later
> as a redirect.

---

## Cost summary

| Item | Cost |
| --- | --- |
| tensorgate.com (Cloudflare Registrar) | ~$10.44 / yr (at cost) |
| WHOIS privacy | Free |
| Cloudflare Email Routing (receive + forward) | Free |
| Gmail "Send mail as" (send) | Free (uses your existing Gmail) |
| **Total** | **~$10 / yr** |

### Honest limitations of the free email setup

- It is **forwarding**, not a real mailbox. Mail to `founder@tensorgate.com`
  lands in your normal Gmail inbox.
- Your personal Gmail address is still visible in the **raw headers** of mail you
  send. Fine for a founder/small team; not ideal for a 20-person company.
- It uses Gmail's **consumer sending limits** (~500 recipients/day). For bulk or
  transactional email later, add Amazon SES / Mailgun / Resend on a subdomain
  (e.g. `mail.tensorgate.com`) — that does not affect this inbox setup.

---

## Step 1 — Register the domain (manual, ~5 min, you do this)

This is the one part that cannot be scripted: registering a brand-new domain
needs a logged-in, funded account and checkout. No registrar exposes new-domain
registration over a public API.

1. Create/sign in to a Cloudflare account: <https://dash.cloudflare.com/sign-up>
2. Go to **Domain Registration → Register Domains**.
3. Search `tensorgate`, add **tensorgate.com**, and check out (~$10.44).
   WHOIS privacy is on by default and free.

Registering here automatically creates the DNS **zone** and points the domain at
Cloudflare nameservers — so the script below works immediately, no DNS waiting.

> If you ever register elsewhere (e.g. Porkbun is a touch cheaper), add the
> domain to Cloudflare as a zone and switch its nameservers to the two Cloudflare
> NS shown in the dashboard. Then the script works the same.

## Step 2 — Create a Cloudflare API token (you do this, then give it to me)

Dashboard → **My Profile → API Tokens → Create Token → Create Custom Token**.

Grant exactly these permissions (least privilege):

| Type | Resource | Permission |
| --- | --- | --- |
| Zone | Zone | Read |
| Zone | DNS | Edit |
| Zone | Email Routing Rules | Edit |
| Account | Email Routing Addresses | Edit |

- **Zone Resources:** Include → Specific zone → `tensorgate.com`
- **Account Resources:** Include → your account

Copy the token once. **This is the only secret I need to do everything else.**

## Step 3 — Run the setup script (I run this with your token)

```bash
export CF_API_TOKEN="<the token from step 2>"
export ZONE_NAME="tensorgate.com"
export FORWARD_TO="dawoodshah100@gmail.com"   # your destination inbox
export ADDRESSES="founder,hello,info"          # addresses to create
export CATCH_ALL="true"                         # forward everything else too
./scripts/setup-domain-email.sh
```

The script (`scripts/setup-domain-email.sh`) is idempotent and:

1. Enables Email Routing and creates the required **MX + SPF** records.
2. Updates **SPF** to `include:_spf.google.com` so Gmail send-as passes.
3. Publishes a **DMARC** record (`p=none`).
4. Registers your Gmail as a **destination** (triggers a verification email).
5. Creates **forwarding rules** for each address (+ optional catch-all).

## Step 4 — Verify the destination (manual, 10 sec, you do this)

Cloudflare sends a "Verify your email address" message to your Gmail. **Click the
link.** Forwarding does not work until this is confirmed.

## Step 5 — Configure Gmail "Send mail as" (manual, you do this)

So replies come *from* `founder@tensorgate.com`:

1. Enable **2-Step Verification** on your Google account if not already.
2. Create an **App Password**: <https://myaccount.google.com/apppasswords>
   (name it "TensorGate Mail"). Copy the 16-character code.
3. Gmail → **Settings → Accounts and Import → "Send mail as" → Add another
   email address**.
   - Name: `TensorGate` · Email: `founder@tensorgate.com` · uncheck "alias".
   - SMTP server: `smtp.gmail.com` · Port: `587` · TLS.
   - Username: your full Gmail address · Password: the **App Password**.
4. Gmail sends a confirmation code to `founder@tensorgate.com` — it arrives in
   your Gmail via routing. Enter it.
5. Optional: set it as default, and Settings → "reply from the same address the
   message was sent to".

Done. You now receive at and send from `founder@tensorgate.com` for ~$10/yr.

---

## What I need from you (only the things I can't do)

1. ✅ **Register tensorgate.com** at Cloudflare Registrar (Step 1).
2. ✅ **Cloudflare API token** with the 4 permissions above (Step 2) — paste it
   to me and I run Step 3.
3. ✅ Confirm the **destination inbox** (default: `dawoodshah100@gmail.com`) and
   which **addresses** you want (default: `founder`, `hello`, `info`).
4. ✅ **Click the verification email** (Step 4) and do the **Gmail send-as**
   setup (Step 5) — these are in Google/Cloudflare UIs I can't drive.

Everything else (DNS, routing, SPF/DKIM/DMARC, rules) is automated by the script.

> Security note: treat the API token like a password. Scope it to this one zone
> as above, and rotate/delete it after setup if you prefer — DNS/routing config
> persists without it.
