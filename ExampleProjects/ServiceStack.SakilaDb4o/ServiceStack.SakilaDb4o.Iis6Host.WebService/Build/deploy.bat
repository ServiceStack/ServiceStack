REM SET GZIP=C:\Tools\gzip-1.3.12-1-bin\bin\gzip.exe
SET DEPLOY_PATH=out\SakilaDb4o

RMDIR /S /Q %DEPLOY_PATH%
MKDIR %DEPLOY_PATH%
XCOPY ..\Public %DEPLOY_PATH% /E /Y 
MKDIR %DEPLOY_PATH%\bin\
XCOPY ..\bin %DEPLOY_PATH%\bin\ /E /Y 
MKDIR %DEPLOY_PATH%\App_Data\
XCOPY ..\App_Data %DEPLOY_PATH%\App_Data\ /E /Y 
COPY  ..\Web.config %DEPLOY_PATH%
COPY  ..\Global.asax %DEPLOY_PATH%
