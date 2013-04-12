#!/bin/bash

MODO=Debug
LIB_DIR=lib
MONO_DIR=lib
#MODO=Release

CURRENT_DIR=`pwd`
cd ../..

function makeMonoDir {
    echo "$1/$MONO_DIR"
	if [ ! -d "$1/$MONO_DIR" ]
	then
		mkdir -p "$1/$MONO_DIR"/tests
	fi
}  

if [ ! -d "$LIB_DIR" ]
then
	mkdir "$LIB_DIR"
fi

function buildComponentAndCopyToServiceStack {
	xbuild /p:Configuration="$MODO" "$1"/src/"$2"/"$2".csproj
	cp "$1"/src/"$2"/bin/"$MODO"/"$2".dll "$LIB_DIR"
	cp "$LIB_DIR"/"$2".dll ServiceStack/"$MONO_DIR"
}

function buildComponent {
	xbuild /p:Configuration="$MODO" "$1"/src/"$2"/"$2".csproj
	cp "$1"/src/"$2"/bin/"$MODO"/"$2".dll "$LIB_DIR"
}

function buildTestComponent {
	xbuild /p:Configuration="$MODO" "$1"/tests/"$2"/"$2".csproj
	cp "$1"/tests/"$2"/bin/"$MODO"/"$2".dll "$LIB_DIR"
}

function buildServiceStackBenchmarks {
	xbuild /p:Configuration="$MODO" "$1"/src/"$2"/"$3"/"$3".csproj
	cp "$1"/src/"$2"/"$3"/bin/"$MODO"/"$3".dll "$LIB_DIR"
}

makeMonoDir ServiceStack
makeMonoDir ServiceStack.OrmLite
makeMonoDir ServiceStack.Redis
makeMonoDir ServiceStack.Text


buildComponentAndCopyToServiceStack ServiceStack.Text ServiceStack.Text
cp "$LIB_DIR"/ServiceStack.Text.dll ServiceStack.OrmLite/"$MONO_DIR"
cp "$LIB_DIR"/ServiceStack.Text.dll ServiceStack.Redis/"$MONO_DIR"
cp "$LIB_DIR"/ServiceStack.Text.dll ServiceStack.Redis/"$MONO_DIR"/tests
cp "$LIB_DIR"/ServiceStack.Text.dll ServiceStack.Text/"$MONO_DIR"/tests
cp "$LIB_DIR"/ServiceStack.Text.dll ServiceStack.Logging/"$MONO_DIR"

buildComponentAndCopyToServiceStack ServiceStack ServiceStack.Interfaces
cp "$LIB_DIR"/ServiceStack.Interfaces.dll ServiceStack.OrmLite/"$MONO_DIR"
cp "$LIB_DIR"/ServiceStack.Interfaces.dll ServiceStack.Redis/"$MONO_DIR"
cp "$LIB_DIR"/ServiceStack.Interfaces.dll ServiceStack.Redis/"$MONO_DIR"/tests
cp "$LIB_DIR"/ServiceStack.Interfaces.dll ServiceStack.Text/"$MONO_DIR"/tests
cp "$LIB_DIR"/ServiceStack.Interfaces.dll ServiceStack.Logging/"$MONO_DIR"

buildComponentAndCopyToServiceStack ServiceStack ServiceStack.Common
cp "$LIB_DIR"/ServiceStack.Common.dll ServiceStack.OrmLite/"$MONO_DIR"
cp "$LIB_DIR"/ServiceStack.Common.dll ServiceStack.Redis/"$MONO_DIR"
cp "$LIB_DIR"/ServiceStack.Common.dll ServiceStack.Redis/"$MONO_DIR"/tests
cp "$LIB_DIR"/ServiceStack.Common.dll ServiceStack.Text/"$MONO_DIR"/tests/
cp "$LIB_DIR"/ServiceStack.Common.dll ServiceStack.Logging/"$MONO_DIR"

buildComponent ServiceStack ServiceStack
cp "$LIB_DIR"/ServiceStack.dll ServiceStack.Redis/"$MONO_DIR"/tests
cp "$LIB_DIR"/ServiceStack.dll ServiceStack.Text/"$MONO_DIR"/tests

buildComponentAndCopyToServiceStack ServiceStack.OrmLite ServiceStack.OrmLite
buildComponentAndCopyToServiceStack ServiceStack.OrmLite ServiceStack.OrmLite.Sqlite
buildComponentAndCopyToServiceStack ServiceStack.OrmLite ServiceStack.OrmLite.SqlServer
buildComponent ServiceStack.OrmLite ServiceStack.OrmLite.MySql
buildComponent ServiceStack.OrmLite ServiceStack.OrmLite.PostgreSQL
buildComponent ServiceStack.OrmLite ServiceStack.OrmLite.Firebird
buildComponent ServiceStack.OrmLite ServiceStack.OrmLite.Oracle

cp "$LIB_DIR"/ServiceStack.OrmLite.dll ServiceStack.Text/"$MONO_DIR"/tests
cp "$LIB_DIR"/ServiceStack.OrmLite.SqlServer.dll ServiceStack.Text/"$MONO_DIR"/tests
cp "$LIB_DIR"/ServiceStack.OrmLite.Sqlite.dll ServiceStack.Text/"$MONO_DIR"/tests

#ServiceStack again 
buildComponent ServiceStack ServiceStack.Authentication.OpenId
buildComponent ServiceStack ServiceStack.Plugins.MsgPack
buildComponent ServiceStack ServiceStack.ServiceInterface
buildComponent ServiceStack ServiceStack.Plugins.ProtoBuf
buildComponent ServiceStack ServiceStack.FluentValidation.Mvc3
buildComponent ServiceStack ServiceStack.Razor2
cp "$LIB_DIR"/ServiceStack.Razor2.dll ServiceStack.Redis/"$MONO_DIR"/tests/
cp "$LIB_DIR"/ServiceStack.ServiceInterface.dll ServiceStack.Redis/"$MONO_DIR"/tests/
cp "$LIB_DIR"/ServiceStack.ServiceInterface.dll ServiceStack.Text/"$MONO_DIR"/tests/


buildComponentAndCopyToServiceStack ServiceStack.Redis ServiceStack.Redis
cp  "$LIB_DIR"/ServiceStack.Redis.dll ServiceStack.Redis/"$LIB_DIR"/tests
cp  "$LIB_DIR"/ServiceStack.Redis.dll ServiceStack.Text/"$LIB_DIR"/tests

#ServiceStack.Benchmarks
cp "$LIB_DIR"/ServiceStack.dll ServiceStack.Benchmarks/lib
cp "$LIB_DIR"/ServiceStack.Interfaces.dll ServiceStack.Benchmarks/lib

cp "$LIB_DIR"/ServiceStack.Text.dll ServiceStack.Benchmarks/src/Northwind.Benchmarks/Lib
cp "$LIB_DIR"/ServiceStack.Interfaces.dll ServiceStack.Benchmarks/src/Northwind.Benchmarks/Lib

buildServiceStackBenchmarks ServiceStack.Benchmarks Northwind.Benchmarks Northwind.Common
buildServiceStackBenchmarks ServiceStack.Benchmarks Northwind.Benchmarks Northwind.Perf
buildServiceStackBenchmarks ServiceStack.Benchmarks Northwind.Benchmarks Northwind.Benchmarks
buildServiceStackBenchmarks ServiceStack.Benchmarks Northwind.Benchmarks Northwind.Benchmarks.Console

cp "$LIB_DIR"/Northwind.Common.dll ServiceStack/"$MONO_DIR"/tests/
cp "$LIB_DIR"/Northwind.Common.dll ServiceStack.OrmLite/"$MONO_DIR"/tests/
cp "$LIB_DIR"/Northwind.Common.dll ServiceStack.Redis/"$MONO_DIR"/tests/
cp "$LIB_DIR"/Northwind.Common.dll ServiceStack.Text/"$MONO_DIR"/tests/
cp "$LIB_DIR"/Northwind.Perf.dll ServiceStack.OrmLite/"$MONO_DIR"/tests/


buildTestComponent ServiceStack ServiceStack.Common.Tests
cp  "$LIB_DIR"/ServiceStack.Common.Tests.dll ServiceStack.OrmLite/"$MONO_DIR"/tests
cp  "$LIB_DIR"/ServiceStack.Common.Tests.dll ServiceStack.Redis/"$MONO_DIR"/tests
cp  "$LIB_DIR"/ServiceStack.Common.Tests.dll ServiceStack.Text/"$MONO_DIR"/tests
buildTestComponent ServiceStack ServiceStack.Messaging.Tests
cp  "$LIB_DIR"/ServiceStack.Messaging.Tests.dll ServiceStack.Redis/"$MONO_DIR"/tests
cp  "$LIB_DIR"/ServiceStack.Messaging.Tests.dll ServiceStack.Text/"$MONO_DIR"/tests


#ServiceStack.RazorHostTests : xbuild can not  build it, but monodevelop does it!

#xbuild  ServiceStack/tests/ServiceStack.RazorHostTests/ServiceStack.RazorHostTests.csproj

#Imported project: 
#"/usr/local/lib/mono/xbuild/Microsoft/VisualStudio/v10.0/WebApplications/Microsoft.WebApplication.targets" does not exist.
# there is v9.0 

#ServiceStack.RazorNancyTests : xbuild can not  build it, but monodevelop does it!
#xbuild  ServiceStack/tests/ServiceStack.RazorNancyTests/ServiceStack.RazorNancyTests.csproj

buildTestComponent ServiceStack ServiceStack.ServiceHost.Tests
buildTestComponent ServiceStack ServiceStack.ServiceModel.Tests
#ServiceStack.WebHost.Endpoints.Tests:  warning as error
buildTestComponent ServiceStack ServiceStack.WebHost.Endpoints.Tests
#ServiceStack.WebHost.IntegrationTests
# execute once :
#sudo ln -s /usr/local/lib/mono/xbuild/Microsoft/VisualStudio/v9.0 /usr/local/lib/mono/xbuild/Microsoft/VisualStudio/v10.0
buildTestComponent ServiceStack ServiceStack.WebHost.IntegrationTests
cp  ServiceStack/tests/ServiceStack.WebHost.IntegrationTests/bin/ServiceStack.WebHost.IntegrationTests.dll "$LIB_DIR"


buildTestComponent ServiceStack.Redis ServiceStack.Redis.Tests
buildTestComponent ServiceStack.Text ServiceStack.Text.Tests
#ServiceStack.Text.Tests  // comment public void #Can_Serialize_User_OAuthSession_list()  and public void #Doesnt_serialize_TypeInfo_when_set()


#ServiceStack.Logging.EventLog

#fail: ../../src//.nuget/nuget.targets: Project file could not be imported
#use monodevelop
#xbuild  ServiceStack.Logging/src/ServiceStack.Logging.EventLog/ServiceStack.Logging.EventLog.csproj

#ServiceStack.Logging.Log4Net
#fail: ../../src//.nuget/nuget.targets: Project file could not be imported
#use monodevelop
#xbuild  #ServiceStack.Logging/src/ServiceStack.Logging.Log4Net/ServiceStack.Logging.Log4Net.csproj

cd "$CURRENT_DIR"
