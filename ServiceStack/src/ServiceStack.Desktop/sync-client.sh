node sync-client.js
rm ../ServiceStack/js/servicestack-client.js
cp lib/js/@servicestack/client/servicestack-client.min.js ../ServiceStack/js/servicestack-client.js
tsc && \
bash inject-umd.sh && \
uglifyjs lib/js/@servicestack/desktop/servicestack-desktop.js --compress --mangle -o lib/js/@servicestack/desktop/servicestack-desktop.min.js && \
rm lib/js/@servicestack/desktop/servicestack-desktop.js
