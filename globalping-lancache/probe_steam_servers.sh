#!/usr/bin/env bash
#
# probe_steam_servers.sh
# ----------------------
# Reads every <CC>.json produced by collect_steam_servers.sh, gathers the unique
# set of content servers across all of them, and hits each one LOCALLY with curl
# (direct to origin, no Globalping, no rate limit). Prints one line per host:
#
#   cache1-sof-glbl.steamcontent.com: 200 | OpenCache bypass: true
#
# Env vars:
#   INDIR            directory of <CC>.json files (default ./servers)
#   DEPOT_CHUNK      chunk path to request (must be a currently-valid chunk)
#   CONNECT_TIMEOUT  per-host connect timeout in seconds (default 8)
#   PARALLEL         number of concurrent curls (default 1; try 16 to go faster)
#   OUTFILE          if set, also write results here

set -uo pipefail

INDIR="${INDIR:-./servers}"
DEPOT_CHUNK="${DEPOT_CHUNK:-/depot/3527290/chunk/e41acf05fcdcb9588846b2aadde861bfda765c64}"
CONNECT_TIMEOUT="${CONNECT_TIMEOUT:-8}"
MAX_TIME="${MAX_TIME:-25}"
PARALLEL="${PARALLEL:-1}"
UA="Valve/Steam HTTP Client 1.0"
OUTFILE="${OUTFILE:-}"

command -v jq >/dev/null 2>&1 || { echo "ERROR: 'jq' is required." >&2; exit 1; }

# Capture the JSON file list up front (outside any pipeline). If the glob does
# not match, the literal pattern remains, so test that the first entry exists.
set -- "$INDIR"/*.json
if [ ! -e "$1" ]; then
  echo "ERROR: no <CC>.json files found in '${INDIR}'. Run collect_steam_servers.sh first." >&2
  exit 1
fi
num_files=$#

# Collect "host<TAB>opencache" from every JSON, then dedupe to one line per host.
tmp=$(mktemp 2>/dev/null || mktemp -t probesteam.XXXXXX)
trap 'rm -f "$tmp"' EXIT

for f in "$@"; do
  jq -r '.response.servers[]?
         | "\(.host)\t\(((.bypass_proxies_of_type // []) | index("OpenCache")) != null)"' \
     "$f" 2>/dev/null
done | sort -t"$(printf '\t')" -k1,1 -u > "$tmp"

total=$(wc -l < "$tmp" | tr -d ' ')
echo "# ${total} unique hosts across ${num_files} countries; probing locally (parallel=${PARALLEL})..." >&2

# Probe one host: prints the result line.
probe_one() {
  host="$1"; oc="$2"
  status=$(curl -o /dev/null -sL \
              --connect-timeout "$CONNECT_TIMEOUT" \
              --max-time "$MAX_TIME" \
              --max-redirs 5 \
              -A "$UA" -w '%{http_code}' "http://${host}${DEPOT_CHUNK}" 2>/dev/null)
  # curl prints 000 (and we may get empty) when it times out / gets no response.
  case "$status" in
    ""|000) status="000(timeout/no-response)" ;;
  esac
  printf '%s: %s | OpenCache bypass: %s\n' "$host" "$status" "$oc"
}
export -f probe_one
export UA CONNECT_TIMEOUT MAX_TIME DEPOT_CHUNK

run() {
  if [ "$PARALLEL" -gt 1 ]; then
    # xargs runs probe_one in parallel; output order is not guaranteed.
    awk -F"$(printf '\t')" '{print $1" "$2}' "$tmp" \
      | xargs -P "$PARALLEL" -n 2 bash -c 'probe_one "$0" "$1"'
  else
    while IFS="$(printf '\t')" read -r host oc; do
      [ -n "$host" ] && probe_one "$host" "$oc"
    done < "$tmp"
  fi
}

if [ -n "$OUTFILE" ]; then
  run | tee "$OUTFILE"
  echo "# Results written to ${OUTFILE}" >&2
else
  run
fi
