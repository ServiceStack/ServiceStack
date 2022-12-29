set TO=%NetCoreTemplates%\blazor-wasm\MyApp.Client

COPY %TO%\MyApp.Client.csproj .
RD /q /s %TO%\MyApp.Client
MD %TO%\MyApp.Client
XCOPY /Y /E /H /C /I Client %TO%\
MOVE MyApp.Client.csproj %TO%\

RD /q /s %NetCoreTemplates%\blazor-wasm\MyApp\wwwroot\modules\
XCOPY /Y /E /H /C /I Server\modules %NetCoreTemplates%\blazor-wasm\MyApp\wwwroot\modules\

COPY Server\*.cs %NetCoreTemplates%\blazor-wasm\MyApp\
COPY Server\Migrations\*.cs %NetCoreTemplates%\blazor-wasm\MyApp\Migrations\
COPY ServiceModel\*.cs %NetCoreTemplates%\blazor-wasm\MyApp.ServiceModel\
COPY Tests\*.cs %NetCoreTemplates%\blazor-wasm\MyApp.Tests\
