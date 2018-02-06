SET NUGET=..\src\.nuget\nuget
%NUGET% pack ServiceStack\servicestack.nuspec -symbols
%NUGET% pack ServiceStack.Server\servicestack.server.nuspec -symbols
%NUGET% pack ServiceStack.Interfaces\servicestack.interfaces.nuspec -symbols
%NUGET% pack ServiceStack.Client\servicestack.client.nuspec -symbols
%NUGET% pack ServiceStack.Common\servicestack.common.nuspec -symbols
%NUGET% pack ServiceStack.Mvc\servicestack.mvc.nuspec -symbols
%NUGET% pack ServiceStack.Api.Swagger\servicestack.api.swagger.nuspec -symbols
%NUGET% pack ServiceStack.Razor\servicestack.razor.nuspec -symbols

%NUGET% pack ServiceStack.Host.AspNet\servicestack.host.aspnet.nuspec
%NUGET% pack ServiceStack.Host.Mvc\servicestack.host.mvc.nuspec

%NUGET% pack ServiceStack.Authentication.OpenId\servicestack.authentication.openid.nuspec -symbols
%NUGET% pack ServiceStack.Authentication.OAuth2\servicestack.authentication.oauth2.nuspec -symbols

%NUGET% pack ServiceStack.Authentication.MongoDb\servicestack.authentication.mongodb.nuspec -symbols
%NUGET% pack ServiceStack.Authentication.NHibernate\servicestack.authentication.nhibernate.nuspec -symbols
%NUGET% pack ServiceStack.Authentication.RavenDb\servicestack.authentication.ravendb.nuspec -symbols

%NUGET% pack ServiceStack.Caching.AwsDynamoDb\servicestack.caching.awsdynamodb.nuspec -symbols
%NUGET% pack ServiceStack.Caching.Azure\servicestack.caching.azure.nuspec -symbols
%NUGET% pack ServiceStack.Caching.Memcached\servicestack.caching.memcached.nuspec -symbols

%NUGET% pack ServiceStack.Logging.Elmah\servicestack.logging.elmah.nuspec -symbols
%NUGET% pack ServiceStack.Logging.EntLib5\servicestack.logging.entlib5.nuspec -symbols
%NUGET% pack ServiceStack.Logging.EventLog\servicestack.logging.eventlog.nuspec -symbols
%NUGET% pack ServiceStack.Logging.Log4Net\servicestack.logging.log4net.nuspec -symbols
%NUGET% pack ServiceStack.Logging.Log4Net\servicestack.logging.log4net.core.nuspec -symbols
%NUGET% pack ServiceStack.Logging.NLog\servicestack.logging.nlog.nuspec -symbols
%NUGET% pack ServiceStack.Logging.Serilog\servicestack.logging.serilog.nuspec -symbols

%NUGET% pack ServiceStack.ProtoBuf\servicestack.protobuf.nuspec -symbols
%NUGET% pack ServiceStack.MsgPack\servicestack.msgpack.nuspec -symbols

