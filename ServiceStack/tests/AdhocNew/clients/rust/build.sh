#!/bin/bash

rm -f dtos.rs && npx get-dtos rust https://localhost:5001

# Check dtos.rs for syntax errors
echo "Checking dtos.rs for syntax errors..."

# Build the library
BUILD_OUTPUT=$(cargo build 2>&1)
BUILD_EXIT_CODE=$?

# Show first 30 lines of output
echo "$BUILD_OUTPUT" | head -30

if [ $BUILD_EXIT_CODE -eq 0 ]; then
    echo "✓ dtos.rs has no syntax or build errors"
else
    echo "✗ dtos.rs has syntax or build errors"
    exit 1
fi

rm -f dtos.rs && npx get-dtos rust https://localhost:5001 --include "ChatCompletion.*"

# Build the library
BUILD_OUTPUT=$(cargo build 2>&1)
BUILD_EXIT_CODE=$?

# Show first 30 lines of output
echo "$BUILD_OUTPUT" | head -30

if [ $BUILD_EXIT_CODE -eq 0 ]; then
    echo "✓ dtos.rs has no syntax or build errors"
else
    echo "✗ dtos.rs has syntax or build errors"
    exit 1
fi
