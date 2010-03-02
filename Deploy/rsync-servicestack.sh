#!/bin/sh

. ../env-vars.sh

rsync --verbose  --progress --stats --compress \
      -e "ssh -i $SERVER_KEY" \
      --recursive --times --perms --links \
      $BASE_PATH/html/ $HOST_LOGIN:html/servicestack.net/


rsync --verbose  --progress --stats --compress \
      -e "ssh -i $SERVER_KEY" \
      --recursive --times --perms --links \
      --exclude ".svn" \
      --exclude "App_Data" \
      --exclude "obj" \
      --exclude "Properties" \
      --exclude "*.cs" \
      --exclude "*.csproj*" \
      --exclude "*.config" \
      $BASE_PATH/ServiceStack.Examples/ServiceStack.Examples.Clients $HOST_LOGIN:mono/servicestack.net/


rsync --verbose  --progress --stats --compress \
      -e "ssh -i $SERVER_KEY" \
      --recursive --times --perms --links \
      --exclude ".svn" \
      --exclude "App_Data" \
      --exclude "obj" \
      --exclude "Properties" \
      --exclude "*.cs" \
      --exclude "*.csproj*" \
      --exclude "*.config" \
      $BASE_PATH/ServiceStack.Examples/ServiceStack.Examples.Host.Web $HOST_LOGIN:mono/servicestack.net/

