set TO=C:\src\netcore\BlazorGallery

RD /q /s %TO%\Gallery.Server

XCOPY /Y /E /H /C /I Gallery.Server %TO%\Gallery.Server
