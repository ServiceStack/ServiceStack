@echo off

SET SED="..\Lib\gnutools\sed.exe"

%SED% -i "s/object/byte[]/g" DataModel\Customer.generated.cs
%SED% -i "s/object/byte[]/g" DataModel\CustomerOrder.generated.cs

REM Revert the unused generated files and directories from subversion if they have been added

svn revert -q .\Base\ManagerBase.cs
svn revert -q .\Base\NHibernateSession.cs
svn revert -q .\Base\NHibernateSessionManager.cs
svn revert -q .\Base\UnitTestsBase.cs
svn revert -q .\ManagerObjects
svn revert -q .\UnitTests

REM Remove the unused generated files and directories from the DataAccess project

REM remove the files from the VS2008 project

xpathrun -remove-elements "//doc:Compile[starts-with(@Include, 'Base\ManagerBase.cs') or starts-with(@Include, 'Base\NHibernateSession.cs') or starts-with(@Include, 'Base\NHibernateSessionManager.cs') or starts-with(@Include, 'Base\UnitTestsBase.cs') or starts-with(@Include, 'ManagerObjects\') or starts-with(@Include, 'UnitTests\')]" ServiceStack.Sakila.DataAccess.csproj

REM Delete unused generated files and directories

del /F /Q .\Base\ManagerBase.cs
del /F /Q .\Base\NHibernateSession.cs
del /F /Q .\Base\NHibernateSessionManager.cs
del /F /Q .\Base\UnitTestsBase.cs
rd /S /Q .\ManagerObjects
rd /S /Q .\UnitTests
