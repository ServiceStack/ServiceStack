@echo off
CALL ..\env-vars.bat

%NANT_UTIL% -D:template.properties=template.db4o.properties
