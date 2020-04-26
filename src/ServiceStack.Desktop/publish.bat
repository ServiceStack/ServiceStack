tsc && ^
bash inject-umd.sh && ^
uglifyjs lib/js/@servicestack/desktop/servicestack-desktop.js --compress --mangle -o lib/js/@servicestack/desktop/servicestack-desktop.min.js && ^
del lib\js\@servicestack\desktop\servicestack-desktop.js
