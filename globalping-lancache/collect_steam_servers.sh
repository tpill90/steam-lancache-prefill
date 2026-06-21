#!/usr/bin/env bash
#
# collect_steam_servers.sh
# ------------------------
# For every country Globalping has probes in (or a COUNTRIES override), run ONE
# Globalping measurement against Steam's SteamPipe directory FROM that country,
# and save the geo-local server list to <OUTDIR>/<CC>.json.
#
# This is the only rate-limited part (1 test per country). The companion script
# probe_steam_servers.sh then hits every server locally with no rate limit.
#
# Env vars:
#   GLOBALPING_TOKEN  raises the limit to 500/hr+ (else 250/hr per IP)
#   COUNTRIES         space-separated ISO2 list to override auto-discovery
#   MAX_SERVERS       servers to request from the directory (default 30)
#   OUTDIR            output directory (default ./servers)
#   MIN_DELAY         base seconds between calls (default 1)
#   RL_FLOOR          pause when this many tests remain in the window (default 8)

set -uo pipefail

API="https://api.globalping.io/v1"
UA="Valve/Steam HTTP Client 1.0"
STEAM_HOST="api.steampowered.com"
STEAM_PATH="/IContentServerDirectoryService/GetServersForSteamPipe/v1/"
MAX_SERVERS="${MAX_SERVERS:-30}"
OUTDIR="${OUTDIR:-./servers}"
MIN_DELAY="${MIN_DELAY:-1}"
RL_FLOOR="${RL_FLOOR:-8}"
TOKEN="${GLOBALPING_TOKEN:-}"

# curl wrapper: add auth header only when a token is set (bash 3.2 / macOS safe).
req() {
  if [ -n "$TOKEN" ]; then
    curl -H "Authorization: Bearer $TOKEN" "$@"
  else
    curl "$@"
  fi
}

for bin in curl jq; do
  command -v "$bin" >/dev/null 2>&1 || { echo "ERROR: '$bin' is required." >&2; exit 1; }
done
mkdir -p "$OUTDIR"

RL_REMAINING=""
RL_RESET=""
read_rl() {
  local hf="$1" v
  v=$(grep -iE '^(x-)?ratelimit-remaining:' "$hf" 2>/dev/null | tail -1 | awk -F': *' '{print $2}' | tr -dc '0-9')
  [ -n "$v" ] && RL_REMAINING="$v"
  v=$(grep -iE '^(x-)?ratelimit-reset:' "$hf" 2>/dev/null | tail -1 | awk -F': *' '{print $2}' | tr -dc '0-9')
  [ -n "$v" ] && RL_RESET="$v"
}
throttle() {
  if [ -n "$RL_REMAINING" ] && [ "$RL_REMAINING" -le "$RL_FLOOR" ]; then
    local wait="${RL_RESET:-60}"; [ "$wait" -lt 1 ] && wait=60
    echo ">> Rate limit low (${RL_REMAINING} left). Pausing $((wait + 2))s for reset..." >&2
    sleep $((wait + 2)); RL_REMAINING=""
  fi
  sleep "$MIN_DELAY"
}

# Run one directory measurement from country $1; echo the finished JSON or ERR:*
measure_dir() {
  local cc="$1" body hf resp code json id attempt=0 m st
  body=$(jq -n --arg cc "$cc" --arg p "$STEAM_PATH" --arg q "max_servers=${MAX_SERVERS}" \
              --arg t "$STEAM_HOST" --arg ua "$UA" \
    '{type:"http", target:$t, limit:1, locations:[{country:$cc}],
      measurementOptions:{ protocol:"HTTPS",
        request:{ path:$p, query:$q, method:"GET", headers:{ "User-Agent":$ua } } } }')

  while :; do
    throttle
    hf=$(mktemp 2>/dev/null || mktemp -t collectsteam.XXXXXX)
    resp=$(req -sS -D "$hf" -w $'\n%{http_code}' -H 'Content-Type: application/json' \
            -X POST "$API/measurements" --data "$body" 2>/dev/null)
    code=$(printf '%s' "$resp" | tail -n1)
    json=$(printf '%s' "$resp" | sed '$d')
    read_rl "$hf"
    if [ "$code" = "429" ]; then
      local ra; ra=$(grep -iE '^retry-after:' "$hf" | tail -1 | awk -F': *' '{print $2}' | tr -dc '0-9')
      rm -f "$hf"; ra="${ra:-${RL_RESET:-60}}"; [ "$ra" -lt 1 ] && ra=60
      attempt=$((attempt + 1)); [ "$attempt" -gt 5 ] && { echo "ERR:429-giveup"; return 1; }
      echo ">> 429, backing off ${ra}s (attempt ${attempt})..." >&2; sleep $((ra + 2)); continue
    fi
    rm -f "$hf"
    [ "$code" = "202" ] || { echo "ERR:http-$code"; return 1; }
    break
  done

  id=$(printf '%s' "$json" | jq -r '.id // empty')
  [ -n "$id" ] || { echo "ERR:no-id"; return 1; }
  for _ in $(seq 1 40); do
    sleep 1
    m=$(req -sS "$API/measurements/$id" 2>/dev/null)
    st=$(printf '%s' "$m" | jq -r '.status // empty')
    [ "$st" = "finished" ] && { printf '%s' "$m"; return 0; }
    [ "$st" = "failed" ] && { echo "ERR:measurement-failed"; return 1; }
  done
  echo "ERR:poll-timeout"; return 1
}

# --- country list ---
if [ -n "${COUNTRIES:-}" ]; then
  COUNTRY_ARR=()
  for c in $COUNTRIES; do COUNTRY_ARR+=("$c"); done
else
  echo "# Discovering countries with online probes..." >&2
  COUNTRY_ARR=()
  while IFS= read -r _cc; do
    [ -n "$_cc" ] && COUNTRY_ARR+=("$_cc")
  done < <(req -sS "$API/probes" 2>/dev/null | jq -r '[.[].location.country] | unique | .[]' 2>/dev/null)
fi
N=${#COUNTRY_ARR[@]}
[ "$N" -gt 0 ] || { echo "ERROR: could not get country list from Globalping." >&2; exit 1; }
echo "# ${N} countries. One directory lookup each (~${N} tests). 250/hr unregistered, 500/hr with token." >&2

# --- collect ---
for cc in "${COUNTRY_ARR[@]}"; do
  res=$(measure_dir "$cc")
  if [ "${res#ERR:}" != "$res" ]; then
    echo "${cc}: lookup failed (${res})" >&2
    continue
  fi
  body=$(printf '%s' "$res" | jq -r '.results[0].result.rawBody // empty')
  if printf '%s' "$body" | jq -e '.response.servers' >/dev/null 2>&1; then
    printf '%s' "$body" | jq '.' > "$OUTDIR/${cc}.json"
    n=$(printf '%s' "$body" | jq '.response.servers | length')
    echo "${cc}: ${n} servers -> ${OUTDIR}/${cc}.json"
  else
    echo "${cc}: no valid server list (body empty/truncated; try a lower MAX_SERVERS)" >&2
  fi
done
echo "# Done. JSON per country in ${OUTDIR}/" >&2
