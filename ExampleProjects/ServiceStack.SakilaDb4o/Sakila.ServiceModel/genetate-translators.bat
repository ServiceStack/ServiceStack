
COPY ..\..\..\Common\ServiceStack.Common\Build\ServiceStack.Interfaces.dll bin\Debug\
COPY ..\..\..\Common\ServiceStack.Common\Build\ServiceStack.Translators.Generator.exe bin\Debug\

PUSHD bin\Debug\

REM Usage: ServiceStack.Translators.Generator.exe [/f Overwrite existing files]  [/assembly:Full Path to Assembly] [/out: Directory for generated classes]

ServiceStack.Translators.Generator.exe /f /assembly:%cd%\Sakila.ServiceModel.dll /out:%cd%\..\..\Version100\Types

POPD