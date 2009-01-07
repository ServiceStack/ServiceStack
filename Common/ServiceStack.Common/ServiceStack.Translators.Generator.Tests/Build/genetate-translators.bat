COPY C:\Projects\code.google\Common\ServiceStack.Common\Build\*.dll C:\Projects\PoToPe\trunk\desktop\prototypes\Utopia.UserCatalogue\Lib
COPY C:\Projects\code.google\Common\ServiceStack.Common\Build\*.dll C:\Projects\PoToPe\trunk\desktop\prototypes\Utopia.UserCatalogue\UserCatalogue.ServiceModel\bin\Debug

PUSHD C:\Projects\PoToPe\trunk\desktop\prototypes\Utopia.UserCatalogue\UserCatalogue.ServiceModel\bin\Debug

REM Usage: ServiceStack.Translators.Generator.exe [/f Overwrite existing files]  [/assembly:Full Path to Assembly] [/out: Directory for generated classes]

ServiceStack.Translators.Generator.exe /f /assembly:C:\Projects\PoToPe\trunk\desktop\prototypes\Utopia.UserCatalogue\UserCatalogue.ServiceModel\bin\Debug\UserCatalogue.ServiceModel.dll /out:C:\Projects\PoToPe\trunk\desktop\prototypes\Utopia.UserCatalogue\UserCatalogue.ServiceModel\Version100\Types

POPD