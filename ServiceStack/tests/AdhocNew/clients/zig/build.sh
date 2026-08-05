#!/bin/bash
set -e

# Generate the DTOs
rm -f dtos.zig && npx get-dtos zig http://localhost:5000 --include "ChatCompletion.*"

# Verify the generated dtos.zig and client code have no syntax or build errors
if [ -f dtos.zig ]; then
    echo "Checking dtos.zig and client code..."
    zig ast-check dtos.zig
    zig ast-check client.zig
    zig ast-check main.zig
    echo "✓ dtos.zig and client code passed check"
else
    echo "✗ dtos.zig was not generated"
    exit 1
fi
