SET ILMERGE=..\Lib\ilmerge.exe

COPY ..\..\ServiceStack.Interfaces\Build\ServiceStack.Interfaces.dll .

SET PROJ_LIBS=
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Common\bin\Debug\ServiceStack.Common.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceClient.Web\bin\Debug\ServiceStack.ServiceClient.Web.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.CacheAccess.Memcached\bin\Debug\ServiceStack.CacheAccess.Memcached.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.DataAccess.NHibernateProvider\bin\Debug\ServiceStack.DataAccess.NHibernateProvider.dll

REM    Include Adapters and 3rd party dll's
SET PROJ_LIBS=%PROJ_LIBS% ..\Lib\ServiceStack.Logging.Log4Net.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\Lib\Enyim.Caching.dll

REM SET PROJ_LIBS=%PROJ_LIBS% ..\Lib\log4net.dll

%ILMERGE% /ndebug /t:library /out:ServiceStack.Providers.dll %PROJ_LIBS%
