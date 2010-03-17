CALL ..\..\..\env-vars.bat

PUSHD ..\..\ServiceStack.Interfaces\Build
CALL ilmerge-all.bat 
POPD

REM SET BUILD=Debug
SET BUILD=Release

REM DON'T NEED TO ILMERGE BECAUSE THERE IS ONLY 1 ASSEMBLY

SET PROJ_LIBS=
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Redis\bin\%BUILD%\ServiceStack.Text.dll

%ILMERGE_UTIL% /ndebug /t:library /out:ServiceStack.Text.dll %PROJ_LIBS%

COPY *.dll ..\..\..\release\latest\ServiceStack.Text
