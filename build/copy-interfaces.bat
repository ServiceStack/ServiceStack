REM SET BUILD=Debug
SET BUILD=Release

COPY ..\src\ServiceStack.Interfaces\bin\Pcl\ServiceStack.Interfaces.* ..\lib
COPY ..\src\ServiceStack.Common\bin\%BUILD%\ServiceStack.Common.* ..\lib
COPY ..\src\ServiceStack.Client\bin\%BUILD%\ServiceStack.Client.* ..\lib

COPY ..\lib\ServiceStack.Interfaces.dll ..\..\ServiceStack.Text\lib
COPY ..\lib\ServiceStack.Interfaces.dll ..\..\ServiceStack.Text\lib\tests
COPY ..\lib\ServiceStack.Common.dll ..\..\ServiceStack.Text\lib
COPY ..\lib\ServiceStack.Client.dll ..\..\ServiceStack.Text\lib

COPY ..\lib\ServiceStack.Interfaces.dll ..\..\ServiceStack.Redis\lib
COPY ..\lib\ServiceStack.Common.dll ..\..\ServiceStack.Redis\lib
COPY ..\lib\ServiceStack.Client.dll ..\..\ServiceStack.Redis\lib

COPY ..\lib\ServiceStack.Interfaces.dll ..\..\ServiceStack.OrmLite\lib
COPY ..\lib\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib
COPY ..\lib\ServiceStack.Client.dll ..\..\ServiceStack.OrmLite\lib

COPY ..\lib\ServiceStack.Interfaces.dll ..\..\ServiceStack.Aws\lib
COPY ..\lib\ServiceStack.Common.dll ..\..\ServiceStack.Aws\lib
COPY ..\lib\ServiceStack.Client.dll ..\..\ServiceStack.Aws\lib

