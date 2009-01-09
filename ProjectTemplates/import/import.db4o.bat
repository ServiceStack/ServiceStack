@echo off
CALL ..\..\env-vars.bat

%NANT_UTIL% -D:import.properties=import.db4o.properties
