REM SET BUILD=Debug
SET BUILD=Release

COPY ..\src\ServiceStack.Interfaces\bin\%BUILD%\net45\ServiceStack.Interfaces.* ..\lib\net45
COPY ..\src\ServiceStack.Interfaces\bin\%BUILD%\netstandard2.0\ServiceStack.Interfaces.* ..\lib\netstandard2.0
COPY ..\src\ServiceStack.Common\bin\%BUILD%\net45\ServiceStack.Common.* ..\lib\net45
COPY ..\src\ServiceStack.Common\bin\%BUILD%\netstandard2.0\ServiceStack.Common.* ..\lib\netstandard2.0
COPY ..\src\ServiceStack.Client\bin\%BUILD%\net45\ServiceStack.Client.* ..\lib\net45
COPY ..\src\ServiceStack.Client\bin\%BUILD%\netstandard2.0\ServiceStack.Client.* ..\lib\netstandard2.0
COPY ..\src\ServiceStack\bin\%BUILD%\net45\ServiceStack.dll ..\lib\net45
COPY ..\src\ServiceStack\bin\%BUILD%\netstandard2.0\ServiceStack.dll ..\lib\netstandard2.0

COPY ..\src\ServiceStack\bin\%BUILD%\net45\ServiceStack.* ..\..\ServiceStack.Text\lib\net45
COPY ..\src\ServiceStack\bin\%BUILD%\netstandard2.0\ServiceStack.* ..\..\ServiceStack.Text\lib\netstandard2.0

COPY ..\lib\net45\ServiceStack.Interfaces.dll ..\..\ServiceStack.Text\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Interfaces.dll ..\..\ServiceStack.Text\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Common.dll ..\..\ServiceStack.Text\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Common.dll ..\..\ServiceStack.Text\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\ServiceStack.Text\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Text\lib\netstandard2.0
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Text\lib\netstandard2.0

COPY ..\lib\net45\ServiceStack.Interfaces.dll ..\..\ServiceStack.Redis\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Interfaces.dll ..\..\ServiceStack.Redis\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Common.dll ..\..\ServiceStack.Redis\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Common.dll ..\..\ServiceStack.Redis\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\ServiceStack.Redis\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Redis\lib\netstandard2.0
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Redis\lib\netstandard2.0

COPY ..\lib\net45\ServiceStack.Interfaces.dll ..\..\ServiceStack.OrmLite\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Interfaces.dll ..\..\ServiceStack.OrmLite\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\ServiceStack.OrmLite\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.OrmLite\lib\netstandard2.0
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.OrmLite\lib\netstandard2.0

COPY ..\lib\net45\ServiceStack.Interfaces.dll ..\..\ServiceStack.Aws\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Interfaces.dll ..\..\ServiceStack.Aws\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Common.dll ..\..\ServiceStack.Aws\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Common.dll ..\..\ServiceStack.Aws\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\ServiceStack.Aws\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Aws\lib\netstandard2.0
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\ServiceStack.Aws\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.dll ..\..\ServiceStack.Aws\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.dll ..\..\ServiceStack.Aws\lib\netstandard2.0

COPY ..\lib\net45\ServiceStack.Interfaces.dll ..\..\Admin\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Interfaces.dll ..\..\Admin\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Common.dll ..\..\Admin\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Common.dll ..\..\Admin\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\Admin\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\Admin\lib\netstandard2.0
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\Admin\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.dll ..\..\Admin\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.dll ..\..\Admin\lib\netstandard2.0

COPY ..\lib\net45\ServiceStack.Interfaces.dll ..\..\Stripe\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Interfaces.dll ..\..\Stripe\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\Stripe\lib\net45
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\Stripe\lib\netstandard2.0
COPY ..\lib\netstandard2.0\ServiceStack.Client.dll ..\..\Stripe\lib\netstandard2.0
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\Stripe\lib\net45


