SET NUGET=..\src\.nuget\nuget
%NUGET% pack ServiceStack\servicestack.nuspec -symbols
%NUGET% pack ServiceStack.Common\servicestack.common.nuspec -symbols
%NUGET% pack ServiceStack.Mvc\servicestack.mvc.nuspec -symbols
%NUGET% pack ServiceStack.Api.Swagger\servicestack.api.swagger.nuspec -symbols
%NUGET% pack ServiceStack.Razor\servicestack.razor.nuspec -symbols

%NUGET% pack ServiceStack.Host.AspNet\servicestack.host.aspnet.nuspec
%NUGET% pack ServiceStack.Host.Mvc\servicestack.host.mvc.nuspec
%NUGET% pack ServiceStack.Client.Silverlight\servicestack.client.silverlight.nuspec

%NUGET% pack ServiceStack.Authentication.OpenId\servicestack.authentication.openid.nuspec -symbols
%NUGET% pack ServiceStack.Authentication.OAuth2\servicestack.authentication.oauth2.nuspec -symbols
%NUGET% pack ServiceStack.Plugins.ProtoBuf\servicestack.plugins.protobuf.nuspec -symbols
%NUGET% pack ServiceStack.Plugins.MsgPack\servicestack.plugins.msgpack.nuspec -symbols

