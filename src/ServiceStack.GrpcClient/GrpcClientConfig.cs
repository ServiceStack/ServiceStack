using System;
using Grpc.Core;
using Grpc.Net.Client;
using ProtoBuf.Grpc;
using ServiceStack.Text;

namespace ServiceStack
{
    public class GrpcClientConfig : IHasSessionId, IHasBearerToken, IHasVersion
    {
        public static class Keywords
        {
            internal const string HeaderSessionId = "X-ss-id";
            internal const string HttpStatus = "httpstatus";
            internal const string GrpcResponseStatus = "responsestatus-bin";
            internal const string Dynamic = nameof(Dynamic);
        }

        public GrpcChannel Channel { get; set; }

        public string ServicesName { get; set; } = "GrpcServices";
        public string BaseUri { get; set; }
        public string SessionId { get; set; }
        public string BearerToken { get; set; }
        public string RefreshToken { get; set; }
        public string RefreshTokenUri { get; set; }
        public int Version { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public Grpc.Core.Metadata Headers { get; set; } = new Metadata();

        public static Action<CallContext> GlobalRequestFilter { get; set; }
        public Action<CallContext> RequestFilter { get; set; }
        public static Action<ResponseCallContext> GlobalResponseFilter { get; set; }
        public Action<ResponseCallContext> ResponseFilter { get; set; }

        public string UserAgent { get; set; } = ".NET gRPC Client " + Env.VersionString;

        public CallInvoker Init() => Channel.ForServiceStack(this);
    }
    
}