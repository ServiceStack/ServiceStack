for /f "usebackq tokens=*" %%i in (`vswhere.exe -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
  SET MSBUILD="%%i"
)
%MSBUILD% build.proj /property:Configuration=Release;MinorVersion=8;PatchVersion=1

