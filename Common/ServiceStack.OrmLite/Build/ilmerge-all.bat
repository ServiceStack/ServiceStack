CALL ..\..\..\env-vars.bat

PUSHD ..\..\ServiceStack.Interfaces\Build
CALL ilmerge-all.bat 
POPD

REM SET BUILD=Debug
SET BUILD=Release

SET PROJ_LIBS=
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.OrmLite\bin\%BUILD%\ServiceStack.Client.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.OrmLite\bin\%BUILD%\ServiceStack.Common.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.OrmLite\bin\%BUILD%\ServiceStack.OrmLite.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.OrmLite\bin\%BUILD%\ServiceStack.OrmLite.Sqlite.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.OrmLite\bin\%BUILD%\ServiceStack.OrmLite.SqlServer.dll

%ILMERGE_UTIL% /ndebug /t:library /out:ServiceStack.OrmLite.dll %PROJ_LIBS%

COPY *.dll ..\..\..\release\latest\ServiceStack.OrmLite
