SET MSBUILD=C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe

REM %MSBUILD% build.proj /target:TeamCityBuild;NuGetPack /property:Configuration=Release;RELEASE=true;PatchVersion=0
%MSBUILD% build-sn.proj /target:TeamCityBuild;NuGetPack /property:Configuration=Signed;RELEASE=true;PatchVersion=0
REM %MSBUILD% build-pcl.proj /target:TeamCityBuild;NuGetPack /property:Configuration=Release;RELEASE=true;PatchVersion=0
