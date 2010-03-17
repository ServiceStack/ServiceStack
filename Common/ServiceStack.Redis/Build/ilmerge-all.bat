CALL ..\..\..\env-vars.bat

PUSHD ..\..\ServiceStack.Interfaces\Build
CALL ilmerge-all.bat 
POPD

REM SET BUILD=Debug
SET BUILD=Release

SET PROJ_LIBS=
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Redis\bin\%BUILD%\ServiceStack.Interfaces.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Redis\bin\%BUILD%\ServiceStack.Client.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Redis\bin\%BUILD%\ServiceStack.Common.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Redis\bin\%BUILD%\ServiceStack.Text.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Redis\bin\%BUILD%\ServiceStack.Messaging.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Redis\bin\%BUILD%\ServiceStack.Redis.dll

%ILMERGE_UTIL% /ndebug /t:library /out:ServiceStack.Redis.dll %PROJ_LIBS%

COPY *.dll ..\..\..\release\latest\ServiceStack.Redis
