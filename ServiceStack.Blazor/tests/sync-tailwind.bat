REM Update servicestack-blazor.js
COPY ServiceStack.Blazor.Tailwind.Tests\Client\wwwroot\js\servicestack-blazor.js ..\..\ServiceStack\src\ServiceStack\js\

REM Copy local Blazor Server -> local WASM
RD /q /s ServiceStack.Blazor.Tailwind.Tests\Client\wwwroot\img
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Server.Tests\Server\wwwroot\img ServiceStack.Blazor.Tailwind.Tests\Client\wwwroot\img
RD /q /s ServiceStack.Blazor.Tailwind.Tests\Client\wwwroot\css
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Server.Tests\Server\wwwroot\css ServiceStack.Blazor.Tailwind.Tests\Client\wwwroot\css
RD /q /s ServiceStack.Blazor.Tailwind.Tests\Client\wwwroot\tailwind
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Server.Tests\Server\wwwroot\tailwind ServiceStack.Blazor.Tailwind.Tests\Client\wwwroot\tailwind

RD /q /s ServiceStack.Blazor.Tailwind.Tests\Client\Pages
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Server.Tests\Server\Pages ServiceStack.Blazor.Tailwind.Tests\Client\Pages
DEL ServiceStack.Blazor.Tailwind.Tests\Client\Pages\*.cshtml
COPY ServiceStack.Blazor.Server.Tests\Server\App.razor ServiceStack.Blazor.Tailwind.Tests\Client\

COPY ServiceStack.Blazor.Server.Tests\Server\Configure.* ServiceStack.Blazor.Tailwind.Tests\Server\

RD /q /s ServiceStack.Blazor.Tailwind.Tests\Server\App_Data
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Server.Tests\Server\App_Data ServiceStack.Blazor.Tailwind.Tests\Server\App_Data

RD /q /s ServiceStack.Blazor.Tailwind.Tests\Server\ServiceInterface
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Server.Tests\Server\ServiceInterface ServiceStack.Blazor.Tailwind.Tests\Server\ServiceInterface

RD /q /s ServiceStack.Blazor.Tailwind.Tests\Server\Migrations
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Server.Tests\Server\Migrations ServiceStack.Blazor.Tailwind.Tests\Server\Migrations

DEL ServiceStack.Blazor.Tailwind.Tests\ServiceModel\*.cs
COPY ServiceStack.Blazor.Server.Tests\ServiceModel\*.cs ServiceStack.Blazor.Tailwind.Tests\ServiceModel\

REM Sync local Server -> blazor-server
SET TO=%NetCoreTemplates%\blazor-server

COPY %TO%\MyApp\MyApp.csproj .
RD /q /s %TO%\MyApp
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Server.Tests\Server %TO%\MyApp
RD /q /s %TO%\MyApp\ServiceInterface\
DEL %TO%\MyApp\MyApp.Server.*
MOVE MyApp.csproj %TO%\MyApp\

COPY %TO%\MyApp.ServiceModel\MyApp.ServiceModel.csproj .
RD /q /s %TO%\MyApp.ServiceModel
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Server.Tests\ServiceModel %TO%\MyApp.ServiceModel
MOVE MyApp.ServiceModel.csproj %TO%\MyApp.ServiceModel\

COPY %TO%\MyApp.Tests\MyApp.Tests.csproj .
COPY %TO%\MyApp.Tests\appsettings.json .
RD /q /s %TO%\MyApp.Tests
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Server.Tests\Tests %TO%\MyApp.Tests
MOVE MyApp.Tests.csproj %TO%\MyApp.Tests\
MOVE appsettings.json %TO%\MyApp.Tests\

DEL %TO%\MyApp.ServiceInterface\*.cs
COPY ServiceStack.Blazor.Server.Tests\Server\ServiceInterface\*.cs %TO%\MyApp.ServiceInterface\


REM Sync local WASM -> blazor-tailwind
SET TO=%NetCoreTemplates%\blazor-tailwind

COPY %TO%\MyApp.Client\MyApp.Client.csproj .
RD /q /s %TO%\MyApp.Client
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Tailwind.Tests\Client %TO%\MyApp.Client
MOVE MyApp.Client.csproj %TO%\MyApp.Client\

COPY %TO%\MyApp\MyApp.csproj .
RD /q /s %TO%\MyApp
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Tailwind.Tests\Server %TO%\MyApp
RD /q /s %TO%\MyApp\ServiceInterface\
DEL %TO%\MyApp\MyApp.Server.*
MOVE MyApp.csproj %TO%\MyApp\

COPY %TO%\MyApp.ServiceModel\MyApp.ServiceModel.csproj .
RD /q /s %TO%\MyApp.ServiceModel
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Tailwind.Tests\ServiceModel %TO%\MyApp.ServiceModel
MOVE MyApp.ServiceModel.csproj %TO%\MyApp.ServiceModel\

COPY %TO%\MyApp.Tests\MyApp.Tests.csproj .
COPY %TO%\MyApp.Tests\appsettings.json .
RD /q /s %TO%\MyApp.Tests
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Tailwind.Tests\Tests %TO%\MyApp.Tests
MOVE MyApp.Tests.csproj %TO%\MyApp.Tests\
MOVE appsettings.json %TO%\MyApp.Tests\

COPY ServiceStack.Blazor.Tailwind.Tests\ServiceInterface\*.cs %TO%\MyApp.ServiceInterface\

powershell -Command "(Get-Content %TO%\MyApp.Client\package.json) -replace 'Server', 'MyApp' | Out-File -encoding ASCII %TO%\MyApp.Client\package.json"
