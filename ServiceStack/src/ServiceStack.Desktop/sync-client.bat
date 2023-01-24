node sync-client.js
del ..\ServiceStack\js\servicestack-client.js
copy lib\js\@servicestack\client\servicestack-client.min.js ..\ServiceStack\js\servicestack-client.js
tsc && ^
bash inject-umd.sh && ^
uglifyjs lib/js/@servicestack/desktop/servicestack-desktop.js --compress --mangle -o lib/js/@servicestack/desktop/servicestack-desktop.min.js && ^
del lib\js\@servicestack\desktop\servicestack-desktop.js
