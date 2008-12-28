SET ILMERGE=..\Lib\ilmerge.exe

SET PROJ_LIBS=
SET PROJ_LIBS=%PROJ_LIBS% C:\Projects\code.google\Common\ServiceStack.Interfaces\ServiceStack.CacheAccess\bin\Debug\ServiceStack.CacheAccess.dll
SET PROJ_LIBS=%PROJ_LIBS% C:\Projects\code.google\Common\ServiceStack.Interfaces\ServiceStack.Configuration\bin\Debug\ServiceStack.Configuration.dll
SET PROJ_LIBS=%PROJ_LIBS% C:\Projects\code.google\Common\ServiceStack.Interfaces\ServiceStack.DataAccess\bin\Debug\ServiceStack.DataAccess.dll
SET PROJ_LIBS=%PROJ_LIBS% C:\Projects\code.google\Common\ServiceStack.Interfaces\ServiceStack.DesignPatterns\bin\Debug\ServiceStack.DesignPatterns.dll
SET PROJ_LIBS=%PROJ_LIBS% C:\Projects\code.google\Common\ServiceStack.Interfaces\ServiceStack.Logging\bin\Debug\ServiceStack.Logging.dll
SET PROJ_LIBS=%PROJ_LIBS% C:\Projects\code.google\Common\ServiceStack.Interfaces\ServiceStack.ServiceClient\bin\Debug\ServiceStack.ServiceClient.dll
SET PROJ_LIBS=%PROJ_LIBS% C:\Projects\code.google\Common\ServiceStack.Interfaces\ServiceStack.LogicFacade\bin\Debug\ServiceStack.LogicFacade.dll

%ILMERGE% /ndebug /t:library /out:ServiceStack.Interfaces.dll %PROJ_LIBS%
