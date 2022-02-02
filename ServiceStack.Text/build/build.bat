SET MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Preview\MSBuild\Current\Bin\MSBuild.exe"

%MSBUILD% build.proj /p:Configuration=Release /p:MinorVersion=12 /p:PatchVersion=1
