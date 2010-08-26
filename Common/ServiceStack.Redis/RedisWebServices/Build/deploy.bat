SET HOST_PATH=..\RedisWebServices.Host
SET DEPLOY_PATH=release\RedisAdminUI

IF EXIST "%DEPLOY_PATH%" RMDIR /S /Q %DEPLOY_PATH%
IF EXIST "%DEPLOY_PATH%.zip" DEL /S /Q "%DEPLOY_PATH%.zip"

MD "%DEPLOY_PATH%"

XCOPY "%HOST_PATH%\*.aspx" "%DEPLOY_PATH%\" /Y
XCOPY "%HOST_PATH%\*.asax" "%DEPLOY_PATH%\" /Y
XCOPY "%HOST_PATH%\*.config" "%DEPLOY_PATH%\" /Y
XCOPY "%HOST_PATH%\bin" "%DEPLOY_PATH%\bin\" /Y /S 
XCOPY "%HOST_PATH%\AjaxClient" "%DEPLOY_PATH%\AjaxClient\" /Y /S 

for /f %%D in ('dir/s/b/ad %DEPLOY_PATH% ^| find/i ".svn" ') do if exist "%%D" rd/s/q "%%D"
for /f %%D in ('dir/s/b/ad %DEPLOY_PATH% ^| find/i ".idea" ') do if exist "%%D" rd/s/q "%%D"

PUSHD release
..\7z.exe a -tzip RedisAdminUI.zip "RedisAdminUI"
POPD