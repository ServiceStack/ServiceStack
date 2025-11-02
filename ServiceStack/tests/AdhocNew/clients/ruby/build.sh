#!/bin/bash

rm -f dtos.rb && npx get-dtos ruby https://localhost:5001

# Check dtos.rb for syntax errors
echo "Checking dtos.rb for syntax errors..."
ruby -c dtos.rb

if [ $? -eq 0 ]; then
    echo "✓ dtos.rb has no syntax errors"
else
    echo "✗ dtos.rb has syntax errors"
    exit 1
fi

rm -f dtos.rb && npx get-dtos ruby https://localhost:5001 --include "ChatCompletion.*"

# Check dtos.rb for syntax errors
echo "Checking dtos.rb for syntax errors..."
ruby -c dtos.rb

if [ $? -eq 0 ]; then
    echo "✓ dtos.rb has no syntax errors"
else
    echo "✗ dtos.rb has syntax errors"
    exit 1
fi
