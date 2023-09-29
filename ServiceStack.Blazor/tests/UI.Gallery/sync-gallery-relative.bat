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

REM Create a temp directory
MD TempDir

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
MOVE %TO%\Gallery.Unified.Client\App.razor %TO%\Gallery.Unified

COPY %TO%\Gallery.Unified.Client\Pages\SignIn.razor TempDir\
COPY %TO%\Gallery.Unified.Client\Pages\SignUp.razor TempDir\
COPY %TO%\Gallery.Unified.Client\Pages\Profile.razor TempDir\

REM Remove and copy Pages
RD /q /s %TO%\Gallery.Unified.Client\Pages
XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm.Client\Pages %TO%\Gallery.Unified.Client\Pages
DEL %TO%\Gallery.Unified\Pages.Client\*.cshtml

MOVE TempDir\SignIn.razor %TO%\Gallery.Unified.Client\Pages\
MOVE TempDir\SignUp.razor %TO%\Gallery.Unified.Client\Pages\
MOVE TempDir\Profile.razor %TO%\Gallery.Unified.Client\Pages\

REM Move back
MOVE %TO%\Gallery.Unified\App.razor %TO%\Gallery.Unified.Client\App.razor

REM Copy the files to exclude to the temp directory
COPY %TO%\Gallery.Unified.Client\Shared\Header.razor TempDir\
COPY %TO%\Gallery.Unified.Client\Shared\MainLayout.razor TempDir\
COPY %TO%\Gallery.Unified.Client\Shared\Sidebar.razor TempDir\

REM Remove and copy Shared
RD /q /s %TO%\Gallery.Unified.Client\Shared
XCOPY /Y /E /H /C /I Gallery.Wasm\Gallery.Wasm.Client\Shared %TO%\Gallery.Unified.Client\Shared
DEL %TO%\Gallery.Unified.Client\Shared\*.cshtml

REM Move the excluded files back from the temp directory to Gallery.Unified.Client
MOVE TempDir\Header.razor %TO%\Gallery.Unified.Client\Shared\
MOVE TempDir\MainLayout.razor %TO%\Gallery.Unified.Client\Shared\
MOVE TempDir\Sidebar.razor %TO%\Gallery.Unified.Client\Shared\

REM Delete the temp directory
RD /q /s TempDir

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

REM ========================================================================
REM Sync Gallery.Unified to NetCoreApps project
REM ========================================================================


REM Update BlazorGalleryUnified from Gallery.Unified
SET TO=..\..\..\..\..\BlazorGalleryUnified

REM Move the existing Gallery.Unified.Client.csproj file to the current directory
MOVE %TO%\Gallery.Unified.Client\Gallery.Unified.Client.csproj .

REM Remove the existing Gallery.Unified.Client directory
RD /q /s %TO%\Gallery.Unified.Client

REM Copy Gallery.Unified.Client files to BlazorGalleryUnified
XCOPY /Y /E /H /C /I Gallery.Unified\Gallery.Unified.Client %TO%\Gallery.Unified.Client

REM Move Gallery.Unified.Client.csproj back to the Gallery.Unified.Client directory
MOVE Gallery.Unified.Client.csproj %TO%\Gallery.Unified.Client\

REM Move the existing Gallery.Unified.csproj file to the current directory
MOVE %TO%\Gallery.Unified\Gallery.Unified.csproj .

REM Remove the existing Gallery.Unified directory
RD /q /s %TO%\Gallery.Unified

REM Copy Gallery.Unified files to BlazorGalleryUnified
XCOPY /Y /E /H /C /I Gallery.Unified\Gallery.Unified %TO%\Gallery.Unified

REM Move Gallery.Unified.csproj back to the Gallery.Unified directory
MOVE Gallery.Unified.csproj %TO%\Gallery.Unified\

REM Move the existing Gallery.Unified.Tests.csproj file to the current directory
MOVE %TO%\Gallery.Unified.Tests\Gallery.Unified.Tests.csproj .

REM Remove the existing Gallery.Unified.Tests directory
RD /q /s %TO%\Gallery.Unified.Tests

REM Copy Gallery.Unified.Tests files to BlazorGalleryUnified
XCOPY /Y /E /H /C /I Gallery.Unified\Gallery.Unified.Tests %TO%\Gallery.Unified.Tests

REM Move Gallery.Unified.Tests.csproj back to the Gallery.Unified.Tests directory
MOVE Gallery.Unified.Tests.csproj %TO%\Gallery.Unified.Tests\

