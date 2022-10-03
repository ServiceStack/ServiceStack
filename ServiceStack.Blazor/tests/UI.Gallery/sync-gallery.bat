REM Copies from Gallery.Server -> Gallery.Wasm -> blazor-gallery.servicestack.net -> blazor-gallery.jamstacks.net

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
SET TO=C:\src\netcore\BlazorGallery

MOVE %TO%\Gallery.Server\Gallery.Server.csproj .

RD /q /s %TO%\Gallery.Server

XCOPY /Y /E /H /C /I Gallery.Server %TO%\Gallery.Server

MOVE Gallery.Server.csproj %TO%\Gallery.Server\


REM Update blazor-gallery.jamstacks.net
SET TO=C:\src\netcore\BlazorGalleryWasm

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
