#!/usr/bin/env bash
set -euo pipefail
root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
cd "$root"
mkdir -p artifacts
stage=$(mktemp -d "$root/artifacts/stage.XXXXXX")
trap 'rm -rf -- "$stage"' EXIT
dotnet publish src/AssemblySearch.Cli -c Release -r linux-x64 --self-contained true -p:PublishTrimmed=false -o "$stage/cli"
dotnet publish src/AssemblySearch.Desktop -c Release -r linux-x64 --self-contained true -p:PublishTrimmed=false -o "$stage/desktop"
cp README.md THIRD_PARTY_NOTICES.md "$stage/"
cp docs/verification.md "$stage/VERIFICATION.md"
tar -czf "$root/artifacts/assembly-search-linux-x64.tar.gz" -C "$stage" .
sha256sum artifacts/assembly-search-linux-x64.tar.gz > artifacts/SHA256SUMS
printf 'Linux package: %s\n' "$root/artifacts/assembly-search-linux-x64.tar.gz"
