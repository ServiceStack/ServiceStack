SET PROJ_PATH= .\..\..\..\Common\ServiceStack.Common
SET BUILD_LIB_PATH=%PROJ_PATH%\Build\Lib

COPY %PROJ_PATH%\ServiceStack.Pattern.Design\bin\Debug\*.dll %BUILD_LIB_PATH%
COPY %PROJ_PATH%\ServiceStack.Common.Services\bin\Debug\*.dll %BUILD_LIB_PATH%
COPY %PROJ_PATH%\ServiceStack.Common.Wcf\bin\Debug\*.dll %BUILD_LIB_PATH%
COPY %PROJ_PATH%\ServiceStack.DataAccess.NHibernateProvider\bin\Debug\*.dll %BUILD_LIB_PATH%
