#!/bin/bash

# Start the server in the background
cd "$(dirname "$0")"
dotnet run --no-build --urls "http://localhost:5556" > /tmp/openapi3-server.log 2>&1 &
SERVER_PID=$!

# Wait for server to start
echo "Waiting for server to start..."
sleep 5

# Fetch swagger.json
echo "Fetching swagger.json..."
curl -s http://localhost:5556/swagger/v1/swagger.json > /tmp/swagger-test.json

# Check if /types routes are present
echo ""
echo "Checking for hidden routes..."
if grep -q '"/types"' /tmp/swagger-test.json; then
    echo "❌ FAIL: /types route is still present in swagger.json"
    RESULT=1
else
    echo "✅ PASS: /types route is NOT present in swagger.json"
    RESULT=0
fi

if grep -q '"/types/metadata"' /tmp/swagger-test.json; then
    echo "❌ FAIL: /types/metadata route is still present in swagger.json"
    RESULT=1
else
    echo "✅ PASS: /types/metadata route is NOT present in swagger.json"
fi

if grep -q '"/types/csharp"' /tmp/swagger-test.json; then
    echo "❌ FAIL: /types/csharp route is still present in swagger.json"
    RESULT=1
else
    echo "✅ PASS: /types/csharp route is NOT present in swagger.json"
fi

# Check that /hello route is still present (should not be filtered)
if grep -q '"/hello"' /tmp/swagger-test.json; then
    echo "✅ PASS: /hello route is present in swagger.json (as expected)"
else
    echo "❌ FAIL: /hello route is missing from swagger.json"
    RESULT=1
fi

# Kill the server
echo ""
echo "Stopping server..."
kill $SERVER_PID 2>/dev/null

exit $RESULT

