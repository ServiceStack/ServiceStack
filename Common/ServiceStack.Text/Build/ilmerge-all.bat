
REM SET BUILD=Debug
SET BUILD=Release

COPY ..\ServiceStack.Text\bin\%BUILD%\* ..\..\..\release\latest\ServiceStack.Text\




REM DON'T NEED TO ILMERGE BECAUSE THERE IS ONLY 1 ASSEMBLY

REM CALL ..\..\..\env-vars.bat

REM PUSHD ..\..\ServiceStack.Interfaces\Build
REM CALL ilmerge-all.bat 
REM POPD

REM SET PROJ_LIBS=
REM SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Redis\bin\%BUILD%\ServiceStack.Text.dll

REM %ILMERGE_UTIL% /ndebug /t:library /out:ServiceStack.Text.dll %PROJ_LIBS%

REM COPY *.dll ..\..\..\release\latest\ServiceStack.Text
