REM SET BUILD=Debug
SET BUILD=Release

COPY "..\src\ServiceStack.Interfaces\bin\%BUILD%\portable40-net45+sl5+win8+wp8+wpa81\ServiceStack.Interfaces.*" ..\lib
COPY ..\src\ServiceStack.Interfaces\bin\%BUILD%\netstandard1.1\ServiceStack.Interfaces.* ..\lib\netstandard1.1
COPY ..\src\ServiceStack.Common\bin\%BUILD%\net45\ServiceStack.Common.* ..\lib
COPY ..\src\ServiceStack.Common\bin\%BUILD%\netstandard1.3\ServiceStack.Common.* ..\lib\netstandard1.3
COPY ..\src\ServiceStack.Common\bin\Signed\net45\ServiceStack.Common.* ..\lib\signed
COPY ..\src\ServiceStack.Client\bin\%BUILD%\net45\ServiceStack.Client.* ..\lib
COPY ..\src\ServiceStack.Client\bin\%BUILD%\netstandard1.1\ServiceStack.Client.* ..\lib\netstandard1.1
COPY ..\src\ServiceStack.Client\bin\%BUILD%\netstandard1.6\ServiceStack.Client.* ..\lib\netstandard1.6
COPY ..\src\ServiceStack.Common\bin\Signed\net45\ServiceStack.Client.* ..\lib\signed
COPY "..\src\ServiceStack.Client\bin\%BUILD%\portable45-net45+win8\ServiceStack.Client.*" ..\lib\pcl
COPY ..\src\ServiceStack\bin\%BUILD%\net45\ServiceStack.dll ..\lib
COPY ..\src\ServiceStack\bin\%BUILD%\netstandard1.6\ServiceStack.dll ..\lib\netstandard1.6

COPY ..\src\ServiceStack\bin\%BUILD%\net45\ServiceStack.* ..\..\ServiceStack.Text\lib\tests
COPY ..\src\ServiceStack\bin\%BUILD%\netstandard1.6\ServiceStack.* ..\..\ServiceStack.Text\lib\tests\netstandard1.6

COPY ..\lib\ServiceStack.Interfaces.dll ..\..\ServiceStack.Text\lib
COPY ..\lib\ServiceStack.Interfaces.dll ..\..\ServiceStack.Text\lib\tests
COPY ..\lib\netstandard1.1\ServiceStack.Interfaces.dll ..\..\ServiceStack.Text\lib\tests\netstandard1.1
COPY ..\lib\ServiceStack.Common.dll ..\..\ServiceStack.Text\lib
COPY ..\lib\netstandard1.3\ServiceStack.Common.dll ..\..\ServiceStack.Text\lib\tests\netstandard1.3
COPY ..\lib\ServiceStack.Client.dll ..\..\ServiceStack.Text\lib
COPY ..\lib\netstandard1.1\ServiceStack.Client.dll ..\..\ServiceStack.Text\lib\tests\netstandard1.1
COPY ..\lib\netstandard1.6\ServiceStack.Client.dll ..\..\ServiceStack.Text\lib\tests\netstandard1.6

COPY ..\lib\ServiceStack.Interfaces.dll ..\..\ServiceStack.Redis\lib
COPY ..\lib\netstandard1.1\ServiceStack.Interfaces.dll ..\..\ServiceStack.Redis\lib\netstandard1.1
COPY ..\lib\ServiceStack.Common.dll ..\..\ServiceStack.Redis\lib
COPY ..\lib\netstandard1.3\ServiceStack.Common.dll ..\..\ServiceStack.Redis\lib\netstandard1.3
COPY ..\lib\signed\ServiceStack.Common.dll ..\..\ServiceStack.Redis\lib\signed
COPY ..\lib\ServiceStack.Client.dll ..\..\ServiceStack.Redis\lib
COPY ..\lib\netstandard1.1\ServiceStack.Client.dll ..\..\ServiceStack.Redis\lib\netstandard1.1
COPY ..\lib\netstandard1.6\ServiceStack.Client.dll ..\..\ServiceStack.Redis\lib\netstandard1.6

COPY ..\lib\ServiceStack.Interfaces.dll ..\..\ServiceStack.OrmLite\lib
COPY ..\lib\netstandard1.1\ServiceStack.Interfaces.dll ..\..\ServiceStack.OrmLite\lib\netstandard1.1
COPY ..\lib\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib
COPY ..\lib\netstandard1.3\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib\netstandard1.3
COPY ..\lib\signed\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib\signed
COPY ..\lib\ServiceStack.Client.dll ..\..\ServiceStack.OrmLite\lib
COPY ..\lib\netstandard1.1\ServiceStack.Client.dll ..\..\ServiceStack.OrmLite\lib\netstandard1.1
COPY ..\lib\netstandard1.6\ServiceStack.Client.dll ..\..\ServiceStack.OrmLite\lib\netstandard1.6

COPY ..\lib\ServiceStack.Interfaces.dll ..\..\ServiceStack.Aws\lib
COPY ..\lib\netstandard1.1\ServiceStack.Interfaces.dll ..\..\ServiceStack.Aws\lib\netstandard1.1
COPY ..\lib\ServiceStack.Common.dll ..\..\ServiceStack.Aws\lib
COPY ..\lib\netstandard1.3\ServiceStack.Common.dll ..\..\ServiceStack.Aws\lib\netstandard1.3
COPY ..\lib\ServiceStack.Client.dll ..\..\ServiceStack.Aws\lib
COPY ..\lib\netstandard1.1\ServiceStack.Client.dll ..\..\ServiceStack.Aws\lib\netstandard1.1
COPY ..\lib\netstandard1.6\ServiceStack.Client.dll ..\..\ServiceStack.Aws\lib\netstandard1.6
COPY ..\lib\ServiceStack.dll ..\..\ServiceStack.Aws\lib
COPY ..\lib\netstandard1.6\ServiceStack.dll ..\..\ServiceStack.Aws\lib\netstandard1.6

COPY ..\lib\ServiceStack.Interfaces.dll ..\..\Admin\lib
COPY ..\lib\netstandard1.1\ServiceStack.Interfaces.dll ..\..\Admin\lib\netstandard1.1
COPY ..\lib\ServiceStack.Common.dll ..\..\Admin\lib
COPY ..\lib\netstandard1.3\ServiceStack.Common.dll ..\..\Admin\lib\netstandard1.3
COPY ..\lib\ServiceStack.Client.dll ..\..\Admin\lib
COPY ..\lib\netstandard1.1\ServiceStack.Client.dll ..\..\Admin\lib\netstandard1.1
COPY ..\lib\netstandard1.6\ServiceStack.Client.dll ..\..\Admin\lib\netstandard1.6
COPY ..\lib\ServiceStack.dll ..\..\Admin\lib
COPY ..\lib\netstandard1.6\ServiceStack.dll ..\..\Admin\lib\netstandard1.6

COPY ..\lib\ServiceStack.Interfaces.dll ..\..\Stripe\lib
COPY ..\lib\netstandard1.1\ServiceStack.Interfaces.dll ..\..\Stripe\lib\netstandard1.1
COPY ..\lib\ServiceStack.Client.dll ..\..\Stripe\lib
COPY ..\lib\netstandard1.1\ServiceStack.Client.dll ..\..\Stripe\lib\netstandard1.1
COPY ..\lib\netstandard1.6\ServiceStack.Client.dll ..\..\Stripe\lib\netstandard1.6
COPY ..\lib\pcl\ServiceStack.Client.dll ..\..\Stripe\lib\pcl


