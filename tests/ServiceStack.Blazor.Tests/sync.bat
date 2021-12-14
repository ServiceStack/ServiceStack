set TO=..\..\..\NetCoreTemplates\blazor-wasm\MyApp.Client

COPY %TO%\MyApp.Client.csproj .
RD /q /s %TO%\MyApp.Client
MD %TO%\MyApp.Client
XCOPY /Y /E /H /C /I Client %TO%\
MOVE MyApp.Client.csproj %TO%\

COPY Server\*.cs ..\..\..\NetCoreTemplates\blazor-wasm\MyApp\
COPY ServiceModel\*.cs ..\..\..\NetCoreTemplates\blazor-wasm\MyApp.ServiceModel\
