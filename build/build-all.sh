#!/bin/bash
cd ../ServiceStack/build
./build.sh
cd ../../ServiceStack.Aws/build
./build.sh
cd ../../ServiceStack.Azure/build
./build.sh
cd ../../ServiceStack.Blazor/build
./build.sh
cd ../../ServiceStack.CefGlue/build
./build.sh
cd ../../ServiceStack.Logging/build
./build.sh
cd ../../ServiceStack.OrmLite/build
./build.sh
cd ../../ServiceStack.Redis/build
./build.sh
cd ../../ServiceStack.Stripe/build
./build.sh
cd ../../ServiceStack.Text/build
./build.sh
cd ../ServiceStack.Core/build
./build.sh
