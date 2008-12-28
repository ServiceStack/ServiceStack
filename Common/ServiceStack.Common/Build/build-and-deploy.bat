for /f "delims=" %%i in ('cd') do set cwd=%%i

call "build.bat"
cd %cwd%
call "deploy.bat"

PAUSE
