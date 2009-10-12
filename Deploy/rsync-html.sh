#!/bin/sh

. ../env-vars.sh

echo uploading ServiceStack/html...

rsync --verbose  --progress --stats --compress \
      -e "ssh -i $SERVER_KEY" \
      --recursive --times --perms --links \
      $BASE_PATH/html/ $HOST_LOGIN:html/servicestack.net/

rsync --verbose  --progress --stats --compress \
      -e "ssh -i $SERVER_KEY" \
      --recursive --times --perms --links --delete \
      --exclude ".svn" \
      $BASE_PATH/MonoTouch.Examples/html/ $HOST_LOGIN:html/servicestack.net/monotouch
