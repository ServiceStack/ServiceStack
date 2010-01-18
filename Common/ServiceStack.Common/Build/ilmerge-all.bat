CALL ..\..\..\env-vars.bat

PUSHD ..\..\ServiceStack.Interfaces\Build
CALL ilmerge-all.bat 
POPD

REM SET BUILD=Debug
SET BUILD=Release

COPY ..\..\ServiceStack.Interfaces\Build\ServiceStack.Interfaces.dll .
REM COPY ..\ServiceStack.Translators.Generator\bin\%BUILD%\ServiceStack.Translators.Generator.exe .

SET PROJ_LIBS=
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Common\bin\%BUILD%\ServiceStack.Common.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceModel\bin\%BUILD%\ServiceStack.ServiceModel.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceInterface\bin\%BUILD%\ServiceStack.ServiceInterface.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Configuration\bin\%BUILD%\ServiceStack.Configuration.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Validation\bin\%BUILD%\ServiceStack.Validation.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.WebHost.Endpoints\bin\%BUILD%\ServiceStack.WebHost.Endpoints.dll

SET PROJ_LIBS=%PROJ_LIBS% ..\Funq\bin\%BUILD%\Funq.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceHost\bin\%BUILD%\ServiceStack.ServiceHost.dll

SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceClient.Web\bin\%BUILD%\ServiceStack.ServiceClient.Web.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.CacheAccess.Providers\bin\%BUILD%\ServiceStack.CacheAccess.Providers.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Compression\bin\%BUILD%\ServiceStack.Compression.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.OrmLite\bin\%BUILD%\ServiceStack.OrmLite.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.OrmLite.Sqlite\bin\%BUILD%\ServiceStack.OrmLite.Sqlite.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.OrmLite.SqlServer\bin\%BUILD%\ServiceStack.OrmLite.SqlServer.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Redis\bin\%BUILD%\ServiceStack.Redis.dll

REM SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.DataAccess.NHibernateProvider\bin\%BUILD%\ServiceStack.DataAccess.NHibernateProvider.dll

%ILMERGE_UTIL% /ndebug /t:library /out:ServiceStack.dll %PROJ_LIBS%

%ILMERGE_UTIL% /ndebug /t:library /out:ServiceStack.Memcached.dll ..\Lib\Enyim.Caching.dll ..\ServiceStack.CacheAccess.Memcached\bin\%BUILD%\ServiceStack.CacheAccess.Memcached.dll
COPY ..\ServiceStack.DataAccess.Db4oProvider\bin\%BUILD%\ServiceStack.DataAccess.Db4oProvider.dll .
COPY ..\ServiceStack.SpringFactory\bin\%BUILD%\ServiceStack.SpringFactory.dll .
COPY ..\Lib\ServiceStack.Logging.Log4Net.dll .


COPY *.dll %SERVICESTACK_DEPLOY_PATH%
COPY *.exe %SERVICESTACK_DEPLOY_PATH%

COPY *.dll ..\Lib
COPY *.dll ..\..\..\release\latest

COPY ServiceStack.dll ..\..\..\ServiceStack.Examples\Lib
COPY ServiceStack.Interfaces.dll ..\..\..\ServiceStack.Examples\Lib
COPY ServiceStack.dll ..\..\..\MonoTouch.Examples\RemoteInfo\Server\Lib
COPY ServiceStack.Interfaces.dll ..\..\..\MonoTouch.Examples\RemoteInfo\Server\Lib
