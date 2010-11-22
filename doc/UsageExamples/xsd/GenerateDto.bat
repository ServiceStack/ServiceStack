REM ECHO OFF

SET SVC_UTIL="C:\Program Files\Microsoft SDKs\Windows\v6.0A\bin\SvcUtil.exe"
SET CS_PATH=XsdDto.cs
SET CS_NAMESPACE=ServiceStack.UsageExamples.xsd
SET WGET="..\Lib\wget.exe"

%WGET% http://localhost/ServiceStack.Sakila.Host.WebService/Public/Metadata?xsd=1 -O ServiceTypes.xsd
%WGET% http://localhost/ServiceStack.Sakila.Host.WebService/Public/Metadata?xsd=0 -O WcfDataTypes.xsd
%WGET% http://localhost/ServiceStack.Sakila.Host.WebService/Public/Metadata?xsd=2 -O WcfCollectionTypes.xsd

ECHO %SVC_UTIL% /dconly *.xsd /out:%CS_PATH% /n:%CS_NAMESPACE% /s
%SVC_UTIL% /dconly *.xsd /out:%CS_PATH% /n:*,%CS_NAMESPACE% /s
