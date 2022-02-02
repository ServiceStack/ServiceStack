REM RUN prepare.bat

SET DST_PATH=..\src\ServiceStack.CefGlue\CefGlue

RMDIR %DST_PATH% /s /q

XCOPY /E ..\..\cefglue\CefGlue %DST_PATH%\

DEL %DST_PATH%\CefGlue.csproj %DST_PATH%\Interop\Base\cef_string_t.disabled.cs
