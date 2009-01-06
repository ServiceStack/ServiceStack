
PUSHD ..\..\Sakila.ServiceModel\bin\Debug\

REM Usage: ServiceStack.Translators.Generator.exe [/f Overwrite existing files]  [/assembly:Full Path to Assembly] [/out: Directory for generated classes]

ServiceStack.Translators.Generator.exe /f /assembly:%cd%\Sakila.ServiceModel.dll /out:%cd%\..\..\Version100\Types

POPD