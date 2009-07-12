SET SS_INTERFACES=C:\Projects\code.google.com\servicestack\Common\ServiceStack.Interfaces
SET SS_MODEL=C:\Projects\code.google.com\servicestack\Common\ServiceStack.ServiceModel

COPY %SS_MODEL%\Version100\*.cs ..\ServiceModel\Version100\

COPY %SS_INTERFACES%\ServiceStack.Logging\*.cs ..\Logging\
COPY %SS_INTERFACES%\ServiceStack.Logging\Support\Logging\*.cs ..\Logging\Support\

COPY %SS_INTERFACES%\ServiceStack.DesignPatterns\Serialization\*.cs ..\Serialization\

COPY %SS_INTERFACES%\ServiceStack.Service\*.cs ..\Service\
