CALL ..\..\..\..\env-vars.bat

REM SET BUILD=Debug
SET BUILD=Release

COPY ..\bin\%BUILD%\*.dll ..\..\..\..\release\latest\
