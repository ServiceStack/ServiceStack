@echo off
CALL ..\env-vars.bat

%NANT_UTIL% -D:template.properties=template.utopia.db4o.properties
