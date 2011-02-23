SET WEB_SERVER="C:\Program Files\Common Files\microsoft shared\DevServer\10.0\WebDev.WebServer20.EXE"

%WEB_SERVER% /port:5001 /path:"C:\src\ServiceStack\tests\ServiceStack.HostTests" /vpath:"/"
