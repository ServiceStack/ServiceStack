CALL ..\..\..\env-vars.bat

PUSHD ..\..\ServiceStack.Interfaces\Build
CALL ilmerge-all.bat 
POPD

COPY ..\..\ServiceStack.Interfaces\Build\ServiceStack.Interfaces.dll .
REM COPY ..\ServiceStack.Translators.Generator\bin\Debug\ServiceStack.Translators.Generator.exe .

SET PROJ_LIBS=
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Common\bin\Debug\ServiceStack.Common.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceModel\bin\Debug\ServiceStack.ServiceModel.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceInterface\bin\Debug\ServiceStack.ServiceInterface.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Configuration\bin\Debug\ServiceStack.Configuration.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Validation\bin\Debug\ServiceStack.Validation.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.WebHost.Endpoints\bin\Debug\ServiceStack.WebHost.Endpoints.dll

SET PROJ_LIBS=%PROJ_LIBS% ..\Funq\bin\Debug\Funq.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceHost\bin\Debug\ServiceStack.ServiceHost.dll

SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceClient.Web\bin\Debug\ServiceStack.ServiceClient.Web.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.CacheAccess.Providers\bin\Debug\ServiceStack.CacheAccess.Providers.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Compression\bin\Debug\ServiceStack.Compression.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.OrmLite\bin\Debug\ServiceStack.OrmLite.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.OrmLite.Sqlite\bin\Debug\ServiceStack.OrmLite.Sqlite.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.OrmLite.SqlServer\bin\Debug\ServiceStack.OrmLite.SqlServer.dll

REM SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.DataAccess.NHibernateProvider\bin\Debug\ServiceStack.DataAccess.NHibernateProvider.dll

%ILMERGE_UTIL% /ndebug /t:library /out:ServiceStack.dll %PROJ_LIBS%

%ILMERGE_UTIL% /ndebug /t:library /out:ServiceStack.Memcached.dll ..\Lib\Enyim.Caching.dll ..\ServiceStack.CacheAccess.Memcached\bin\Debug\ServiceStack.CacheAccess.Memcached.dll
COPY ..\ServiceStack.DataAccess.Db4oProvider\bin\Debug\ServiceStack.DataAccess.Db4oProvider.dll .
COPY ..\Lib\ServiceStack.Logging.Log4Net.dll .

COPY *.dll %SERVICESTACK_DEPLOY_PATH%
COPY *.exe %SERVICESTACK_DEPLOY_PATH%

COPY *.dll ..\Lib
COPY *.dll ..\..\..\release\latest

COPY ServiceStack.dll ..\..\..\ServiceStack.Examples\Lib
COPY ServiceStack.Interfaces.dll ..\..\..\ServiceStack.Examples\Lib
COPY ServiceStack.dll ..\..\..\MonoTouch.Examples\RemoteInfo\Server\Lib
COPY ServiceStack.Interfaces.dll ..\..\..\MonoTouch.Examples\RemoteInfo\Server\Lib
