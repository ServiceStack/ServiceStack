#!/usr/bin/env bash
# Syncs UI + config assets from the llms-py project (source of truth) into chat/
# Usage: ./sync.sh [path-to-llms-package-dir]
# Platform-specific behavior is applied server-side (base url substitution in ai.mjs/index.html),
# so all synced files remain byte-identical to the Python originals.
set -e

cd "$(dirname "$0")"
LLMS="${1:-${LLMS:-../../../../llms/llms}}"
# User extensions live outside the package, in the llms home dir (each is its own repo)
LLMS_HOME="${LLMS_HOME:-$LLMS/../llms-home}"

if [ ! -f "$LLMS/index.html" ]; then
    echo "llms package dir not found at: $LLMS" >&2
    echo "Usage: ./sync.sh /path/to/llms/llms" >&2
    exit 1
fi

# Extensions not ported: auth is provided by ServiceStack Identity Auth instead
# (credentials has its own C#-only UI signing in with the host's Identity users)
SKIP_EXT="credentials github_auth browser"
# Ported extensions synced from $LLMS_HOME/extensions instead of $LLMS/extensions
HOME_EXT="gemini"
# UI extensions not under $LLMS/extensions, so never deleted as stale:
# identity + credentials are C#-only, gemini is synced from $LLMS_HOME above
KEEP_EXT="identity credentials gemini"

# clear stale config files
rm -f ../../tests/NorthwindAuto/App_Data/chat/llms.json
rm -f ../../tests/NorthwindAuto/App_Data/chat/providers.json
rm -f ../../tests/NorthwindAuto/App_Data/chat/providers-extra.json

echo "Syncing from $LLMS"

# Core UI (verbatim, incl. ai.mjs)
mkdir -p chat/ui chat/ext
rsync -a --delete "$LLMS/ui/" chat/ui/

# SPA entry + seed configs
cp "$LLMS/index.html" chat/index.html
cp "$LLMS/llms.json" chat/llms.json
cp "$LLMS/providers.json" chat/providers.json
cp "$LLMS/providers-extra.json" chat/providers-extra.json

# Server-side assets the C# ports read at runtime the same way Python does, e.g. the AI prompts
# and the pdf starter templates. Synced after ui/ because that rsync --deletes anything else in the dir.
SERVER_ASSETS="prompts examples"

# Extension UIs -> chat/ext/<name>/
for ext in "$LLMS"/extensions/*/; do
    name=$(basename "$ext")
    if [[ " $SKIP_EXT " == *" $name "* ]]; then continue; fi
    if [ -d "$ext/ui" ]; then
        mkdir -p "chat/ext/$name"
        rsync -a --delete "$ext/ui/" "chat/ext/$name/"
    fi
    for asset in $SERVER_ASSETS; do
        if [ -d "$ext/$asset" ]; then
            mkdir -p "chat/ext/$name/$asset"
            rsync -a --delete "$ext/$asset/" "chat/ext/$name/$asset/"
        fi
    done
done

# User extension UIs (llms-home) -> chat/ext/<name>/
for name in $HOME_EXT; do
    ext="$LLMS_HOME/extensions/$name"
    if [ -d "$ext/ui" ]; then
        mkdir -p "chat/ext/$name"
        rsync -aL --delete "$ext/ui/" "chat/ext/$name/"
    else
        echo "Skipping $name: $ext/ui not found" >&2
    fi
done

# Remove synced ext dirs that no longer exist upstream (preserving KEEP_EXT)
for dir in chat/ext/*/; do
    [ -d "$dir" ] || continue
    name=$(basename "$dir")
    if [[ " $KEEP_EXT " == *" $name "* ]]; then continue; fi
    if [ ! -d "$LLMS/extensions/$name/ui" ]; then
        echo "Removing stale chat/ext/$name"
        rm -rf "$dir"
    fi
done

# App themes + Agent profiles
rsync -a --delete "$LLMS/extensions/app/themes/" chat/themes/
rsync -a --delete "$LLMS/extensions/agents/profiles/" chat/profiles/

VERSION=$(grep -o "version: '[^']*'" chat/ui/ai.mjs | head -1 | cut -d"'" -f2)
echo "Synced llms-py v$VERSION"

cp -f ./chat/ui/components/* ../../tests/NorthwindAuto/wwwroot/js/components

