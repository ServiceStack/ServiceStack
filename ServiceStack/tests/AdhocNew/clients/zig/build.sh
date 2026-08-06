#!/bin/bash
set -e

# Generate the DTOs
rm -f dtos.zig && npx get-dtos zig http://localhost:5000 --include "ChatCompletion.*"

# Verify the generated dtos.zig and client code build
echo "Building dtos.zig and main.zig..."
zig build
echo "✓ dtos.zig and main.zig built successfully"
