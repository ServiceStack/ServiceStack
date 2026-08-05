#!/bin/bash

rm -f dtos/dtos.go && (cd dtos && npx get-dtos go http://localhost:5000 --include "ChatCompletion.*")

echo "Building and running main.go..."
go run .
