SET MSBUILD=C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe

REM %MSBUILD% build.proj /target:TeamCityBuild;NuGetPack /property:Configuration=Release;RELEASE=true;PatchVersion=0
%MSBUILD% build-sn.proj /target:TeamCityBuild;NuGetPack /property:Configuration=Signed;RELEASE=true;PatchVersion=0
REM %MSBUILD% build-pcl.proj /target:TeamCityBuild;NuGetPack /property:Configuration=Release;RELEASE=true;PatchVersion=0

REM Debug Task
REM C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe /t:rebuild /debug C:\src\ServiceStack\tests\RazorRockstars.BuildTask\RazorRockstars.BuildTask.csproj
