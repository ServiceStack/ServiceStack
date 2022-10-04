REM Update servicestack-blazor.js
COPY ServiceStack.Blazor.Tailwind.Tests\Client\wwwroot\js\servicestack-blazor.js ..\..\..\ServiceStack\src\ServiceStack\js\

REM Copy local Blazor Server -> local WASM
RD /q /s ServiceStack.Blazor.Tailwind.Tests\Client\Pages
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Server.Tests\Server\Pages ServiceStack.Blazor.Tailwind.Tests\Client\Pages
DEL ServiceStack.Blazor.Tailwind.Tests\Client\Pages\*.cshtml
COPY ServiceStack.Blazor.Server.Tests\Server\App.razor ServiceStack.Blazor.Tailwind.Tests\Client\

COPY ServiceStack.Blazor.Server.Tests\Server\Configure.* ServiceStack.Blazor.Tailwind.Tests\Server\

RD /q /s ServiceStack.Blazor.Tailwind.Tests\Client\Auth
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Server.Tests\Server\Auth ServiceStack.Blazor.Tailwind.Tests\Client\Auth

RD /q /s ServiceStack.Blazor.Tailwind.Tests\Server\App_Data
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Server.Tests\Server\App_Data ServiceStack.Blazor.Tailwind.Tests\Server\App_Data

RD /q /s ServiceStack.Blazor.Tailwind.Tests\Server\ServiceInterface
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Server.Tests\Server\ServiceInterface ServiceStack.Blazor.Tailwind.Tests\Server\ServiceInterface

RD /q /s ServiceStack.Blazor.Tailwind.Tests\Server\Migrations
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Server.Tests\Server\Migrations ServiceStack.Blazor.Tailwind.Tests\Server\Migrations

DEL ServiceStack.Blazor.Tailwind.Tests\ServiceModel\*.cs
COPY ServiceStack.Blazor.Server.Tests\ServiceModel\*.cs ServiceStack.Blazor.Tailwind.Tests\ServiceModel\


REM Sync local WASM -> blazor-tailwind
SET TO=%NetCoreTemplates%\blazor-tailwind

COPY %TO%\MyApp.Client\MyApp.Client.csproj .
RD /q /s %TO%\MyApp.Client
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Tailwind.Tests\Client %TO%\MyApp.Client
MOVE MyApp.Client.csproj %TO%\MyApp.Client\

COPY %TO%\MyApp\MyApp.csproj .
RD /q /s %TO%\MyApp
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Tailwind.Tests\Server %TO%\MyApp
MOVE MyApp.csproj %TO%\MyApp\

COPY %TO%\MyApp.ServiceModel\MyApp.ServiceModel.csproj .
RD /q /s %TO%\MyApp.ServiceModel
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Tailwind.Tests\ServiceModel %TO%\MyApp.ServiceModel
MOVE MyApp.ServiceModel.csproj %TO%\MyApp.ServiceModel\

COPY %TO%\MyApp.Tests\MyApp.Tests.csproj .
RD /q /s %TO%\MyApp.Tests
XCOPY /Y /E /H /C /I ServiceStack.Blazor.Tailwind.Tests\Tests %TO%\MyApp.Tests
MOVE MyApp.Tests.csproj %TO%\MyApp.Tests\

COPY ServiceStack.Blazor.Tailwind.Tests\ServiceInterface\*.cs %TO%\MyApp.ServiceInterface\

powershell -Command "(Get-Content %TO%\MyApp.Client\package.json) -replace 'Server', 'MyApp' | Out-File -encoding ASCII %TO%\MyApp.Client\package.json"
