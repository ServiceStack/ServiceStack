#!/bin/bash
set -x
ls -1 | grep .nupkg | sed -e 's/\..*$//' | while read line
do 
  echo "/orgs/ServiceStack/packages/nuget/$line"
done
