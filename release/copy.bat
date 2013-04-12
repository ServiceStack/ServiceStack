MD latest\ServiceStack
MD latest\ServiceStack.OrmLite
MD latest\ServiceStack.Redis

COPY ..\NuGet\ServiceStack\lib\net35\*  latest\ServiceStack
COPY ..\NuGet\ServiceStack\lib\net40\*  latest\ServiceStack
COPY ..\NuGet\ServiceStack.Common\lib\net35\*  latest\ServiceStack
COPY ..\NuGet\ServiceStack.Mvc\lib\net40\*  latest\ServiceStack
COPY ..\NuGet\ServiceStack.Authentication.OpenId\lib\net35\*  latest\ServiceStack
COPY ..\NuGet\ServiceStack.Plugins.ProtoBuf\lib\net35\*  latest\ServiceStack
COPY ..\NuGet\ServiceStack.Plugins.MsgPack\lib\net40\*  latest\ServiceStack
COPY ..\NuGet\ServiceStack.Razor2\lib\net40\*  latest\ServiceStack

COPY ..\..\ServiceStack.Text\NuGet\lib\net35\*  latest\ServiceStack
COPY ..\..\ServiceStack.Redis\NuGet\lib\net35\*  latest\ServiceStack
COPY ..\..\ServiceStack.OrmLite\NuGet\ServiceStack.OrmLite.SqlServer\lib\*  latest\ServiceStack

MD latest\ServiceStack.OrmLite\Firebird
MD latest\ServiceStack.OrmLite\MySql
MD latest\ServiceStack.OrmLite\PostgreSQL
MD latest\ServiceStack.OrmLite\Sqlite32
MD latest\ServiceStack.OrmLite\Sqlite64
MD latest\ServiceStack.OrmLite\SqlServer
MD latest\ServiceStack.OrmLite\Sqlite32.Mono

COPY ..\..\ServiceStack.Text\NuGet\lib\net35\*  latest\ServiceStack.OrmLite
COPY ..\NuGet\ServiceStack.Common\lib\net35\*  latest\ServiceStack.OrmLite
COPY ..\..\ServiceStack.OrmLite\NuGet\ServiceStack.OrmLite.Firebird\lib\*  latest\ServiceStack.OrmLite\Firebird
COPY ..\..\ServiceStack.OrmLite\NuGet\ServiceStack.OrmLite.MySql\lib\*  latest\ServiceStack.OrmLite\MySql
COPY ..\..\ServiceStack.OrmLite\NuGet\ServiceStack.OrmLite.PostgreSQL\lib\*  latest\ServiceStack.OrmLite\PostgreSQL
COPY ..\..\ServiceStack.OrmLite\NuGet\ServiceStack.OrmLite.Sqlite32\lib\*  latest\ServiceStack.OrmLite\Sqlite32
COPY ..\..\ServiceStack.OrmLite\NuGet\ServiceStack.OrmLite.Sqlite64\lib\*  latest\ServiceStack.OrmLite\Sqlite64
COPY ..\..\ServiceStack.OrmLite\NuGet\ServiceStack.OrmLite.SqlServer\lib\*  latest\ServiceStack.OrmLite\SqlServer
COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.Sqlite\bin\Release\ServiceStack.OrmLite.*  latest\ServiceStack.OrmLite\Sqlite32.Mono

