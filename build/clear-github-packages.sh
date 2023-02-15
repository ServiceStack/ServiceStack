#!/bin/bash
ls -1 | grep .nupkg | sed -e 's/\.[^.]*$//' | while read line
do 
  gh api --method DELETE -H "Accept: application/vnd.github+json" /orgs/ServiceStack/packages/nuget/${line}
done
