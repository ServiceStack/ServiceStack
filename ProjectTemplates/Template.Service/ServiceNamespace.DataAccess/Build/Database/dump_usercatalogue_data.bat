@echo off

FOR /F "tokens=1-4 DELIMS=/ " %%F IN ('date /T') DO (set v_date=%%F%%G%%H)
FOR /F "tokens=1-4 DELIMS=: " %%F IN ('time /T') DO (set v_time=%%F%%G%%H)

set dbname=@ServiceName@
set dumpapp="C:\Program Files\MySQL\MySQL Server 5.0\bin\mysqldump.exe"

set fname=.\database_data.sql

echo Dumping %dbname% to %fname%

echo. > %fname%
echo SET FOREIGN_KEY_CHECKS = 0; >> %fname%
echo. >> %fname%

%dumpapp% --no-create-db --no-create-info -u root -proot %dbname% >> %fname% 

echo. >> %fname%
echo SET FOREIGN_KEY_CHECKS = 1; >> %fname%