#!/bin/sh

. ../env-vars.sh

echo uploading ServiceStack/html...

rsync --verbose  --progress --stats --compress \
      -e "ssh -i $SERVER_KEY" \
      --recursive --times --perms --links \
      $BASE_PATH/html/ $HOST_LOGIN:html/servicestack.net/

