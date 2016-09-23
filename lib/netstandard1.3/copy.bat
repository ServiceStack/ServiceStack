SET BUILD=Debug
REM SET BUILD=Release

COPY ..\..\..\ServiceStack.Text\src\ServiceStack.Text\bin\%BUILD%\netstandard1.3\ServiceStack.Text.* .\
COPY ..\..\..\ServiceStack.Redis\src\ServiceStack.Redis\bin\%BUILD%\netstandard1.3\ServiceStack.Redis.* .\
COPY ..\..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.Sqlite\bin\%BUILD%\netstandard1.3\ServiceStack.OrmLite.Sqlite.* .\
COPY ..\..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.SqlServer\bin\%BUILD%\netstandard1.3\ServiceStack.OrmLite.SqlServer.* .\
COPY ..\..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite\bin\%BUILD%\netstandard1.3\ServiceStack.OrmLite.* .\

