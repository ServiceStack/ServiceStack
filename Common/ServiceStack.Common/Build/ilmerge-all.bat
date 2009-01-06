CALL ..\..\..\env-vars.bat

PUSHD ..\..\ServiceStack.Interfaces\Build
CALL ilmerge-all.bat 
POPD

COPY ..\..\ServiceStack.Interfaces\Build\ServiceStack.Interfaces.dll .
COPY ..\ServiceStack.Translators.Generator\bin\Debug\ServiceStack.Translators.Generator.exe .

SET PROJ_LIBS=
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Common\bin\Debug\ServiceStack.Common.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceModel\bin\Debug\ServiceStack.ServiceModel.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceInterface\bin\Debug\ServiceStack.ServiceInterface.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Configuration\bin\Debug\ServiceStack.Configuration.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Validation\bin\Debug\ServiceStack.Validation.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.WebHost.Endpoints\bin\Debug\ServiceStack.WebHost.Endpoints.dll

SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceClient.Web\bin\Debug\ServiceStack.ServiceClient.Web.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.CacheAccess.Providers\bin\Debug\ServiceStack.CacheAccess.Providers.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.DataAccess.NHibernateProvider\bin\Debug\ServiceStack.DataAccess.NHibernateProvider.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.DataAccess.Db4oProvider\bin\Debug\ServiceStack.DataAccess.Db4oProvider.dll

REM Include Adapters to popular 3rd party dll's
SET PROJ_LIBS=%PROJ_LIBS% ..\Lib\ServiceStack.Logging.Log4Net.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\Lib\Enyim.Caching.dll

%ILMERGE_UTIL% /ndebug /t:library /out:ServiceStack.dll %PROJ_LIBS%
