CALL ..\..\..\env-vars.bat

SET PROJ_LIBS=
SET PROJ_LIBS=%PROJ_LIBS% C:\Projects\code.google\Common\ServiceStack.Interfaces\ServiceStack.CacheAccess\bin\Debug\ServiceStack.CacheAccess.dll
SET PROJ_LIBS=%PROJ_LIBS% C:\Projects\code.google\Common\ServiceStack.Interfaces\ServiceStack.Configuration\bin\Debug\ServiceStack.Configuration.dll
SET PROJ_LIBS=%PROJ_LIBS% C:\Projects\code.google\Common\ServiceStack.Interfaces\ServiceStack.DataAccess\bin\Debug\ServiceStack.DataAccess.dll
SET PROJ_LIBS=%PROJ_LIBS% C:\Projects\code.google\Common\ServiceStack.Interfaces\ServiceStack.DesignPatterns\bin\Debug\ServiceStack.DesignPatterns.dll
SET PROJ_LIBS=%PROJ_LIBS% C:\Projects\code.google\Common\ServiceStack.Interfaces\ServiceStack.Logging\bin\Debug\ServiceStack.Logging.dll
SET PROJ_LIBS=%PROJ_LIBS% C:\Projects\code.google\Common\ServiceStack.Interfaces\ServiceStack.LogicFacade\bin\Debug\ServiceStack.LogicFacade.dll
SET PROJ_LIBS=%PROJ_LIBS% C:\Projects\code.google\Common\ServiceStack.Interfaces\ServiceStack.Service\bin\Debug\ServiceStack.Service.dll

%%ILMERGE_UTIL% % /ndebug /t:library /out:ServiceStack.Interfaces.dll %PROJ_LIBS%
