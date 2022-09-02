set TO=..\..\..\..\NetCoreTemplates\blazor-tailwind\MyApp.Client

COPY %TO%\MyApp.Client.csproj .
RD /q /s %TO%\MyApp.Client
MD %TO%\MyApp.Client
XCOPY /Y /E /H /C /I Client %TO%\
MOVE MyApp.Client.csproj %TO%\

REM /shared/Brand.html unique to blazor-tailwind
REM RD /q /s ..\..\..\..\NetCoreTemplates\nextjs\ui\public\modules\
REM RD /q /s ..\..\..\..\NetCoreTemplates\vue-ssg\ui\public\modules\
REM RD /q /s ..\..\..\..\NetCoreTemplates\vue-vite\ui\public\modules\
RD /q /s ..\..\..\..\NetCoreTemplates\blazor-tailwind\MyApp\wwwroot\modules\
REM XCOPY /Y /E /H /C /I Server\modules ..\..\..\..\NetCoreTemplates\nextjs\ui\public\modules\
REM XCOPY /Y /E /H /C /I Server\modules ..\..\..\..\NetCoreTemplates\vue-ssg\ui\publicwwwroot\modules\
REM XCOPY /Y /E /H /C /I Server\modules ..\..\..\..\NetCoreTemplates\vue-vite\ui\public\modules\
XCOPY /Y /E /H /C /I Server\modules ..\..\..\..\NetCoreTemplates\blazor-tailwind\MyApp\wwwroot\modules\

COPY Server\*.cs ..\..\..\..\NetCoreTemplates\blazor-tailwind\MyApp\
COPY Server\Migrations\*.cs ..\..\..\..\NetCoreTemplates\blazor-tailwind\MyApp\Migrations\
COPY ServiceModel\*.cs ..\..\..\..\NetCoreTemplates\blazor-tailwind\MyApp.ServiceModel\
COPY Tests\*.cs ..\..\..\..\NetCoreTemplates\blazor-tailwind\MyApp.Tests\

powershell -Command "(Get-Content %TO%\package.json) -replace 'Server', 'MyApp' | Out-File -encoding ASCII %TO%\package.json"
