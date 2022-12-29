copy /v/y orig\redis-6380\* redis-6380
copy /v/y orig\redis-6381\* redis-6381
copy /v/y orig\redis-6382\* redis-6382

del /F /Q redis-6380\dump.rdb
del /F /Q redis-6381\dump.rdb
del /F /Q redis-6382\dump.rdb

redis\redis-cli -p 6380 SHUTDOWN NOSAVE
redis\redis-cli -p 6381 SHUTDOWN NOSAVE
redis\redis-cli -p 6382 SHUTDOWN NOSAVE
redis\redis-cli -p 26380 SHUTDOWN NOSAVE
redis\redis-cli -p 26381 SHUTDOWN NOSAVE
redis\redis-cli -p 26382 SHUTDOWN NOSAVE
