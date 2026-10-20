#!/usr/bin/env bash
set -euo pipefail
root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
archive=${1:-"$root/artifacts/assembly-search-linux-x64.tar.gz"}
fixture=${2:-"$root/tests/Fixtures/SearchFixture/bin/Release/net10.0/SearchFixture.dll"}
task_tmp=$(mktemp -d -t 'assembly search smoke.XXXXXX')
trap 'rm -rf -- "$task_tmp"' EXIT
tar -xzf "$archive" -C "$task_tmp"
"$task_tmp/cli/assembly-search" --help > "$task_tmp/help.txt"
grep -q -- '--scope' "$task_tmp/help.txt"
"$task_tmp/cli/assembly-search" "$fixture" --query Read --kind MethodDef --json > "$task_tmp/results.json"
python3 - "$task_tmp/results.json" <<'PY'
import json,sys
with open(sys.argv[1]) as f:d=json.load(f)
assert d['schemaVersion']==1
assert d['summary']['status']=='Complete',d['summary']
assert len(d['results'])==3,d['results']
assert all(r['candidate']['token'] for r in d['results'])
print('Self-contained CLI archive smoke: valid JSON, three expected methods, exit 0.')
PY
