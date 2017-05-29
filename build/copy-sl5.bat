SET MSBUILD=C:\Windows\Microsoft.NET\Framework\v3.5\MSBuild.exe

REM SET BUILD=Debug
SET BUILD=Release

COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\bin\Sl5\ServiceStack.Text.* ..\lib\sl5
COPY ..\src\ServiceStack.Client\bin\Release\sl5\ServiceStack.Client.* ..\lib\sl5
