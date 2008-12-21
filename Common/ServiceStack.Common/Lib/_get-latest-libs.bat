COPY .\..\..\ServiceStack.Logging\ServiceStack.Logging.Log4Net\bin\Debug\*.dll .\
COPY .\..\..\ServiceStack.Messaging\ServiceStack.Messaging\bin\Debug\*.dll .\

MOVE /Y .\log4net.1.2.10.dll .\log4net.dll