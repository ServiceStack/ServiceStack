REM SET BUILD=Debug
SET BUILD=Release

COPY ..\..\ServiceStack\src\ServiceStack.Interfaces\bin\%BUILD%\net45\ServiceStack.Interfaces.* net45
COPY ..\..\ServiceStack\src\ServiceStack.Interfaces\bin\%BUILD%\netstandard2.0\ServiceStack.Interfaces.* netstandard2.0

COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\bin\%BUILD%\net45\ServiceStack.Text.* net45
COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\bin\%BUILD%\netstandard2.0\ServiceStack.Text.* netstandard2.0
COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\bin\Signed\net45\ServiceStack.Text.* signed
COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\bin\%BUILD%\net45\ServiceStack.Text.* net45

COPY ..\..\ServiceStack.Text\src\ServiceStack.Memory\bin\%BUILD%\netcoreapp2.1\ServiceStack.Memory.* netcoreapp2.1
COPY ..\..\ServiceStack.Text\src\ServiceStack.Memory\bin\%BUILD%\netcoreapp2.1\ServiceStack.Text.* netcoreapp2.1

COPY ..\..\ServiceStack\src\ServiceStack.Client\bin\%BUILD%\net45\ServiceStack.Client.* net45
COPY ..\..\ServiceStack\src\ServiceStack.Client\bin\%BUILD%\netstandard2.0\ServiceStack.Client.* netstandard2.0

COPY ..\..\ServiceStack\src\ServiceStack.Common\bin\%BUILD%\net45\ServiceStack.Common.* net45
COPY ..\..\ServiceStack\src\ServiceStack.Common\bin\%BUILD%\netstandard2.0\ServiceStack.Common.* netstandard2.0

COPY ..\..\ServiceStack\src\ServiceStack\bin\%BUILD%\net45\ServiceStack.dll net45
COPY ..\..\ServiceStack\src\ServiceStack\bin\%BUILD%\net45\ServiceStack.xml net45
COPY ..\..\ServiceStack\src\ServiceStack\bin\%BUILD%\netstandard2.0\ServiceStack.dll netstandard2.0
COPY ..\..\ServiceStack\src\ServiceStack\bin\%BUILD%\netstandard2.0\ServiceStack.xml netstandard2.0

COPY ..\..\ServiceStack\src\ServiceStack.Server\bin\%BUILD%\net45\ServiceStack.Server.* net45
COPY ..\..\ServiceStack\src\ServiceStack.Server\bin\%BUILD%\netstandard2.0\ServiceStack.Server.* netstandard2.0

COPY ..\..\ServiceStack.Redis\src\ServiceStack.Redis\bin\%BUILD%\net45\ServiceStack.Redis.* net45
COPY ..\..\ServiceStack.Redis\src\ServiceStack.Redis\bin\%BUILD%\netstandard2.0\ServiceStack.Redis.* netstandard2.0

COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite\bin\%BUILD%\net45\ServiceStack.OrmLite.* net45
COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite\bin\%BUILD%\netstandard2.0\ServiceStack.OrmLite.* netstandard2.0

COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.Sqlite\bin\%BUILD%\net45\ServiceStack.OrmLite.Sqlite.* net45
COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.Sqlite\bin\%BUILD%\netstandard2.0\ServiceStack.OrmLite.Sqlite.* netstandard2.0

COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.SqlServer\bin\%BUILD%\net45\ServiceStack.OrmLite.SqlServer.* net45
COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.SqlServer\bin\%BUILD%\netstandard2.0\ServiceStack.OrmLite.SqlServer.* netstandard2.0

COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.PostgreSQL\bin\%BUILD%\net45\ServiceStack.OrmLite.PostgreSQL.* net45
COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.PostgreSQL\bin\%BUILD%\netstandard2.0\ServiceStack.OrmLite.PostgreSQL.* netstandard2.0

COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.MySql\bin\%BUILD%\net45\ServiceStack.OrmLite.MySql.* net45
COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.MySql\bin\%BUILD%\netstandard2.0\ServiceStack.OrmLite.MySql.* netstandard2.0

COPY ..\..\ServiceStack.Aws\src\ServiceStack.Aws\bin\%BUILD%\net45\ServiceStack.Aws.* net45
COPY ..\..\ServiceStack.Aws\src\ServiceStack.Aws\bin\%BUILD%\netstandard2.0\ServiceStack.Aws.* netstandard2.0

COPY ..\..\Admin\src\ServiceStack.Admin\bin\%BUILD%\net45\ServiceStack.Admin.* net45
COPY ..\..\Admin\src\ServiceStack.Admin\bin\%BUILD%\netstandard2.0\ServiceStack.Admin.* netstandard2.0


