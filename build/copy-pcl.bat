SET MSBUILD=C:\Windows\Microsoft.NET\Framework\v3.5\MSBuild.exe

REM SET BUILD=Debug
SET BUILD=Release

COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\bin\Pcl\ServiceStack.Text.* ..\lib\pcl
COPY ..\src\ServiceStack.Client\bin\Pcl\ServiceStack.Client.* ..\lib\pcl

COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\PclExport.Net40.cs ..\src\ServiceStack.Pcl.Android
COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\PclExport.Net40.cs ..\src\ServiceStack.Pcl.Ios
COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\PclExport.Net40.cs ..\src\ServiceStack.Pcl.Net45
COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\PclExport.WinStore.cs ..\src\ServiceStack.Pcl.WinStore

COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\Pcl.* ..\src\ServiceStack.Pcl.Android
COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\Pcl.* ..\src\ServiceStack.Pcl.Ios
COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\Pcl.* ..\src\ServiceStack.Pcl.Net45
COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\Pcl.* ..\src\ServiceStack.Pcl.WinStore

