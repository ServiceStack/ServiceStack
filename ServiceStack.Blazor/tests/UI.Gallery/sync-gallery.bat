REM Copies from Gallery.Server -> Gallery.Wasm -> blazor-gallery.servicestack.net -> blazor-gallery.jamstacks.net

REM Update Gallery.Wasm
SET TO=Gallery.Wasm

RD /q /s %TO%\Client\Pages

RD /q /s %TO%\Server\Migrations
XCOPY /Y /E /H /C /I Gallery.Server\Migrations %TO%\Server\Migrations

RD /q /s %TO%\Server\ServiceInterface
XCOPY /Y /E /H /C /I Gallery.Server\ServiceInterface %TO%\Server\ServiceInterface

RD /q /s %TO%\Client\ServiceModel
XCOPY /Y /E /H /C /I Gallery.Server\ServiceModel %TO%\Client\ServiceModel

XCOPY /Y /E /H /C /I Gallery.Server\Pages %TO%\Client\Pages
DEL %TO%\Client\Pages\*.cshtml

RD /q /s %TO%\Client\wwwroot\css
XCOPY /Y /E /H /C /I Gallery.Server\wwwroot\css %TO%\Client\wwwroot\css

RD /q /s %TO%\Client\wwwroot\img
XCOPY /Y /E /H /C /I Gallery.Server\wwwroot\img %TO%\Client\wwwroot\img

RD /q /s %TO%\Client\wwwroot\tailwind
XCOPY /Y /E /H /C /I Gallery.Server\wwwroot\tailwind %TO%\Client\wwwroot\tailwind


REM Update blazor-gallery.servicestack.net
REM SET TO=C:\src\netcore\BlazorGallery

REM MOVE %TO%\Gallery.Server\Gallery.Server.csproj .

REM RD /q /s %TO%\Gallery.Server

REM XCOPY /Y /E /H /C /I Gallery.Server %TO%\Gallery.Server

REM MOVE Gallery.Server.csproj %TO%\Gallery.Server\
