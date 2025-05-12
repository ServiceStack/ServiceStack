#!/bin/bash

# Update servicestack-blazor.js
cp ServiceStack.Blazor.Tailwind.Tests/Client/wwwroot/js/servicestack-blazor.js ../../ServiceStack/src/ServiceStack/js/

# Copy local Blazor Server -> local WASM
rm -rf ServiceStack.Blazor.Tailwind.Tests/Client/wwwroot/img
cp -r ServiceStack.Blazor.Server.Tests/Server/wwwroot/img ServiceStack.Blazor.Tailwind.Tests/Client/wwwroot/img
rm -rf ServiceStack.Blazor.Tailwind.Tests/Client/wwwroot/css
cp -r ServiceStack.Blazor.Server.Tests/Server/wwwroot/css ServiceStack.Blazor.Tailwind.Tests/Client/wwwroot/css
rm -rf ServiceStack.Blazor.Tailwind.Tests/Client/wwwroot/tailwind
cp -r ServiceStack.Blazor.Server.Tests/Server/wwwroot/tailwind ServiceStack.Blazor.Tailwind.Tests/Client/wwwroot/tailwind

rm -rf ServiceStack.Blazor.Tailwind.Tests/Client/Pages
cp -r ServiceStack.Blazor.Server.Tests/Server/Pages ServiceStack.Blazor.Tailwind.Tests/Client/Pages
rm ServiceStack.Blazor.Tailwind.Tests/Client/Pages/*.cshtml
# cp ServiceStack.Blazor.Server.Tests/Server/App.razor ServiceStack.Blazor.Tailwind.Tests/Client/

cp ServiceStack.Blazor.Server.Tests/Server/Configure.* ServiceStack.Blazor.Tailwind.Tests/Server/

rm -rf ServiceStack.Blazor.Tailwind.Tests/Server/App_Data
cp -r ServiceStack.Blazor.Server.Tests/Server/App_Data ServiceStack.Blazor.Tailwind.Tests/Server/App_Data

rm -rf ServiceStack.Blazor.Tailwind.Tests/Server/ServiceInterface
cp -r ServiceStack.Blazor.Server.Tests/Server/ServiceInterface ServiceStack.Blazor.Tailwind.Tests/Server/ServiceInterface

rm -rf ServiceStack.Blazor.Tailwind.Tests/Server/Migrations
cp -r ServiceStack.Blazor.Server.Tests/Server/Migrations ServiceStack.Blazor.Tailwind.Tests/Server/Migrations

rm ServiceStack.Blazor.Tailwind.Tests/ServiceModel/*.cs
cp ServiceStack.Blazor.Server.Tests/ServiceModel/*.cs ServiceStack.Blazor.Tailwind.Tests/ServiceModel/
