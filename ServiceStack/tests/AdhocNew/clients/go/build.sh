#!/bin/bash

rm -f dtos.go && npx get-dtos go https://localhost:5001

# Initialize Go module if not already initialized
if [ ! -f "go.mod" ]; then
    go mod init adhocnew/clients/go
fi

# Build dtos.go to check for errors
echo "Building dtos.go..."
go build -o /dev/null dtos.go

if [ $? -eq 0 ]; then
    echo "✓ dtos.go compiled successfully"
else
    echo "✗ dtos.go has build errors"
    exit 1
fi
