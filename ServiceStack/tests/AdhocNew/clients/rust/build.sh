#!/bin/bash

rm -f dtos.rs && npx get-dtos rust http://localhost:5000 --include "ChatCompletion.*"

echo "Checking dtos.rs and client code..."
cargo check

if [ $? -eq 0 ]; then
    echo "✓ dtos.rs and client code passed check"
else
    echo "✗ dtos.rs or client code check failed"
    exit 1
fi
