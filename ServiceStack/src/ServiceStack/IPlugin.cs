using System;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class Plugins
    {
        public const string Grpc = "grpc";
        public const string Cors = "cors";
        public const string AutoQuery = "autoquery";
        public const string AutoQueryData = "autoquerydata";
        public const string AutoQueryMetadata = "autoquerymeta";
        public const string NativeTypes = "ssref";
        public const string Auth = "auth";
        public const string Csv = "csv";
        public const string Html = "html";
        public const string HttpCache = "httpcache";
        public const string LispTcpServer = "lisptcp";
        public const string EncryptedMessaging = "cryptmsg";
        public const string Metadata = "metadata";
        public const string MsgPack = "msgpack";
        public const string OpenApi = "openapi";
        public const string Postman = "postman";
        public const string PredefinedRoutes = "autoroutes";
        public const string PrettyUrls = "prettyurls";
        public const string ProtoBuf = "protobuf";
        public const string Razor = "razor";
        public const string Register = "register";
        public const string RequestInfo = "reqinfo";
        public const string Proxy = "proxy";
        public const string RequestLogs = "reqlogs";
        public const string ServerEvents = "sse";
        public const string Session = "session";
        public const string SharpPages = "sharp";
        public const string Sitemap = "sitemap";
        public const string Soap = "soap";
        public const string Svg = "svg";
        public const string Validation = "validation";
        public const string Desktop = "desktop";
        public const string WebSudo = "websudo";
        public const string CancelRequests = "reqcancel";
        public const string Swagger = "swagger";
        public const string MiniProfiler = "miniprofiler";
        public const string HotReload = "hotreload";
        public const string RedisErrorLogs = "redislogs";
        public const string AdminUsers = "adminusers";
        public const string AdminRedis = "adminredis";
        public const string AdminDatabase = "admindb";
        public const string Ui = "ui";
        public const string FileUpload = "filesupload";
        public const string Profiling = "profiling";
        public const string RunAsAdmin = "runasadmin";

        public static void AddToAppMetadata(this IAppHost appHost, Action<AppMetadata> fn)
        {
            var feature = appHost.GetPlugin<MetadataFeature>();
            if (feature == null)
                return;
            
            if (fn != null)
                feature.AppMetadataFilters.Add(fn);
        }

        public static void ModifyAppMetadata(this IAppHost appHost, Action<IRequest,AppMetadata> fn)
        {
            var feature = appHost.GetPlugin<MetadataFeature>();
            if (feature == null)
                return;
            
            if (fn != null)
                feature.AfterAppMetadataFilters.Add(fn);
        }
    }
    
    /// <summary>
    /// Callback for Plugins to register necessary handlers with ServiceStack
    /// </summary>
    public interface IPlugin
    {
        void Register(IAppHost appHost);
    }

    /// <summary>
    /// Callback to pre-configure any logic before IPlugin.Register() is fired
    /// </summary>
    public interface IPreInitPlugin
    {
        void BeforePluginsLoaded(IAppHost appHost);
    }

    /// <summary>
    /// Callback to post-configure any logic after IPlugin.Register() is fired
    /// </summary>
    public interface IPostInitPlugin
    {
        void AfterPluginsLoaded(IAppHost appHost);
    }

    /// <summary>
    /// Callback for AuthProviders to register callbacks with AuthFeature
    /// </summary>
    public interface IAuthPlugin
    {
        void Register(IAppHost appHost, AuthFeature feature);
    }

    public interface IMsgPackPlugin { }         //Marker for MsgPack plugin
    public interface IWirePlugin { }            //Marker for Wire plugin
    public interface INetSerializerPlugin { }   //Marker for NetSerialize plugin
    public interface IRazorPlugin { }           //Marker for MVC Razor plugin

    //Marker for ProtoBuf plugin
    public interface IProtoBufPlugin
    {
        string GetProto(Type type);
    }        
}