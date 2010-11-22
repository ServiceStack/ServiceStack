ECHO OFF

SET SVC_UTIL="C:\Program Files\Microsoft SDKs\Windows\v6.0A\bin\SvcUtil.exe"
SET WGET="..\Lib\wget.exe"
SET CS_NAMESPACE=ServiceStack.UsageExamples.svc
SET CS_PATH=SvcSyncReplyClient.cs
%WGET% http://localhost/ServiceStack.Sakila.Host.WebService/Public/Soap12/Wsdl -O Service.wsdl

%SVC_UTIL% Service.wsdl /out:%CS_PATH% /n:*,%CS_NAMESPACE% /s
