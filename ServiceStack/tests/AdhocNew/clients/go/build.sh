#!/bin/bash

rm -f dtos.go && npx get-dtos go http://localhost:5000 --include "ChatCompletion.*"

# Update package name from dtos to main so main.go can compile with it
sed -i 's/^package dtos/package main/' dtos.go

echo "Building and running main.go..."
go run main.go client.go dtos.go
