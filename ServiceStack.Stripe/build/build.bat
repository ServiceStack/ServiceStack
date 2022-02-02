SET MSBUILD="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"

%MSBUILD% build.proj /property:Configuration=Release;MinorVersion=4;PatchVersion=1
