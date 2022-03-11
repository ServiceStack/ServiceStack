#!/bin/bash
docker-compose -f ./webhost-tests-environment.yml up -d
export PGSQL_CONNECTION="Server=localhost;Port=48303;User Id=postgres;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200"
export MSSQL_CONNECTION="Server=localhost,48501;Database=master;User Id=sa;Password=Test!tesT;MultipleActiveResultSets=True;"
dotnet clean --framework net6.0 ../tests/ServiceStack.WebHost.Endpoints.Tests/ServiceStack.WebHost.Endpoints.Tests.csproj
dotnet test --framework net6.0 ../tests/ServiceStack.WebHost.Endpoints.Tests/ServiceStack.WebHost.Endpoints.Tests.csproj
