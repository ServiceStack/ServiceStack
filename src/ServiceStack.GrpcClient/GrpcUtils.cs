using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Grpc.Core;
using ServiceStack.Text;

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
                return await auc.ResponseAsync.ConfigAwait();
            }
        }

        public static HttpClientHandler AddPemCertificate(this HttpClientHandler handler, X509Certificate2 cert,
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>
                serverCertificateCustomValidationCallback = null)
        {
            handler.ClientCertificates.Add(cert);
            if (serverCertificateCustomValidationCallback != null)
                handler.ServerCertificateCustomValidationCallback = serverCertificateCustomValidationCallback;
            return handler;
        }

        public static HttpClientHandler AddPemCertificateFromFile(this HttpClientHandler handler, string fileName,
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> serverCertificateCustomValidationCallback = null) =>
            handler.AddPemCertificate(new X509Certificate2(fileName), serverCertificateCustomValidationCallback);

        public static HttpClientHandler AllowSelfSignedCertificatesFrom(this HttpClientHandler handler, string dnsName)
        {
            handler.ServerCertificateCustomValidationCallback = AllowSelfSignedCertificatesFrom(dnsName);
            return handler;
        }
        
        public static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> AllowSelfSignedCertificatesFrom(string dnsName) =>
            (req, cert, certChain, sslPolicyErrors) =>
                cert.SubjectName.RawData.SequenceEqual(cert.IssuerName.RawData) && // self-signed
                cert.GetNameInfo(X509NameType.DnsName, forIssuer: false) == dnsName &&
                (sslPolicyErrors & ~SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.None; // only this

        public static void Set(this Metadata headers, string name, string value)
        {
            for (var i = 0; i < headers.Count; i++)
            {
                var entry = headers[i];
                if (entry.Key.EqualsIgnoreCase(name))
                {
                    headers.RemoveAt(i);
                    break;
                }
            }

            headers.Add(name, value);
        }
        
        public static CallOptions Init(this CallOptions options, GrpcClientConfig config, bool noAuth)
        {
            var auth = noAuth
                ? null
                : !string.IsNullOrEmpty(config.UserName) && !string.IsNullOrEmpty(config.Password)
                    ? "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(config.UserName + ":" + config.Password))
                    : !string.IsNullOrEmpty(config.BearerToken)
                        ? "Bearer " + config.BearerToken
                        : !string.IsNullOrEmpty(config.SessionId)
                            ? nameof(config.SessionId)
                            : null;

            if (config.Headers.Count > 0 || auth != null || config.UserAgent != null)
            {
                var headers = options.Headers;
                if (headers == null)
                    options = options.WithHeaders(headers = new Metadata());
                
                foreach (var entry in config.Headers)
                {
                    headers.Add(entry);
                }

                if (auth != null)
                {
                    if (auth == nameof(config.SessionId))
                        headers.Set(GrpcClientConfig.Keywords.HeaderSessionId, config.SessionId);
                    else
                        headers.Set(HttpHeaders.Authorization, auth);
                }

                if (config.UserAgent != null)
                    headers.Set(HttpHeaders.UserAgent, config.UserAgent);
            }
            return options;
        }

        public static bool InitRequestDto(GrpcClientConfig config, object requestDto)
        {
            config.PopulateRequestMetadata(requestDto);
            var authIncluded = !string.IsNullOrEmpty((requestDto is IHasBearerToken hasBearerToken ? hasBearerToken.BearerToken : null) ?? 
                (requestDto is IHasSessionId hasSessionId ? hasSessionId.SessionId : null));
            return authIncluded;
        }

        public static async Task<(TResponse, ResponseStatus, Metadata)> GetResponseAsync<TResponse>(GrpcClientConfig config, AsyncUnaryCall<TResponse> auc)
        {
            var headers = await auc.ResponseHeadersAsync.ConfigAwait();
            object status = null;
            ResponseStatus typedStatus = null;
            string errorCode = null;
            string message = null;
            TResponse response = default;
            try
            {
                response = await auc.ResponseAsync.ConfigAwait();
                status = response.GetResponseStatus();

                if (response is AuthenticateResponse authResponse)
                {
                    if (!string.IsNullOrEmpty(authResponse.BearerToken))
                        config.BearerToken = authResponse.BearerToken;
                    else if (!string.IsNullOrEmpty(authResponse.SessionId))
                        config.SessionId = authResponse.SessionId;
                }
            }
            catch (RpcException ex)
            {
                var statusBytes = ResponseCallContext.GetHeaderBytes(headers, GrpcClientConfig.Keywords.GrpcResponseStatus);
                status = statusBytes != null 
                    ? GrpcServiceStack.ParseResponseStatus(statusBytes)
                    : new ResponseStatus {
                        ErrorCode = errorCode = ex.Status.Detail ?? ex.StatusCode.ToString(),
                        Message = message = HttpStatus.GetStatusDescription(ResponseCallContext.GetHttpStatus(headers)) 
                    };

                typedStatus = status as ResponseStatus; 
                if (typedStatus != null && string.IsNullOrEmpty(typedStatus.Message))
                {
                    typedStatus.ErrorCode = errorCode = ex.StatusCode.ToString();
                    typedStatus.Message = message = ex.Status.Detail;
                }

                var prop = TypeProperties<TResponse>.GetAccessor(nameof(IHasResponseStatus.ResponseStatus));
                if (prop != null)
                {
                    response = typeof(TResponse).CreateInstance<TResponse>();
                    if (prop.PropertyInfo.PropertyType.IsInstanceOfType(status))
                    {
                        prop.PublicSetter(response, status);
                    }
                    else
                    {
                        // Protoc clients generate different ResponseStatus DTO
                        var propStatus = status.ConvertTo(prop.PropertyInfo.PropertyType);
                        prop.PublicSetter(response, propStatus);
                    }
                }
            }
            finally
            {
                await InvokeResponseFiltersAsync(config, auc, response).ConfigAwait();
            }

            if (typedStatus == null)
                typedStatus = status as ResponseStatus;
            
            if (typedStatus == null && status != null)
            {
                typedStatus = new ResponseStatus {
                    ErrorCode = errorCode ?? TypeProperties.Get(status.GetType()).GetPublicGetter(nameof(ResponseStatus.ErrorCode))(status) as string,
                    Message = message ?? TypeProperties.Get(status.GetType()).GetPublicGetter(nameof(ResponseStatus.Message))(status) as string,
                };
            }

            return (response, typedStatus, headers);
        }

        public static async Task<Metadata> InvokeResponseFiltersAsync<TResponse>(GrpcClientConfig config, AsyncUnaryCall<TResponse> auc, TResponse response, Action<ResponseCallContext> fn = null)
        {
            var headers = await auc.ResponseHeadersAsync.ConfigAwait();
            if (GrpcClientConfig.GlobalResponseFilter != null || config.ResponseFilter != null)
            {
                var ctx = new ResponseCallContext(response, auc.GetStatus(), headers);
                fn?.Invoke(ctx);

                GrpcClientConfig.GlobalResponseFilter?.Invoke(ctx);
                config.ResponseFilter?.Invoke(ctx);
            }
            return headers;
        }

        public static async Task<Metadata> InvokeResponseFiltersAsync<TResponse>(GrpcClientConfig config, AsyncServerStreamingCall<TResponse> asc, IAsyncStreamReader<TResponse> response, Action<ResponseCallContext> fn = null)
        {
            var headers = await asc.ResponseHeadersAsync.ConfigAwait();
            if (GrpcClientConfig.GlobalResponseFilter != null || config.ResponseFilter != null)
            {
                var ctx = new ResponseCallContext(response, asc.GetStatus(), headers);
                fn?.Invoke(ctx);

                GrpcClientConfig.GlobalResponseFilter?.Invoke(ctx);
                config.ResponseFilter?.Invoke(ctx);
            }
            return headers;
        }

        public static async Task<(IAsyncStreamReader<TResponse>, ResponseStatus, Metadata)> GetResponseAsync<TResponse>(GrpcClientConfig config, AsyncServerStreamingCall<TResponse> auc)
        {
            var headers = await auc.ResponseHeadersAsync.ConfigAwait();
            ResponseStatus status = null;
            IAsyncStreamReader<TResponse> response = default;
            try
            {
                response = auc.ResponseStream;
                status = response.GetResponseStatus();
            }
            catch (RpcException ex)
            {
                status = HandleRpcException(headers, ex);
            }
            finally
            {
                await InvokeResponseFiltersAsync(config, auc, response).ConfigAwait();
            }

            return (response, status, headers);
        }

        public static ResponseStatus HandleRpcException(Metadata headers, RpcException ex)
        {
            var statusBytes = ResponseCallContext.GetHeaderBytes(headers, GrpcClientConfig.Keywords.GrpcResponseStatus);
            var status = statusBytes != null
                ? GrpcMarshaller<ResponseStatus>.Instance.Deserializer(statusBytes)
                : new ResponseStatus {
                    ErrorCode = ex.Status.Detail ?? ex.StatusCode.ToString(),
                    Message = HttpStatus.GetStatusDescription(ResponseCallContext.GetHttpStatus(headers))
                };
            return status;
        }
        
        public static WebHeaderCollection ResolveHeaders(Metadata headers)
        {
            var to = new WebHeaderCollection();
            foreach (var header in headers)
            {
                if (header.Key.EndsWith("-bin"))
                    continue;
                
                to[header.Key] = header.Value;
            }
            return to;
        }

        public static Metadata ToHeaders(Dictionary<string, string> headers)
        {
            var to = new Metadata();
            foreach (var entry in headers)
            {
                to.Add(entry.Key, entry.Value);
            }
            return to;
        }

        public static Metadata ToHeaders<T>(T headers)
        {
            var to = new Metadata();
            var objDictionary = headers.ToObjectDictionary();
            foreach (var entry in objDictionary)
            {
                var val = entry.Value.ConvertTo<string>();
                to.Add(entry.Key, val);
            }
            return to;
        }
    }
}