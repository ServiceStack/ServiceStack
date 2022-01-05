npm run ui:build 
RD /q /s ..\..\src\ServiceStack\modules\ui 
XCOPY /Y /E /H /C /I ui ..\..\src\ServiceStack\modules\ui 
DEL ..\..\src\ServiceStack\modules\ui\index.css
RD /q /s ..\..\src\ServiceStack\modules\shared 
XCOPY /Y /E /H /C /I shared ..\..\src\ServiceStack\modules\shared
