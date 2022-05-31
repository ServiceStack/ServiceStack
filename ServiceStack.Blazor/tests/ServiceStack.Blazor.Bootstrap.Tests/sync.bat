set TO=..\..\..\..\NetCoreTemplates\blazor-bootstrap\MyApp.Client

COPY %TO%\MyApp.Client.csproj .
RD /q /s %TO%\MyApp.Client
MD %TO%\MyApp.Client
XCOPY /Y /E /H /C /I Client %TO%\
MOVE MyApp.Client.csproj %TO%\

REM /shared/Brand.html unique to blazor-bootstrap
REM RD /q /s ..\..\..\..\NetCoreTemplates\nextjs\ui\public\modules\
REM RD /q /s ..\..\..\..\NetCoreTemplates\vue-ssg\ui\public\modules\
REM RD /q /s ..\..\..\..\NetCoreTemplates\vue-vite\ui\public\modules\
RD /q /s ..\..\..\..\NetCoreTemplates\blazor-bootstrap\MyApp\wwwroot\modules\
REM XCOPY /Y /E /H /C /I Server\modules ..\..\..\..\NetCoreTemplates\nextjs\ui\public\modules\
REM XCOPY /Y /E /H /C /I Server\modules ..\..\..\..\NetCoreTemplates\vue-ssg\ui\publicwwwroot\modules\
REM XCOPY /Y /E /H /C /I Server\modules ..\..\..\..\NetCoreTemplates\vue-vite\ui\public\modules\
XCOPY /Y /E /H /C /I Server\modules ..\..\..\..\NetCoreTemplates\blazor-bootstrap\MyApp\wwwroot\modules\

COPY Server\*.cs ..\..\..\..\NetCoreTemplates\blazor-bootstrap\MyApp\
COPY ServiceModel\*.cs ..\..\..\..\NetCoreTemplates\blazor-bootstrap\MyApp.ServiceModel\
COPY Tests\*.cs ..\..\..\..\NetCoreTemplates\blazor-bootstrap\MyApp.Tests\
