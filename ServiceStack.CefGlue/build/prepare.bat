REM COPY https://cef-builds.spotifycdn.com/index.html#windows64 into C:\src\cef_binary_windows64

XCOPY /E /Y C:\src\cef_binary_windows64\include C:\src\cefglue\CefGlue.Interop.Gen\include\

DEL C:\src\cefglue\CefGlue.Interop.Gen\include\cef_thread.h ..\..\cefglue\CefGlue.Interop.Gen\include\cef_waitable_event.h

PUSHD C:\src\cefglue\CefGlue.Interop.Gen
c:\python27\python.exe -B cefglue_interop_gen.py --schema cef3 --cpp-header-dir include --cefglue-dir ..\CefGlue\ --no-backup
POPD

REM Install GTK# for .NET from https://www.mono-project.com/download/stable/#download-win
PUSHD C:\src\cefglue
build-net45.cmd
POPD

REM copy-xilian.bat
REM copy-cef.bat
REM CI Server: update c:\src\cef_binary_windows64 + c:\src\cef_binary_windows64_client + run ServiceStack.CefGlue build task