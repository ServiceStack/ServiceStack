REM SET BUILD=Debug
SET BUILD=Release

COPY "..\src\ServiceStack.Interfaces\bin\%BUILD%\portable40-net40+sl5+win8+wp8+wpa81\ServiceStack.Interfaces.*" ..\lib\pcl
COPY ..\src\ServiceStack.Interfaces\bin\%BUILD%\netstandard1.1\ServiceStack.Interfaces.* ..\lib\netstandard1.1
COPY ..\src\ServiceStack.Common\bin\%BUILD%\net45\ServiceStack.Common.* ..\lib\net45
COPY ..\src\ServiceStack.Common\bin\%BUILD%\netstandard1.3\ServiceStack.Common.* ..\lib\netstandard1.3
COPY ..\src\ServiceStack.Common\bin\Signed\net45\ServiceStack.Common.* ..\lib\signed
COPY ..\src\ServiceStack.Client\bin\%BUILD%\net45\ServiceStack.Client.* ..\lib\net45
COPY ..\src\ServiceStack.Client\bin\%BUILD%\netstandard1.1\ServiceStack.Client.* ..\lib\netstandard1.1
COPY ..\src\ServiceStack.Client\bin\%BUILD%\netstandard1.6\ServiceStack.Client.* ..\lib\netstandard1.6
COPY ..\src\ServiceStack.Common\bin\Signed\net45\ServiceStack.Client.* ..\lib\signed
COPY "..\src\ServiceStack.Client\bin\%BUILD%\portable45-net45+win8\ServiceStack.Client.*" ..\lib\pcl
COPY ..\src\ServiceStack\bin\%BUILD%\net45\ServiceStack.dll ..\lib\net45
COPY ..\src\ServiceStack\bin\%BUILD%\netstandard1.6\ServiceStack.dll ..\lib\netstandard1.6

COPY ..\src\ServiceStack\bin\%BUILD%\net45\ServiceStack.* ..\..\ServiceStack.Text\lib\net45
COPY ..\src\ServiceStack\bin\%BUILD%\netstandard1.6\ServiceStack.* ..\..\ServiceStack.Text\lib\netstandard1.6

COPY ..\lib\pcl\ServiceStack.Interfaces.dll ..\..\ServiceStack.Text\lib\pcl
COPY ..\lib\netstandard1.1\ServiceStack.Interfaces.dll ..\..\ServiceStack.Text\lib\netstandard1.1
COPY ..\lib\net45\ServiceStack.Common.dll ..\..\ServiceStack.Text\lib\net45
COPY ..\lib\netstandard1.3\ServiceStack.Common.dll ..\..\ServiceStack.Text\lib\netstandard1.3
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\ServiceStack.Text\lib\net45
COPY ..\lib\netstandard1.1\ServiceStack.Client.dll ..\..\ServiceStack.Text\lib\netstandard1.1
COPY ..\lib\netstandard1.6\ServiceStack.Client.dll ..\..\ServiceStack.Text\lib\netstandard1.6

COPY ..\lib\pcl\ServiceStack.Interfaces.dll ..\..\ServiceStack.Redis\lib\pcl
COPY ..\lib\netstandard1.1\ServiceStack.Interfaces.dll ..\..\ServiceStack.Redis\lib\netstandard1.1
COPY ..\lib\net45\ServiceStack.Common.dll ..\..\ServiceStack.Redis\lib\net45
COPY ..\lib\netstandard1.3\ServiceStack.Common.dll ..\..\ServiceStack.Redis\lib\netstandard1.3
COPY ..\lib\signed\ServiceStack.Common.dll ..\..\ServiceStack.Redis\lib\signed
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\ServiceStack.Redis\lib\net45
COPY ..\lib\netstandard1.1\ServiceStack.Client.dll ..\..\ServiceStack.Redis\lib\netstandard1.1
COPY ..\lib\netstandard1.6\ServiceStack.Client.dll ..\..\ServiceStack.Redis\lib\netstandard1.6

COPY ..\lib\pcl\ServiceStack.Interfaces.dll ..\..\ServiceStack.OrmLite\lib\pcl
COPY ..\lib\netstandard1.1\ServiceStack.Interfaces.dll ..\..\ServiceStack.OrmLite\lib\netstandard1.1
COPY ..\lib\net45\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib\net45
COPY ..\lib\netstandard1.3\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib\netstandard1.3
COPY ..\lib\signed\ServiceStack.Common.dll ..\..\ServiceStack.OrmLite\lib\signed
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\ServiceStack.OrmLite\lib\net45
COPY ..\lib\netstandard1.1\ServiceStack.Client.dll ..\..\ServiceStack.OrmLite\lib\netstandard1.1
COPY ..\lib\netstandard1.6\ServiceStack.Client.dll ..\..\ServiceStack.OrmLite\lib\netstandard1.6

COPY ..\lib\pcl\ServiceStack.Interfaces.dll ..\..\ServiceStack.Aws\lib\pcl
COPY ..\lib\netstandard1.1\ServiceStack.Interfaces.dll ..\..\ServiceStack.Aws\lib\netstandard1.1
COPY ..\lib\net45\ServiceStack.Common.dll ..\..\ServiceStack.Aws\lib\net45
COPY ..\lib\netstandard1.3\ServiceStack.Common.dll ..\..\ServiceStack.Aws\lib\netstandard1.3
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\ServiceStack.Aws\lib\net45
COPY ..\lib\netstandard1.1\ServiceStack.Client.dll ..\..\ServiceStack.Aws\lib\netstandard1.1
COPY ..\lib\netstandard1.6\ServiceStack.Client.dll ..\..\ServiceStack.Aws\lib\netstandard1.6
COPY ..\lib\net45\ServiceStack.dll ..\..\ServiceStack.Aws\lib\net45
COPY ..\lib\netstandard1.6\ServiceStack.dll ..\..\ServiceStack.Aws\lib\netstandard1.6

COPY ..\lib\pcl\ServiceStack.Interfaces.dll ..\..\Admin\lib\pcl
COPY ..\lib\netstandard1.1\ServiceStack.Interfaces.dll ..\..\Admin\lib\netstandard1.1
COPY ..\lib\net45\ServiceStack.Common.dll ..\..\Admin\lib\net45
COPY ..\lib\netstandard1.3\ServiceStack.Common.dll ..\..\Admin\lib\netstandard1.3
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\Admin\lib\net45
COPY ..\lib\netstandard1.1\ServiceStack.Client.dll ..\..\Admin\lib\netstandard1.1
COPY ..\lib\netstandard1.6\ServiceStack.Client.dll ..\..\Admin\lib\netstandard1.6
COPY ..\lib\net45\ServiceStack.dll ..\..\Admin\lib\net45
COPY ..\lib\netstandard1.6\ServiceStack.dll ..\..\Admin\lib\netstandard1.6

COPY ..\lib\pcl\ServiceStack.Interfaces.dll ..\..\Stripe\lib\pcl
COPY ..\lib\netstandard1.1\ServiceStack.Interfaces.dll ..\..\Stripe\lib\netstandard1.1
COPY ..\lib\net45\ServiceStack.Client.dll ..\..\Stripe\lib\net45
COPY ..\lib\netstandard1.1\ServiceStack.Client.dll ..\..\Stripe\lib\netstandard1.1
COPY ..\lib\netstandard1.6\ServiceStack.Client.dll ..\..\Stripe\lib\netstandard1.6
COPY ..\lib\pcl\ServiceStack.Client.dll ..\..\Stripe\lib\pcl


