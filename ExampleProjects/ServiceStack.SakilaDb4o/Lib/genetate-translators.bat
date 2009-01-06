
PUSHD ..\..\Sakila.ServiceModel\bin\Debug\

ServiceStack.Translators.Generator.exe /f /assembly:%cd%\Sakila.ServiceModel.dll /out:%cd%\..\..\Version100\Types

POPD