
PUSHD ..\Common\ServiceStack.Common\Build
CALL ilmerge-all.bat
POPD

COPY ..\Common\ServiceStack.Common\Build\*.dll ..\ExampleProjects\Lib 
