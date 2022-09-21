set TO=%NetCoreTemplates%\blazor-tailwind\MyApp.Client

COPY %TO%\MyApp.Client.csproj .
RD /q /s %TO%\MyApp.Client
MD %TO%\MyApp.Client
XCOPY /Y /E /H /C /I Client %TO%\
MOVE MyApp.Client.csproj %TO%\

RD /q /s %NetCoreTemplates%\blazor-tailwind\MyApp\wwwroot\modules\
XCOPY /Y /E /H /C /I Server\modules %NetCoreTemplates%\blazor-tailwind\MyApp\wwwroot\modules\

COPY Server\*.cs %NetCoreTemplates%\blazor-tailwind\MyApp\
COPY Server\Migrations\*.cs %NetCoreTemplates%\blazor-tailwind\MyApp\Migrations\
COPY ServiceModel\*.cs %NetCoreTemplates%\blazor-tailwind\MyApp.ServiceModel\
COPY Tests\*.cs %NetCoreTemplates%\blazor-tailwind\MyApp.Tests\

powershell -Command "(Get-Content %TO%\package.json) -replace 'Server', 'MyApp' | Out-File -encoding ASCII %TO%\package.json"
