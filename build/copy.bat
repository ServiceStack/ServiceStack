SET MSBUILD=C:\Windows\Microsoft.NET\Framework\v3.5\MSBuild.exe

REM SET BUILD=Debug
SET BUILD=Release

REM %MSBUILD% build.msbuild

MD ..\NuGet\ServiceStack\lib\net40
MD ..\NuGet\ServiceStack.Api.Swagger\lib\net40
MD ..\NuGet\ServiceStack.Common\lib\net40
MD ..\NuGet\ServiceStack.Mvc\lib\net40
MD ..\NuGet\ServiceStack.Razor\lib\net40
MD ..\NuGet\ServiceStack.Authentication.OpenId\lib\net40
MD ..\NuGet\ServiceStack.Authentication.OAuth2\lib\net40
MD ..\NuGet\ServiceStack.ProtoBuf\lib\net40
MD ..\NuGet\ServiceStack.MsgPack\lib\net40

COPY ..\src\ServiceStack.Razor\bin\%BUILD%\ServiceStack.Razor.* ..\NuGet\ServiceStack.Razor\lib\net40

COPY ..\src\ServiceStack.Mvc\bin\%BUILD%\ServiceStack.Mvc.* ..\NuGet\ServiceStack.Mvc\lib\net40
COPY ..\src\ServiceStack.Mvc\bin\%BUILD%\ServiceStack.Mvc.* ..\NuGet\ServiceStack.Mvc\lib\net40

COPY ..\src\ServiceStack.Authentication.OpenId\bin\%BUILD%\ServiceStack.Authentication.OpenId.* ..\NuGet\ServiceStack.Authentication.OpenId\lib\net40

COPY ..\src\ServiceStack.Authentication.OAuth2\bin\%BUILD%\ServiceStack.Authentication.OAuth2.* ..\NuGet\ServiceStack.Authentication.OAuth2\lib\net40

COPY ..\src\ServiceStack.ProtoBuf\bin\%BUILD%\ServiceStack.ProtoBuf.* ..\NuGet\ServiceStack.ProtoBuf\lib\net40

COPY ..\lib\MsgPack.dll ..\NuGet\ServiceStack.MsgPack\lib\net40
COPY ..\src\ServiceStack.MsgPack\bin\%BUILD%\ServiceStack.MsgPack.* ..\NuGet\ServiceStack.MsgPack\lib\net40

IF EXIST ..\..\swagger-ui\dist (    
    RMDIR ..\tests\ServiceStack.WebHost.IntegrationTests\swagger-ui /s /q
    MD ..\tests\ServiceStack.WebHost.IntegrationTests\swagger-ui

    RMDIR ..\NuGet\ServiceStack.Api.Swagger\content\swagger-ui /s /q
    MD ..\NuGet\ServiceStack.Api.Swagger\content\swagger-ui

    RMDIR ..\src\ServiceStack.Api.Swagger\swagger-ui /s /q
    MD ..\src\ServiceStack.Api.Swagger\swagger-ui

    XCOPY /E ..\..\swagger-ui\dist ..\tests\ServiceStack.WebHost.IntegrationTests\swagger-ui
    XCOPY /E ..\..\swagger-ui\dist ..\src\ServiceStack.Api.Swagger\swagger-ui
    XCOPY /E ..\..\swagger-ui\dist ..\NuGet\ServiceStack.Api.Swagger\content\swagger-ui
)

COPY ..\src\ServiceStack.Api.Swagger\bin\%BUILD%\ServiceStack.Api.Swagger.* ..\NuGet\ServiceStack.Api.Swagger\lib\net35

COPY ..\src\ServiceStack\bin\%BUILD%\*.* ..\..\ServiceStack.Contrib\lib
COPY ..\src\ServiceStack\bin\%BUILD%\*.* ..\..\ServiceStack.RedisWebServices\lib
COPY ..\src\ServiceStack.Server\bin\%BUILD%\ServiceStack.Server.* ..\..\ServiceStack.RedisWebServices\lib

COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Text.* ..\..\ServiceStack.Redis\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Common.* ..\..\ServiceStack.Redis\lib
COPY ..\src\ServiceStack.Server\bin\%BUILD%\ServiceStack.* ..\..\ServiceStack.Redis\lib\tests
COPY ..\tests\ServiceStack.Messaging.Tests\bin\%BUILD%\ServiceStack.Messaging.Tests.* ..\..\ServiceStack.Redis\lib\tests

COPY ..\tests\ServiceStack.Common.Tests\bin\%BUILD%\ServiceStack.Common.Tests.* ..\..\ServiceStack.Text\lib\tests
COPY ..\tests\ServiceStack.Common.Tests\bin\%BUILD%\ServiceStack.Common.Tests.* ..\..\ServiceStack.Redis\lib\tests
COPY ..\tests\ServiceStack.Common.Tests\bin\%BUILD%\ServiceStack.Common.Tests.* ..\..\ServiceStack.OrmLite\lib\tests
COPY ..\src\ServiceStack.Server\bin\%BUILD%\ServiceStack.* ..\..\ServiceStack.Text\lib\tests
COPY ..\src\ServiceStack.Server\bin\%BUILD%\ServiceStack.* ..\..\ServiceStack.Rediss\lib\tests
COPY ..\src\ServiceStack.Server\bin\%BUILD%\ServiceStack.* ..\..\ServiceStack.OrmLite\lib\tests

COPY ..\src\ServiceStack.Interfaces\bin\%BUILD%\ServiceStack.Interfaces.dll ..\..\ServiceStack.Benchmarks\src\Northwind.Benchmarks\Lib

COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Text.dll ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Text.pdb ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Common.pdb ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Client.dll ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Client.pdb ..\..\ServiceStack.OrmLite\lib
COPY ..\tests\ServiceStack.Common.Tests\bin\%BUILD%\ServiceStack.Common.Tests.* ..\..\ServiceStack.OrmLite\lib\tests

COPY ..\src\ServiceStack.Interfaces\bin\Release\ServiceStack.Interfaces.dll ..\lib
COPY ..\src\ServiceStack.Interfaces\bin\Release\ServiceStack.Interfaces.dll ..\..\ServiceStack.Text\lib
COPY ..\src\ServiceStack.Interfaces\bin\Release\ServiceStack.Interfaces.dll ..\..\ServiceStack.Redis\lib
COPY ..\src\ServiceStack.Interfaces\bin\Release\ServiceStack.Interfaces.dll ..\..\ServiceStack.OrmLite\lib

COPY ..\src\ServiceStack.Interfaces\bin\Pcl\ServiceStack.Interfaces.* ..\lib\pcl

