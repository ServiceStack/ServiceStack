SET ILMERGE=..\Lib\ilmerge.exe

COPY ..\Lib\ServiceStack.Interfaces.dll .

SET PROJ_LIBS=
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Common\bin\Debug\ServiceStack.Common.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceModel\bin\Debug\ServiceStack.ServiceModel.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceInterface\bin\Debug\ServiceStack.ServiceInterface.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Configuration\bin\Debug\ServiceStack.Configuration.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Validation\bin\Debug\ServiceStack.Validation.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.WebHost.Endpoints\bin\Debug\ServiceStack.WebHost.Endpoints.dll

%ILMERGE% /ndebug /t:library /out:ServiceStack.Common.Core.dll %PROJ_LIBS%

SET PROJ_LIBS=
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Common\bin\Debug\ServiceStack.Common.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceClient.Web\bin\Debug\ServiceStack.ServiceClient.Web.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.CacheAccess.Memcached\bin\Debug\ServiceStack.CacheAccess.Memcached.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.DataAccess.NHibernateProvider\bin\Debug\ServiceStack.DataAccess.NHibernateProvider.dll

%ILMERGE% /ndebug /t:library /out:ServiceStack.Providers.dll %PROJ_LIBS%
