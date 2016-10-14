@echo off
SET BUILD=Debug
REM SET BUILD=Release

SET NOBUILD=--no-build

PUSHD ..\..\..\ServiceStack.Text\src\ServiceStack.Text\
dotnet pack %NOBUILD% --configuration %BUILD%
POPD
COPY ..\..\..\ServiceStack.Text\src\ServiceStack.Text\bin\%BUILD%\ServiceStack.Text.1.0.0.* .\packages\

PUSHD ..\..\..\ServiceStack.Redis\src\ServiceStack.Redis\
dotnet pack %NOBUILD% --configuration %BUILD%
POPD
COPY ..\..\..\ServiceStack.Redis\src\ServiceStack.Redis\bin\%BUILD%\ServiceStack.Redis.1.0.0.* .\packages\

PUSHD ..\..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite\
dotnet pack %NOBUILD% --configuration %BUILD%
POPD
COPY ..\..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite\bin\%BUILD%\ServiceStack.OrmLite.1.0.0.* .\packages\

PUSHD ..\..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.Sqlite\
dotnet pack %NOBUILD% --configuration %BUILD%
POPD
COPY ..\..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.Sqlite\bin\%BUILD%\ServiceStack.OrmLite.Sqlite.1.0.0.* .\packages\

PUSHD ..\..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.SqlServer\
dotnet pack %NOBUILD% --configuration %BUILD%
POPD
COPY ..\..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.SqlServer\bin\%BUILD%\ServiceStack.OrmLite.SqlServer.1.0.0.* .\packages\

PUSHD ..\..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.PostgreSQL\
dotnet pack %NOBUILD% --configuration %BUILD%
POPD
COPY ..\..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.PostgreSQL\bin\%BUILD%\ServiceStack.OrmLite.PostgreSQL.1.0.0.* .\packages\

PUSHD ..\..\..\ServiceStack\src\ServiceStack\
dotnet pack %NOBUILD% --configuration %BUILD%
POPD
COPY ..\..\..\ServiceStack\src\ServiceStack\bin\%BUILD%\ServiceStack.1.0.0.* .\packages\

PUSHD ..\..\..\ServiceStack\src\ServiceStack.Client\
dotnet pack %NOBUILD% --configuration %BUILD%
POPD
COPY ..\..\..\ServiceStack\src\ServiceStack.Client\bin\%BUILD%\ServiceStack.Client.1.0.0.* .\packages\

PUSHD ..\..\..\ServiceStack\src\ServiceStack.Common\
dotnet pack %NOBUILD% --configuration %BUILD%
POPD
COPY ..\..\..\ServiceStack\src\ServiceStack.Common\bin\%BUILD%\ServiceStack.Common.1.0.0.* .\packages\

PUSHD ..\..\..\ServiceStack\src\ServiceStack.HttpClient\
dotnet pack %NOBUILD% --configuration %BUILD%
POPD
COPY ..\..\..\ServiceStack\src\ServiceStack.HttpClient\bin\%BUILD%\ServiceStack.HttpClient.1.0.0.* .\packages\

PUSHD ..\..\..\ServiceStack\src\ServiceStack.Interfaces\
dotnet pack %NOBUILD% --configuration %BUILD%
POPD
COPY ..\..\..\ServiceStack\src\ServiceStack.Interfaces\bin\%BUILD%\ServiceStack.Interfaces.1.0.0.* .\packages\

PUSHD ..\..\..\ServiceStack\src\ServiceStack.Kestrel\
dotnet pack %NOBUILD% --configuration %BUILD%
POPD
COPY ..\..\..\ServiceStack\src\ServiceStack.Kestrel\bin\%BUILD%\ServiceStack.Kestrel.1.0.0.* .\packages\

PUSHD ..\..\..\ServiceStack\src\ServiceStack.Mvc\
dotnet pack %NOBUILD% --configuration %BUILD%
POPD
COPY ..\..\..\ServiceStack\src\ServiceStack.Mvc\bin\%BUILD%\ServiceStack.Mvc.1.0.0.* .\packages\

PUSHD ..\..\..\ServiceStack\src\ServiceStack.Server\
dotnet pack %NOBUILD% --configuration %BUILD%
POPD
COPY ..\..\..\ServiceStack\src\ServiceStack.Server\bin\%BUILD%\ServiceStack.Server.1.0.0.* .\packages\
