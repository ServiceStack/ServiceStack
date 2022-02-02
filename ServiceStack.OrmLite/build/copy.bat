REM SET BUILD=Debug
SET BUILD=Release

MD ..\..\ServiceStack.OrmLite\NuGet\ServiceStack.OrmLite.SqlServer\lib
MD ..\..\ServiceStack.OrmLite\NuGet\ServiceStack.OrmLite.MySql\lib
MD ..\..\ServiceStack.OrmLite\NuGet\ServiceStack.OrmLite.PostgreSQL\lib
MD ..\..\ServiceStack.OrmLite\NuGet\ServiceStack.OrmLite.Oracle\lib
MD ..\..\ServiceStack.OrmLite\NuGet\ServiceStack.OrmLite.Firebird\lib
MD ..\..\ServiceStack.OrmLite\NuGet\ServiceStack.OrmLite.T4\content

COPY ..\src\T4\*.*  ..\..\ServiceStack.OrmLite\NuGet\ServiceStack.OrmLite.T4\content

COPY ..\src\ServiceStack.OrmLite\bin\%BUILD%\net45\ServiceStack.OrmLite.* ..\..\ServiceStack\lib\net45
COPY ..\src\ServiceStack.OrmLite\bin\%BUILD%\netstandard2.0\ServiceStack.OrmLite.* ..\..\ServiceStack\lib\netstandard2.0
COPY ..\src\ServiceStack.OrmLite.Sqlite\bin\%BUILD%\net45\ServiceStack.OrmLite.Sqlite.* ..\..\ServiceStack\lib\net45
COPY ..\src\ServiceStack.OrmLite.Sqlite\bin\%BUILD%\netstandard2.0\ServiceStack.OrmLite.Sqlite.* ..\..\ServiceStack\lib\netstandard2.0
COPY ..\src\ServiceStack.OrmLite.Sqlite.Windows\bin\%BUILD%\net45\ServiceStack.OrmLite.Sqlite.Windows.* ..\..\ServiceStack\lib\net45
COPY ..\src\ServiceStack.OrmLite.SqlServer\bin\%BUILD%\net45\ServiceStack.OrmLite.SqlServer.* ..\..\ServiceStack\lib\net45
COPY ..\src\ServiceStack.OrmLite.SqlServer\bin\%BUILD%\netstandard2.0\ServiceStack.OrmLite.SqlServer.* ..\..\ServiceStack\lib\netstandard2.0
COPY ..\src\ServiceStack.OrmLite.MySql\bin\%BUILD%\net45\ServiceStack.OrmLite.MySql.* ..\..\ServiceStack\lib\net45
COPY ..\src\ServiceStack.OrmLite.MySql\bin\%BUILD%\netstandard2.0\ServiceStack.OrmLite.MySql.* ..\..\ServiceStack\lib\netstandard2.0
COPY ..\src\ServiceStack.OrmLite.PostgreSQL\bin\%BUILD%\net45\ServiceStack.OrmLite.PostgreSQL.* ..\..\ServiceStack\lib\net45
COPY ..\src\ServiceStack.OrmLite.PostgreSQL\bin\%BUILD%\netstandard2.0\ServiceStack.OrmLite.PostgreSQL.* ..\..\ServiceStack\lib\netstandard2.0
COPY ..\src\ServiceStack.OrmLite.PostgreSQL\bin\%BUILD%\net45\Npgsql.* ..\..\ServiceStack\lib\net45

COPY ..\src\ServiceStack.OrmLite\bin\Signed\net45\ServiceStack.OrmLite.* ..\..\ServiceStack\lib\signed
COPY ..\src\ServiceStack.OrmLite.SqlServer\bin\Signed\net45\ServiceStack.OrmLite.SqlServer.* ..\..\ServiceStack\lib\signed
