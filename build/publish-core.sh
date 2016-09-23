#!/bin/bash

declare -A projects=( \
 ["ServiceStack.Kestrel"]="1.6" \
 ["ServiceStack"]="1.6" \
 ["ServiceStack.Client"]="1.1 1.6" \
 ["ServiceStack.Common"]="1.3" \
 ["ServiceStack.Interfaces"]="1.1" \
)

#for each project copy files to Nuget.Core/$project/lib folder
#and build nuget package
for proj in "${!projects[@]}"; do
  echo "$proj - ${projects[$proj]}";
  rm -r NuGet.Core/$proj.Core/lib/*

  for ver in ${projects[$proj]}; do
    mkdir -p NuGet.Core/$proj.Core/lib/netstandard$ver
    cp src/$proj/bin/Release/netstandard$ver/$proj.dll NuGet.Core/$proj.Core/lib/netstandard$ver
    cp src/$proj/bin/Release/netstandard$ver/$proj.pdb NuGet.Core/$proj.Core/lib/netstandard$ver
    cp src/$proj/bin/Release/netstandard$ver/$proj.deps.json NuGet.Core/$proj.Core/lib/netstandard$ver
  done

  (cd ./NuGet.Core && mono ./nuget.exe pack $proj.Core/$proj.Core.nuspec -symbols)

done
