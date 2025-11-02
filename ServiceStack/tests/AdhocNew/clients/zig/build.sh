#!/bin/bash

# Generate the DTOs
rm -f dtos.zig && npx get-dtos zig https://localhost:5001

# Verify the generated dtos.zig has no syntax or build errors
if [ -f dtos.zig ]; then
    echo "Verifying dtos.zig syntax..."
    zig ast-check dtos.zig
    if [ $? -eq 0 ]; then
        echo "✓ dtos.zig syntax verification passed"
    else
        echo "✗ dtos.zig syntax verification failed"
        exit 1
    fi
else
    echo "✗ dtos.zig was not generated"
    exit 1
fi

rm -f dtos.zig && npx get-dtos zig https://localhost:5001 --include "ChatCompletion.*"

# Verify the generated dtos.zig has no syntax or build errors
if [ -f dtos.zig ]; then
    echo "Verifying dtos.zig syntax..."
    zig ast-check dtos.zig
    if [ $? -eq 0 ]; then
        echo "✓ dtos.zig syntax verification passed"
    else
        echo "✗ dtos.zig syntax verification failed"
        exit 1
    fi
else
    echo "✗ dtos.zig was not generated"
    exit 1
fi
