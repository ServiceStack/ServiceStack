@echo off

if "%*"=="" goto noargs

nant\bin\nant.exe %*

goto exit

:noargs
echo you must supply a valid build target. [all, configure, deploy, install]\n

:exit