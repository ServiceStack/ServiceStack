using System;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Grpc.Core;

namespace ServiceStack
{
    public static class GrpcUtils
    {
        public static Task<TResponse> Execute<TRequest, TResponse>(this Channel channel, TRequest request,
            string servicesName, string methodName,
            CallOptions options = default, string host = null)
            where TRequest : class
            where TResponse : class
            => Execute<TRequest, TResponse>(new DefaultCallInvoker(channel), request, servicesName, methodName, options,
                host);

        public static async Task<TResponse> Execute<TRequest, TResponse>(this CallInvoker invoker, TRequest request,
            string servicesName, string methodName,
            CallOptions options = default, string host = null)
            where TRequest : class
            where TResponse : class
        {
            var method = new Method<TRequest, TResponse>(MethodType.Unary, servicesName, methodName,
                GrpcMarshaller<TRequest>.Instance, GrpcMarshaller<TResponse>.Instance);
            using (var auc = invoker.AsyncUnaryCall(method, host, options, request))
            {
                return await auc.ResponseAsync;
            }
        }

        public static HttpClientHandler AddPemCertificateFromFile(this HttpClientHandler handler, string fileName, 
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> serverCertificateCustomValidationCallback = null)
        {
            handler.ClientCertificates.Add(new X509Certificate2(fileName));
            if (serverCertificateCustomValidationCallback != null)
                handler.ServerCertificateCustomValidationCallback = serverCertificateCustomValidationCallback;
            return handler;
        }

        public static HttpClientHandler AllowSelfSignedCertificatesFrom(this HttpClientHandler handler, string dnsName)
        {
            handler.ServerCertificateCustomValidationCallback = (req, cert, certChain, sslPolicyErrors) =>
                cert.SubjectName.RawData.SequenceEqual(cert.IssuerName.RawData) && // self-signed
                cert.GetNameInfo(X509NameType.DnsName, forIssuer: false) == dnsName &&
                (sslPolicyErrors & ~SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.None; // only this
            return handler;
        }
    }
}