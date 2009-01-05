CALL ..\env-vars.bat

RMDIR /S /Q %DEPLOY_WWW_DEMO_PATH%
MKDIR %DEPLOY_WWW_DEMO_PATH%

PUSHD ..\ExampleProjects\ServiceStack.SakilaDb4o\ServiceStack.SakilaDb4o.Iis6Host.WebService\Build
CALL deploy.bat
POPD

PUSHD ..\ExampleProjects\ServiceStack.SakilaNHibernate\ServiceStack.SakilaNHibernate.Iis6Host.WebService\Build
CALL deploy.bat
POPD

%ZIP_UTIL% SakilaDemo.zip %DEPLOY_WWW_DEMO_PATH%