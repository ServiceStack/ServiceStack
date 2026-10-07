#!/usr/bin/env bash
# Run from any directory. Python dependencies remain local to this tooling.
set -euo pipefail
security_directory="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
security_python="$security_directory/.venv/bin/python"
if [[ ! -x "$security_python" ]]; then
    python3 -m venv "$security_directory/.venv"
fi
if ! "$security_python" -c 'import importlib.metadata; assert importlib.metadata.version("jsonschema") == "4.25.1"' >/dev/null 2>&1; then
    "$security_python" -m pip install -r "$security_directory/requirements.txt"
fi
exec "$security_python" "$security_directory/security_artifacts.py" generate "$@"
