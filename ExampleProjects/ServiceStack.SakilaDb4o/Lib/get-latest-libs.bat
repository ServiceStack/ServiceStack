PUSHD ..\..\..\Common\ServiceStack.Common\Build
CALL ilmerge-all.bat 

POPD
COPY ..\..\..\Common\ServiceStack.Common\Build\*.dll ..\..\Lib
COPY ..\..\..\Common\ServiceStack.Common\Build\*.exe ..\..\Sakila.ServiceModel\bin\Debug\
