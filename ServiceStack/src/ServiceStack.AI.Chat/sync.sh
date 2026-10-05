#!/usr/bin/env bash
# Copies shared llms-py assets verbatim; never writes upstream or to host App_Data/custom UI.
# Usage: ./sync.sh [--core | --extension name] [--check | --dry-run] [path-to-llms/llms]
set -euo pipefail
SCRIPT_DIR="$(cd -- "$(dirname -- "$0")" && pwd)"
if command -v python3 >/dev/null 2>&1; then
    exec python3 "$SCRIPT_DIR/sync.py" "$@"
fi
exec python "$SCRIPT_DIR/sync.py" "$@"
