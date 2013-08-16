SET MSBUILD=C:\Windows\Microsoft.NET\Framework\v3.5\MSBuild.exe

REM SET BUILD=Debug
SET BUILD=Release

REM %MSBUILD% build.msbuild

MD ..\NuGet\ServiceStack\lib\net35
MD ..\NuGet\ServiceStack.Api.Swagger\lib\net35
MD ..\NuGet\ServiceStack.Common\lib\net35
MD ..\NuGet\ServiceStack.Mvc\lib\net40
MD ..\NuGet\ServiceStack.Razor\lib\net40
MD ..\NuGet\ServiceStack.Authentication.OpenId\lib\net35
MD ..\NuGet\ServiceStack.Plugins.ProtoBuf\lib\net35
MD ..\NuGet\ServiceStack.Plugins.MsgPack\lib\net40

COPY ..\src\ServiceStack.ServiceInterface\bin\%BUILD%\ServiceStack.dll ..\NuGet\ServiceStack\lib\net35
COPY ..\src\ServiceStack.ServiceInterface\bin\%BUILD%\ServiceStack.pdb ..\NuGet\ServiceStack\lib\net35
COPY ..\src\ServiceStack.ServiceInterface\bin\%BUILD%\ServiceStack.xml ..\NuGet\ServiceStack\lib\net35
COPY ..\src\ServiceStack.ServiceInterface\bin\%BUILD%\ServiceStack.ServiceInterface.* ..\NuGet\ServiceStack\lib\net35

REM COPY ..\src\ServiceStack.ServiceInterface\bin\%BUILD%\ServiceStack.dll ..\NuGet\ServiceStack\lib\net40
REM COPY ..\src\ServiceStack.ServiceInterface\bin\%BUILD%\ServiceStack.pdb ..\NuGet\ServiceStack\lib\net40
REM COPY ..\src\ServiceStack.ServiceInterface\bin\%BUILD%\ServiceStack.xml ..\NuGet\ServiceStack\lib\net40
REM COPY ..\src\ServiceStack.ServiceInterface\bin\%BUILD%\ServiceStack.ServiceInterface.* ..\NuGet\ServiceStack\lib\net40

COPY ..\src\ServiceStack.Razor\bin\%BUILD%\ServiceStack.Razor.* ..\NuGet\ServiceStack.Razor\lib\net40

COPY ..\src\ServiceStack.ServiceInterface\bin\%BUILD%\ServiceStack.Common.* ..\NuGet\ServiceStack.Common\lib\net35
COPY ..\src\ServiceStack.ServiceInterface\bin\%BUILD%\ServiceStack.Interfaces.* ..\NuGet\ServiceStack.Common\lib\net35

COPY ..\src\ServiceStack.FluentValidation.Mvc3\bin\%BUILD%\ServiceStack.FluentValidation.Mvc3.* ..\NuGet\ServiceStack.Mvc\lib\net40
COPY ..\src\ServiceStack.FluentValidation.Mvc3\bin\%BUILD%\ServiceStack.FluentValidation.Mvc3.* ..\NuGet\ServiceStack.Mvc\lib\net40

COPY ..\src\ServiceStack.Authentication.OpenId\bin\%BUILD%\ServiceStack.Authentication.OpenId.* ..\NuGet\ServiceStack.Authentication.OpenId\lib\net35

COPY ..\src\ServiceStack.Plugins.ProtoBuf\bin\%BUILD%\ServiceStack.Plugins.ProtoBuf.* ..\NuGet\ServiceStack.Plugins.ProtoBuf\lib\net35

COPY ..\lib\MsgPack.dll ..\NuGet\ServiceStack.Plugins.MsgPack\lib\net40
COPY ..\src\ServiceStack.Plugins.MsgPack\bin\%BUILD%\ServiceStack.Plugins.MsgPack.* ..\NuGet\ServiceStack.Plugins.MsgPack\lib\net40

RMDIR ..\NuGet\ServiceStack.Api.Swagger\content\swagger-ui /s /q
MD ..\NuGet\ServiceStack.Api.Swagger\content\swagger-ui
COPY ..\src\ServiceStack.Api.Swagger\bin\%BUILD%\ServiceStack.Api.Swagger.* ..\NuGet\ServiceStack.Api.Swagger\lib\net35
XCOPY /E ..\src\ServiceStack.Api.Swagger\swagger-ui ..\NuGet\ServiceStack.Api.Swagger\content\swagger-ui

COPY ..\src\ServiceStack.ServiceInterface\bin\%BUILD%\*.* ..\..\chaweet\api\lib

COPY ..\src\ServiceStack.Razor\bin\%BUILD%\*.* ..\..\ServiceStack.Examples\lib
COPY ..\src\ServiceStack.ServiceInterface\bin\%BUILD%\*.* ..\..\ServiceStack.Examples\lib
COPY ..\src\ServiceStack\bin\%BUILD%\*.* ..\..\ServiceStack.Contrib\lib
COPY ..\src\ServiceStack\bin\%BUILD%\*.* ..\..\ServiceStack.RedisWebServices\lib
COPY ..\src\ServiceStack.ServiceInterface\bin\%BUILD%\ServiceStack.ServiceInterface.* ..\..\ServiceStack.RedisWebServices\lib

COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Interfaces.dll ..\..\ServiceStack.Redis\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Text.* ..\..\ServiceStack.Redis\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Common.* ..\..\ServiceStack.Redis\lib
COPY ..\src\ServiceStack.ServiceInterface\bin\%BUILD%\ServiceStack.* ..\..\ServiceStack.Redis\lib\tests
COPY ..\tests\ServiceStack.Common.Tests\bin\%BUILD%\ServiceStack.Common.Tests.* ..\..\ServiceStack.Redis\lib\tests
COPY ..\tests\ServiceStack.Messaging.Tests\bin\%BUILD%\ServiceStack.Messaging.Tests.* ..\..\ServiceStack.Redis\lib\tests

COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Interfaces.dll ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Text.dll ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Text.pdb ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Common.pdb ..\..\ServiceStack.OrmLite\lib
COPY ..\tests\ServiceStack.Common.Tests\bin\%BUILD%\ServiceStack.Common.Tests.* ..\..\ServiceStack.OrmLite\lib\tests
