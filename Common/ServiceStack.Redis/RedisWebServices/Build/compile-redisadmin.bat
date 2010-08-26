SET CL_PATH=../RedisWebServices.Host/closure-library
SET APP_PATH=../RedisWebServices.Host/AjaxClient/

REM Different optimization levels for the Google Closure Library. 

REM python %CL_PATH%/closure/bin/build/closurebuilder.py --root=%CL_PATH% --root=%APP_PATH% --namespace="redisadmin.App" --output_mode=script --compiler_jar=compiler.jar > %APP_PATH%/redisadmin-compiled.js

python %CL_PATH%/closure/bin/build/closurebuilder.py --root=%CL_PATH% --root=%APP_PATH% --namespace="redisadmin.App" --output_mode=compiled --compiler_jar=compiler.jar > %APP_PATH%/redisadmin-compiled.js

REM python %CL_PATH%/closure/bin/build/closurebuilder.py --root=%CL_PATH% --root=%APP_PATH% --namespace="redisadmin.App" --output_mode=compiled --compiler_jar=compiler.jar --compiler_flags="--compilation_level=ADVANCED_OPTIMIZATIONS" > %APP_PATH%/redisadmin-compiled.js
