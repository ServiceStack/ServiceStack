#!/bin/bash

declare -A projects=( \
 ["ServiceStack.Kestrel"]="1.6" \
 ["ServiceStack.Api.Swagger.Core"]="1.6" \
 ["ServiceStack.RabbitMq.Core"]="1.6" \
 ["ServiceStack.Mvc.Core"]="1.6" \
 ["ServiceStack.Server.Core"]="1.6" \
 ["ServiceStack.Core"]="1.6" \
 ["ServiceStack.Client.Core"]="1.1 1.6" \
 ["ServiceStack.HttpClient.Core"]="1.1 1.6" \
 ["ServiceStack.Common.Core"]="1.3" \
 ["ServiceStack.Interfaces.Core"]="1.1" \
)

#for each project copy files to Nuget.Core/$project/lib folder
#and build nuget package
for proj in "${!projects[@]}"; do
  projname=$(basename ${proj} .Core)
  echo "$proj - ${projects[$proj]}";
  rm -r NuGet.Core/$proj/lib/*

  for ver in ${projects[$proj]}; do
    mkdir -p NuGet.Core/$proj/lib/netstandard$ver
    cp src/$projname/bin/Release/netstandard$ver/$projname.dll NuGet.Core/$proj/lib/netstandard$ver
    cp src/$projname/bin/Release/netstandard$ver/$projname.pdb NuGet.Core/$proj/lib/netstandard$ver
    cp src/$projname/bin/Release/netstandard$ver/$projname.deps.json NuGet.Core/$proj/lib/netstandard$ver
  done

  (cd ./NuGet.Core && mono ./nuget.exe pack $proj/$proj.nuspec -symbols)

done
