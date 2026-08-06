#!/bin/bash

rm -f dtos.rb && npx get-dtos ruby http://localhost:5000 --include "ChatCompletion.*"

# Check dtos.rb and main.rb for syntax errors
echo "Checking Ruby files for syntax errors..."
ruby -c dtos.rb && ruby -c main.rb

if [ $? -eq 0 ]; then
    echo "✓ Ruby files have no syntax errors"
else
    echo "✗ Syntax errors found"
    exit 1
fi
