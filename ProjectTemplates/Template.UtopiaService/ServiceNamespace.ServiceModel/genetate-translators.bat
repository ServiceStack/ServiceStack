PUSHD bin\Debug

REM Usage: ServiceStack.Translators.Generator.exe [/f Overwrite existing files]  [/assembly:Full Path to Assembly] [/out: Directory for generated classes]

COPY ..\..\..\Lib\ServiceStack.* .
ServiceStack.Translators.Generator.exe /f /assembly:C:\Projects\PoToPe\trunk\webservices\src\Prototypes\@ServiceNamespace@\@ServiceModelNamespace@\bin\Debug\@ServiceModelNamespace@.dll /out:C:\Projects\PoToPe\trunk\webservices\src\Prototypes\@ServiceNamespace@\@ServiceModelNamespace@\Version100\Types

POPD