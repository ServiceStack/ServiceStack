REM Copies from Gallery.Server -> Gallery.Wasm -> blazor-gallery.servicestack.net -> blazor-gallery.jamstacks.net

REM Update tailwind.input.css
COPY Gallery.Server\tailwind.input.css Gallery.Wasm\Gallery.Wasm.Client\
COPY Gallery.Server\tailwind.input.css ..\ServiceStack.Blazor.Server.Tests\Server\
COPY Gallery.Server\tailwind.input.css ..\ServiceStack.Blazor.Tailwind.Tests\Client\

COPY ..\ServiceStack.Blazor.Tailwind.Tests\Client\wwwroot\js\servicestack-blazor.js Gallery.Wasm\Gallery.Wasm.Client\wwwroot\js\
COPY ..\ServiceStack.Blazor.Tailwind.Tests\Client\wwwroot\js\servicestack-blazor.js ..\..\..\ServiceStack\src\ServiceStack\js\


REM Update Gallery.Wasm
SET TO=Gallery.Wasm

RD /q /s %TO%\Gallery.Wasm\App_Data
XCOPY /Y /E /H /C /I Gallery.Server\App_Data %TO%\Gallery.Wasm\App_Data

RD /q /s %TO%\Gallery.Wasm\Migrations
XCOPY /Y /E /H /C /I Gallery.Server\Migrations %TO%\Gallery.Wasm\Migrations

RD /q /s %TO%\Server\ServiceInterface
XCOPY /Y /E /H /C /I Gallery.Server\ServiceInterface %TO%\Gallery.Wasm\ServiceInterface

RD /q /s %TO%\Gallery.Wasm.Client\ServiceModel
XCOPY /Y /E /H /C /I Gallery.Server\ServiceModel %TO%\Gallery.Wasm.Client\ServiceModel

RD /q /s %TO%\Gallery.Wasm.Client\Pages

XCOPY /Y /E /H /C /I Gallery.Server\Pages %TO%\Gallery.Wasm.Client\Pages
DEL %TO%\Gallery.Wasm.Client\Pages\*.cshtml

RD /q /s %TO%\Gallery.Wasm.Client\wwwroot\css
XCOPY /Y /E /H /C /I Gallery.Server\wwwroot\css %TO%\Gallery.Wasm.Client\wwwroot\css

RD /q /s %TO%\Gallery.Wasm.Client\wwwroot\img
XCOPY /Y /E /H /C /I Gallery.Server\wwwroot\img %TO%\Gallery.Wasm.Client\wwwroot\img

RD /q /s %TO%\Gallery.Wasm.Client\wwwroot\profiles
XCOPY /Y /E /H /C /I Gallery.Server\wwwroot\profiles %TO%\Gallery.Wasm.Client\wwwroot\profiles

RD /q /s %TO%\Gallery.Wasm.Client\wwwroot\tailwind
XCOPY /Y /E /H /C /I Gallery.Server\wwwroot\tailwind %TO%\Gallery.Wasm.Client\wwwroot\tailwind


REM Update blazor-gallery.servicestack.net
SET TO=..\..\..\..\..\BlazorGallery

MOVE %TO%\Gallery.Server\Gallery.Server.csproj .

RD /q /s %TO%\Gallery.Server

XCOPY /Y /E /H /C /I Gallery.Server %TO%\Gallery.Server

MOVE Gallery.Server.csproj %TO%\Gallery.Server\


REM Update blazor-gallery.jamstacks.net
SET TO=..\..\..\..\..\BlazorGalleryWasm

MOVE %TO%\Gallery.Wasm.Client\Gallery.Wasm.Client.csproj .

RD /q /s %TO%\Gallery.Wasm.Client

XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm.Client %TO%\Gallery.Wasm.Client

MOVE Gallery.Wasm.Client.csproj %TO%\Gallery.Wasm.Client\

MOVE %TO%\Gallery.Wasm\Gallery.Wasm.csproj .

RD /q /s %TO%\Gallery.Wasm

XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm %TO%\Gallery.Wasm

MOVE Gallery.Wasm.csproj %TO%\Gallery.Wasm\

MOVE %TO%\Gallery.Wasm.Tests\Gallery.Wasm.Tests.csproj .

RD /q /s %TO%\Gallery.Wasm.Tests

XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm.Tests %TO%\Gallery.Wasm.Tests

MOVE Gallery.Wasm.Tests.csproj %TO%\Gallery.Wasm.Tests\
REM ========================================================================
REM Sync Gallery.Unified from Gallery.Wasm
REM ========================================================================

REM Set the destination folder for Gallery.Unified
SET TO=Gallery.Unified

REM Remove and copy App_Data
RD /q /s %TO%\Gallery.Unified\App_Data
XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm\App_Data %TO%\Gallery.Unified\App_Data

REM Remove and copy Migrations
RD /q /s %TO%\Gallery.Unified\Migrations
XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm\Migrations %TO%\Gallery.Unified\Migrations

REM Remove and copy ServiceInterface
RD /q /s %TO%\Gallery.Unified\ServiceInterface
XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm\ServiceInterface %TO%\Gallery.Unified\ServiceInterface

REM Remove and copy ServiceModel
RD /q /s %TO%\Gallery.Unified.Client\ServiceModel
XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm.Client\ServiceModel %TO%\Gallery.Unified.Client\ServiceModel

REM Temp move of App.razor
MOVE %TO%\Gallery.Unified\Components\App.razor %TO%\Gallery.Unified

REM Remove and copy Pages
RD /q /s %TO%\Gallery.Unified\Pages
XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm.Client\Pages %TO%\Gallery.Unified\Pages
DEL %TO%\Gallery.Unified\Pages\*.cshtml

REM Move back
MOVE %TO%\Gallery.Unified\App.razor %TO%\Gallery.Unified\Components\Gallery.Unified

REM Remove and copy Shared
RD /q /s %TO%\Gallery.Unified\Shared
XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm.Client\Shared %TO%\Gallery.Unified\Shared
DEL %TO%\Gallery.Unified\Shared\*.cshtml

REM Remove and copy CSS
RD /q /s %TO%\Gallery.Unified\wwwroot\css
XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm.Client\wwwroot\css %TO%\Gallery.Unified\wwwroot\css

REM Remove and copy img
RD /q /s %TO%\Gallery.Unified\wwwroot\img
XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm.Client\wwwroot\img %TO%\Gallery.Unified\wwwroot\img

REM Remove and copy profiles
RD /q /s %TO%\Gallery.Unified\wwwroot\profiles
XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm.Client\wwwroot\profiles %TO%\Gallery.Unified\wwwroot\profiles

REM Remove and copy tailwind
RD /q /s %TO%\Gallery.Unified\wwwroot\tailwind
XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm.Client\wwwroot\tailwind %TO%\Gallery.Unified\wwwroot\tailwind

REM Copy specific files to Gallery.Unified.Client
COPY Gallery.Wasm\Gallery.Wasm.Client\package.json Gallery.Unified\Gallery.Unified\
COPY Gallery.Wasm\Gallery.Wasm.Client\tailwind.config.js Gallery.Unified\Gallery.Unified\
COPY Gallery.Wasm\Gallery.Wasm.Client\tailwind.input.css Gallery.Unified\Gallery.Unified\
COPY Gallery.Wasm\Gallery.Wasm.Client\_Imports.razor Gallery.Unified\Gallery.Unified\
