SET BUILD=Signed

COPY ..\src\ServiceStack.Common\bin\%BUILD%\ServiceStack.Common.dll ..\lib\signed
COPY ..\src\ServiceStack.Common\bin\%BUILD%\ServiceStack.Common.dll ..\..\ServiceStack.Redis\lib\signed
COPY ..\src\ServiceStack.Common\bin\%BUILD%\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib\signed

COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\bin\%BUILD%\ServiceStack.Text.dll ..\lib\signed
COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\bin\%BUILD%\ServiceStack.Text.dll ..\..\ServiceStack.Redis\lib\signed
COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\bin\%BUILD%\ServiceStack.Text.dll ..\..\ServiceStack.OrmLite\lib\signed

COPY ..\src\ServiceStack.Client\bin\%BUILD%\ServiceStack.Client.dll ..\lib\signed

COPY ..\src\ServiceStack\bin\%BUILD%\ServiceStack.dll ..\lib\signed

COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite\bin\%BUILD%\ServiceStack.OrmLite.dll ..\lib\signed
COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite\bin\%BUILD%\ServiceStack.OrmLite.dll ..\..\ServiceStack.OrmLite\lib\signed

COPY ..\..\ServiceStack.Redis\src\ServiceStack.Redis\bin\%BUILD%\ServiceStack.Redis.dll ..\lib\signed
COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.SqlServer\bin\%BUILD%\ServiceStack.OrmLite.SqlServer.dll ..\lib\signed

