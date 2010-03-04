CALL ..\..\..\env-vars.bat

REM SET BUILD=Debug
SET BUILD=Release

SET PROJ_LIBS=
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.CacheAccess\bin\%BUILD%\ServiceStack.CacheAccess.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Configuration\bin\%BUILD%\ServiceStack.Configuration.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.DataAccess\bin\%BUILD%\ServiceStack.DataAccess.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.DataAnnotations\bin\%BUILD%\ServiceStack.DataAnnotations.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.DesignPatterns\bin\%BUILD%\ServiceStack.DesignPatterns.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Logging\bin\%BUILD%\ServiceStack.Logging.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.LogicFacade\bin\%BUILD%\ServiceStack.LogicFacade.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.ServiceHost\bin\%BUILD%\ServiceStack.ServiceHost.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.SearchIndex\bin\%BUILD%\ServiceStack.SearchIndex.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Service\bin\%BUILD%\ServiceStack.Service.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Translators\bin\%BUILD%\ServiceStack.Translators.dll
SET PROJ_LIBS=%PROJ_LIBS% ..\ServiceStack.Messaging\bin\%BUILD%\ServiceStack.Messaging.dll

%ILMERGE_UTIL% /ndebug /t:library /out:ServiceStack.Interfaces.dll %PROJ_LIBS%
