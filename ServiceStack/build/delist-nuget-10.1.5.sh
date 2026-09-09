#!/usr/bin/env bash

set -euo pipefail

readonly OWNER="servicestack"
readonly VERSION="10.1.5"
readonly SOURCE="https://api.nuget.org/v3/index.json"
readonly SEARCH_URL="https://azuresearch-usnc.nuget.org/query"
readonly FLAT_CONTAINER_URL="https://api.nuget.org/v3-flatcontainer"

execute=false
if [[ "${1:-}" == "--execute" ]]; then
    execute=true
elif [[ $# -ne 0 ]]; then
    echo "Usage: $0 [--execute]" >&2
    exit 2
fi

for command in curl jq dotnet; do
    if ! command -v "$command" >/dev/null 2>&1; then
        echo "Required command not found: $command" >&2
        exit 1
    fi
done

search_result=$(curl --fail --silent --show-error --get "$SEARCH_URL" \
    --data-urlencode "q=owner:${OWNER}" \
    --data-urlencode "prerelease=true" \
    --data-urlencode "semVerLevel=2.0.0" \
    --data-urlencode "skip=0" \
    --data-urlencode "take=1000")

total_hits=$(jq -r '.totalHits' <<<"$search_result")
if [[ ! "$total_hits" =~ ^[0-9]+$ ]] || (( total_hits == 0 || total_hits > 1000 )); then
    echo "Unexpected owner search result count: $total_hits" >&2
    exit 1
fi

mapfile -t owned_packages < <(
    jq -r --arg owner "${OWNER,,}" '
        .data[]
        | select(
            (.owners // []) as $owners
            | (if ($owners | type) == "array" then $owners else [$owners] end)
            | any(ascii_downcase == $owner)
        )
        | .id
    ' <<<"$search_result" | sort -f
)

if (( ${#owned_packages[@]} != total_hits )); then
    echo "Owner verification mismatch: search returned $total_hits results, but ${#owned_packages[@]} were verified as owned by $OWNER." >&2
    exit 1
fi

packages=()
for package in "${owned_packages[@]}"; do
    package_id=${package,,}
    versions_url="$FLAT_CONTAINER_URL/$package_id/index.json"

    if versions=$(curl --fail --silent --show-error "$versions_url"); then
        if jq -e --arg version "$VERSION" '.versions | index($version) != null' <<<"$versions" >/dev/null; then
            packages+=("$package")
        fi
    else
        echo "Unable to inspect versions for $package" >&2
        exit 1
    fi
done

if (( ${#packages[@]} == 0 )); then
    echo "No packages owned by $OWNER currently expose version $VERSION. NuGet indexing may still be in progress."
    exit 0
fi

echo "Found ${#packages[@]} package(s) owned by $OWNER with version $VERSION:"
printf '  %s\n' "${packages[@]}"

if [[ "$execute" == false ]]; then
    echo
    echo "Dry run only. To unlist these package versions, run:"
    echo "  NUGET_API_KEY=... $0 --execute"
    exit 0
fi

if [[ -z "${NUGET_API_KEY:-}" ]]; then
    echo "NUGET_API_KEY must be set when using --execute." >&2
    exit 1
fi

echo
read -r -p "Type 'delist $VERSION' to unlist all packages shown above: " confirmation
if [[ "$confirmation" != "delist $VERSION" ]]; then
    echo "Cancelled."
    exit 1
fi

failures=()
for package in "${packages[@]}"; do
    echo "Unlisting $package $VERSION..."
    if ! dotnet nuget delete "$package" "$VERSION" \
        --source "$SOURCE" \
        --api-key "$NUGET_API_KEY" \
        --non-interactive; then
        failures+=("$package")
    fi
done

if (( ${#failures[@]} > 0 )); then
    echo >&2
    echo "Failed to unlist ${#failures[@]} package(s):" >&2
    printf '  %s\n' "${failures[@]}" >&2
    exit 1
fi

echo "Successfully requested unlisting for ${#packages[@]} package(s) at version $VERSION."
