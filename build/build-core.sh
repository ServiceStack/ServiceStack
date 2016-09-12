#!/bin/sh

if [ -z "$MajorVersion" ]; then
  MajorVersion=1
fi
if [ -z "$MinorVersion" ]; then
  MinorVersion=0
fi
if [ -z "$PatchVersion" ]; then
  PatchVersion=$BUILD_NUMBER
fi
if [ -z "$RELEASE" ]; then
  UnstableTag="-unstable"
fi

Version=$MajorVersion.$MinorVersion.$PatchVersion.0
EnvVersion=$MajorVersion.$MinorVersion$PatchVersion
PackageVersion=$MajorVersion.$MinorVersion.$PatchVersion$UnstableTag

echo replace AssemblyVersion
find ./src -type f -name "AssemblyInfo.cs" -exec sed -i "s/AssemblyVersion(\"[^\"]\+\")/AssemblyVersion(\"1.0.0.0\")/g" {} +
echo replace AssemblyFileVersion
find ./src -type f -name "AssemblyInfo.cs" -exec sed -i "s/AssemblyFileVersion(\"[^\"]\+\")/AssemblyFileVersion(\"${Version}\")/g" {} +

echo replace project.json
sed -i "s/\"version\": \"[^\"]\+\"/\"version\": \"${Version}\"/g" ./src/ServiceStack.Text/project.json
sed -i "s/\"version\": \"[^\"]\+\"/\"version\": \"${Version}\"/g" ./src/ServiceStack.Client/project.json
sed -i "s/\"version\": \"[^\"]\+\"/\"version\": \"${Version}\"/g" ./src/ServiceStack.Interfaces/project.json
sed -i "s/\"version\": \"[^\"]\+\"/\"version\": \"${Version}\"/g" ./src/ServiceStack.Common/project.json

echo replace package
find ./NuGet.Core -type f -name "*.nuspec" -exec sed -i "s/<version>[^<]\+/<version>${PackageVersion}/g" {} +
find ./NuGet.Core -type f -name "*.nuspec" -exec sed -i "s/\"ServiceStack.Text.Core\" version=\"[^\"]\+\"/\"ServiceStack.Text.Core\" version=\"\[${PackageVersion}, \)\"/g" {} +
find ./NuGet.Core -type f -name "*.nuspec" -exec sed -i "s/\"ServiceStack.Interfaces.Core\" version=\"[^\"]\+\"/\"ServiceStack.Interfaces.Core\" version=\"\[${PackageVersion}, \)\"/g" {} +


#restore packages
#(cd ./src && dotnet restore)
#(cd ./tests/ServiceStack.Tests.NetCore/ServiceStack.Common.Tests && dotnet restore)

#execute tests
#(cd ./tests/ServiceStack.Tests.NetCore/ServiceStack.Common.Tests && dotnet run -c Release)

#nuget pack
#(cd ./NuGet.Core && ./nuget.exe pack ServiceStack.Common.Core/servicestack.common.core.nuspec -symbols)
