COPY ServiceStack.Blazor.Tailwind.Tests\Client\wwwroot\js\servicestack-blazor.js ..\..\..\ServiceStack\src\ServiceStack\js\


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
