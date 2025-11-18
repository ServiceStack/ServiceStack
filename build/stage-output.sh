#!/bin/bash
rm -rf ./staging
mkdir -p staging
cp ../ServiceStack/NuGet/*.* ./staging/
cp ../ServiceStack.Aws/NuGet/*.* ./staging/
cp ../ServiceStack.Azure/NuGet/*.* ./staging/
cp ../ServiceStack.Blazor/NuGet/*.* ./staging/
cp ../ServiceStack.Logging/NuGet/*.* ./staging/
cp ../ServiceStack.OrmLite/NuGet/*.* ./staging/
cp ../ServiceStack.Redis/NuGet/*.* ./staging/
cp ../ServiceStack.Stripe/NuGet/*.* ./staging/
cp ../ServiceStack.Text/NuGet/*.* ./staging/
