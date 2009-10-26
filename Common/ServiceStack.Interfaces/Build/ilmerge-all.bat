CALL ..\..\..\env-vars.bat

SET PROJ_LIBS=
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.CacheAccess\bin\Debug\ServiceStack.CacheAccess.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Configuration\bin\Debug\ServiceStack.Configuration.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.DataAccess\bin\Debug\ServiceStack.DataAccess.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.DesignPatterns\bin\Debug\ServiceStack.DesignPatterns.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Logging\bin\Debug\ServiceStack.Logging.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.LogicFacade\bin\Debug\ServiceStack.LogicFacade.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceHost\bin\Debug\ServiceStack.ServiceHost.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.SearchIndex\bin\Debug\ServiceStack.SearchIndex.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Service\bin\Debug\ServiceStack.Service.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Translators\bin\Debug\ServiceStack.Translators.dll

%ILMERGE_UTIL% /ndebug /t:library /out:ServiceStack.Interfaces.dll %PROJ_LIBS%
