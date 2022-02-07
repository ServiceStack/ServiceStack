for /f "usebackq tokens=*" %%i in (`..\..\build\vswhere.exe -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
  SET MSBUILD="%%i"
)
%MSBUILD% build.proj /property:Configuration=Release
