#!/bin/bash
ls -1 | grep .nupkg | sed -e 's/\.[^.].[0-9].*$//' | while read line
do 
  echo ${line}
  gh api --method DELETE -H "Accept: application/vnd.github+json" /orgs/ServiceStack/packages/nuget/${line}
done
echo "Clean complete!"