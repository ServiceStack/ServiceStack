SET BUILD=Signed

COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\bin\%BUILD%\net45\ServiceStack.Text.dll ..\lib\signed
COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\bin\%BUILD%\net45\ServiceStack.Text.dll ..\..\ServiceStack.Redis\lib\signed
COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\bin\%BUILD%\net45\ServiceStack.Text.dll ..\..\ServiceStack.OrmLite\lib\signed
COPY ..\..\ServiceStack.Text\src\ServiceStack.Text\bin\%BUILD%\net45\ServiceStack.Text.dll ..\..\ServiceStack.Aws\lib\signed

COPY ..\src\ServiceStack.Common\bin\%BUILD%\net45\ServiceStack.Common.dll ..\lib\signed
COPY ..\src\ServiceStack.Common\bin\%BUILD%\net45\ServiceStack.Common.dll ..\..\ServiceStack.Redis\lib\signed
COPY ..\src\ServiceStack.Common\bin\%BUILD%\net45\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib\signed
COPY ..\src\ServiceStack.Common\bin\%BUILD%\net45\ServiceStack.Common.dll ..\..\ServiceStack.Aws\lib\signed

COPY ..\src\ServiceStack.Client\bin\%BUILD%\net45\ServiceStack.Client.dll ..\lib\signed

COPY ..\src\ServiceStack\bin\%BUILD%\net45\ServiceStack.dll ..\lib\signed

COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite\bin\%BUILD%\net45\ServiceStack.OrmLite.dll ..\lib\signed
COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite\bin\%BUILD%\net45\ServiceStack.OrmLite.dll ..\..\ServiceStack.OrmLite\lib\signed

COPY ..\..\ServiceStack.Redis\src\ServiceStack.Redis\bin\%BUILD%\net45\ServiceStack.Redis.dll ..\lib\signed
COPY ..\..\ServiceStack.OrmLite\src\ServiceStack.OrmLite.SqlServer\bin\%BUILD%\net45\ServiceStack.OrmLite.SqlServer.dll ..\lib\signed

