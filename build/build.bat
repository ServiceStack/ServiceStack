SET MSBUILD=C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe

%MSBUILD% build.proj /target:TeamCityBuild;NuGetPack /property:Configuration=Release;RELEASE=true;PatchVersion=60