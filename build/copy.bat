SET MSBUILD=C:\Windows\Microsoft.NET\Framework\v3.5\MSBuild.exe

REM SET BUILD=Debug
SET BUILD=Release

REM %MSBUILD% build.msbuild

MD ..\NuGet\ServiceStack\lib\net45
MD ..\NuGet\ServiceStack.Api.Swagger\lib\net45
MD ..\NuGet\ServiceStack.Common\lib\net45
MD ..\NuGet\ServiceStack.Mvc\lib\net45
MD ..\NuGet\ServiceStack.Razor\lib\net45
MD ..\NuGet\ServiceStack.Authentication.OpenId\lib\net45
MD ..\NuGet\ServiceStack.Authentication.OAuth2\lib\net45
MD ..\NuGet\ServiceStack.ProtoBuf\lib\net45
MD ..\NuGet\ServiceStack.MsgPack\lib\net45

COPY ..\src\ServiceStack.Razor\bin\%BUILD%\ServiceStack.Razor.* ..\NuGet\ServiceStack.Razor\lib\net45

COPY ..\src\ServiceStack.Mvc\bin\%BUILD%\ServiceStack.Mvc.* ..\NuGet\ServiceStack.Mvc\lib\net45
COPY ..\src\ServiceStack.Mvc\bin\%BUILD%\ServiceStack.Mvc.* ..\NuGet\ServiceStack.Mvc\lib\net45

COPY ..\src\ServiceStack.Authentication.OpenId\bin\%BUILD%\ServiceStack.Authentication.OpenId.* ..\NuGet\ServiceStack.Authentication.OpenId\lib\net45

COPY ..\src\ServiceStack.Authentication.OAuth2\bin\%BUILD%\ServiceStack.Authentication.OAuth2.* ..\NuGet\ServiceStack.Authentication.OAuth2\lib\net45

COPY ..\src\ServiceStack.ProtoBuf\bin\%BUILD%\ServiceStack.ProtoBuf.* ..\NuGet\ServiceStack.ProtoBuf\lib\net45

COPY ..\lib\MsgPack.dll ..\NuGet\ServiceStack.MsgPack\lib\net45
COPY ..\src\ServiceStack.MsgPack\bin\%BUILD%\ServiceStack.MsgPack.* ..\NuGet\ServiceStack.MsgPack\lib\net45

IF EXIST ..\..\swagger-ui\dist-disable (    
    RMDIR ..\NuGet\ServiceStack.Api.Swagger\content\swagger-ui /s /q
    MD ..\NuGet\ServiceStack.Api.Swagger\content\swagger-ui

    RMDIR ..\src\ServiceStack.Api.Swagger\swagger-ui /s /q
    MD ..\src\ServiceStack.Api.Swagger\swagger-ui

    XCOPY /E ..\..\swagger-ui\dist ..\src\ServiceStack.Api.Swagger\swagger-ui
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

COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.dll ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.pdb ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Text.dll ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Text.pdb ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Common.pdb ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Client.dll ..\..\ServiceStack.OrmLite\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Client.pdb ..\..\ServiceStack.OrmLite\lib
COPY ..\tests\ServiceStack.Common.Tests\bin\%BUILD%\ServiceStack.Common.Tests.* ..\..\ServiceStack.OrmLite\lib\tests

COPY ..\src\ServiceStack.Interfaces\bin\Pcl\ServiceStack.Interfaces.* ..\lib
COPY ..\src\ServiceStack.Common\bin\%BUILD%\ServiceStack.Common.* ..\lib
COPY ..\src\ServiceStack.Client\bin\%BUILD%\ServiceStack.Client.* ..\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.dll ..\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.pdb ..\lib

COPY ..\lib\ServiceStack.Interfaces.dll ..\..\ServiceStack.Text\lib
COPY ..\lib\ServiceStack.Interfaces.dll ..\..\ServiceStack.Redis\lib
COPY ..\lib\ServiceStack.Interfaces.dll ..\..\ServiceStack.OrmLite\lib

COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Client.dll ..\..\Stripe\lib
COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.Client.pdb ..\..\Stripe\lib
COPY ..\lib\ServiceStack.Interfaces.dll ..\..\Stripe\lib

COPY ..\src\ServiceStack.Razor.BuildTask\bin\%BUILD%\ServiceStack.Razor.BuildTask.dll ..\lib

REM COPY ..\src\ServiceStack.DtoGen\*.cs  ..\src\ServiceStack\DtoGen
