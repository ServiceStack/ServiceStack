CALL ..\..\..\env-vars.bat

REM SET BUILD=Debug
SET BUILD=Release

SET PROJ_LIBS=
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Common\bin\%BUILD%\ServiceStack.Common.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceModel\bin\%BUILD%\ServiceStack.ServiceModel.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Configuration\bin\%BUILD%\ServiceStack.Configuration.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Validation\bin\%BUILD%\ServiceStack.Validation.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Messaging\bin\%BUILD%\ServiceStack.Messaging.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.WebHost.Endpoints\bin\%BUILD%\ServiceStack.WebHost.Endpoints.dll

SET PROJ_LIBS=%PROJ_LIBS% ..\Funq\bin\%BUILD%\Funq.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceHost\bin\%BUILD%\ServiceStack.ServiceHost.dll

SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceClient.Web\bin\%BUILD%\ServiceStack.ServiceClient.Web.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.CacheAccess.Providers\bin\%BUILD%\ServiceStack.CacheAccess.Providers.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Compression\bin\%BUILD%\ServiceStack.Compression.dll

REM External ServiceStack components
SET PROJ_LIBS=%PROJ_LIBS% ..\..\..\release\lib\ServiceStack.OrmLite.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\..\..\release\lib\ServiceStack.OrmLite.Sqlite.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\..\..\release\lib\ServiceStack.OrmLite.SqlServer.dll

SET PROJ_LIBS=%PROJ_LIBS% ..\..\..\release\lib\ServiceStack.Redis.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\..\..\release\lib\ServiceStack.Text.dll

REM SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceInterface\bin\%BUILD%\ServiceStack.ServiceInterface.dll


%ILMERGE_UTIL% /ndebug /t:library /out:ServiceStack.dll %PROJ_LIBS%


REM COPY ..\ServiceStack.DataAccess.Db4oProvider\bin\%BUILD%\ServiceStack.DataAccess.Db4oProvider.dll .
REM COPY ..\ServiceStack.SpringFactory\bin\%BUILD%\ServiceStack.SpringFactory.dll .
REM COPY ..\Lib\ServiceStack.Logging.Log4Net.dll .

REM Deploy the memcached server as well
COPY ..\ServiceStack.CacheAccess.Memcached\bin\%BUILD%\ServiceStack.CacheAccess.Memcached.dll .
COPY ..\ServiceStack.ServiceInterface\bin\%BUILD%\ServiceStack.ServiceInterface.dll .

COPY *.dll ..\Lib
COPY *.dll ..\..\..\release\latest

COPY ServiceStack.dll ..\..\..\ServiceStack.Examples\Lib
COPY ..\..\..\release\lib\ServiceStack.Interfaces.dll ..\..\..\ServiceStack.Examples\Lib
COPY ServiceStack.ServiceInterface.dll ..\..\..\ServiceStack.Examples\Lib

COPY ServiceStack.dll ..\..\..\MonoTouch.Examples\RemoteInfo\Server\Lib
COPY ..\..\..\release\lib\ServiceStack.Interfaces.dll ..\..\..\MonoTouch.Examples\RemoteInfo\Server\Lib

COPY ServiceStack.dll ..\..\ServiceStack.Redis\RedisWebServices\Lib
COPY ..\..\..\release\lib\ServiceStack.Interfaces.dll ..\..\ServiceStack.Redis\RedisWebServices\Lib
COPY ServiceStack.ServiceInterface.dll ..\..\ServiceStack.Redis\RedisWebServices\Lib
