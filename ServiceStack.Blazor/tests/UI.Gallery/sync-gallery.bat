REM Copies from Gallery.Server -> Gallery.Wasm -> blazor-gallery.servicestack.net -> blazor-gallery.jamstacks.net

REM Update Gallery.Wasm
SET TO=Gallery.Wasm

RD /q /s %TO%\Gallery.Wasm\Pages

RD /q /s %TO%\Gallery.Wasm.Server\Migrations
XCOPY /Y /E /H /C /I Gallery.Server\Migrations %TO%\Gallery.Wasm.Server\Migrations

RD /q /s %TO%\Server\ServiceInterface
XCOPY /Y /E /H /C /I Gallery.Server\ServiceInterface %TO%\Gallery.Wasm.Server\ServiceInterface

RD /q /s %TO%\Gallery.Wasm\ServiceModel
XCOPY /Y /E /H /C /I Gallery.Server\ServiceModel %TO%\Gallery.Wasm\ServiceModel

XCOPY /Y /E /H /C /I Gallery.Server\Pages %TO%\Gallery.Wasm\Pages
DEL %TO%\Gallery.Wasm\Pages\*.cshtml

RD /q /s %TO%\Gallery.Wasm\wwwroot\css
XCOPY /Y /E /H /C /I Gallery.Server\wwwroot\css %TO%\Gallery.Wasm\wwwroot\css

RD /q /s %TO%\Gallery.Wasm\wwwroot\img
XCOPY /Y /E /H /C /I Gallery.Server\wwwroot\img %TO%\Gallery.Wasm\wwwroot\img

RD /q /s %TO%\Gallery.Wasm\wwwroot\tailwind
XCOPY /Y /E /H /C /I Gallery.Server\wwwroot\tailwind %TO%\Gallery.Wasm\wwwroot\tailwind


REM Update blazor-gallery.servicestack.net
SET TO=C:\src\netcore\BlazorGallery

MOVE %TO%\Gallery.Server\Gallery.Server.csproj .

RD /q /s %TO%\Gallery.Server

XCOPY /Y /E /H /C /I Gallery.Server %TO%\Gallery.Server

MOVE Gallery.Server.csproj %TO%\Gallery.Server\


REM Update blazor-gallery.jamstacks.net
SET TO=C:\src\netcore\BlazorGalleryWasm

MOVE %TO%\Gallery.Wasm\Gallery.Wasm.csproj .

RD /q /s %TO%\Gallery.Wasm

XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm %TO%\Gallery.Wasm

MOVE Gallery.Wasm.csproj %TO%\Gallery.Wasm\

MOVE %TO%\Gallery.Wasm.Server\Gallery.Wasm.Server.csproj .

RD /q /s %TO%\Gallery.Wasm.Server

XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm.Server %TO%\Gallery.Wasm.Server

MOVE Gallery.Wasm.Server.csproj %TO%\Gallery.Wasm.Server\
