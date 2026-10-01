#!/usr/bin/env bash
set -e

dotnet test /home/mythz/src/ServiceStack/ServiceStack/ServiceStack/tests/NetCoreTests/NetCoreTests.csproj --filter "Name~Publish_ui" --logger "console;verbosity=minimal"
