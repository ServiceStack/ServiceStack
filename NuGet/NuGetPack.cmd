nuget pack ServiceStack\servicestack.nuspec -symbols
nuget pack ServiceStack.Common\servicestack.common.nuspec -symbols
nuget pack ServiceStack.Mvc\servicestack.mvc.nuspec -symbols
nuget pack ServiceStack.Api.Swagger\servicestack.api.swagger.nuspec -symbols
nuget pack ServiceStack.Razor\servicestack.razor.nuspec -symbols

nuget pack ServiceStack.Host.AspNet\servicestack.host.aspnet.nuspec
nuget pack ServiceStack.Host.Mvc\servicestack.host.mvc.nuspec
nuget pack ServiceStack.Client.Silverlight\servicestack.client.silverlight.nuspec
nuget pack ServiceStack.Authentication.OpenId\servicestack.authentication.openid.nuspec -symbols
nuget pack ServiceStack.Plugins.ProtoBuf\servicestack.plugins.protobuf.nuspec -symbols
nuget pack ServiceStack.Plugins.MsgPack\servicestack.plugins.msgpack.nuspec -symbols

