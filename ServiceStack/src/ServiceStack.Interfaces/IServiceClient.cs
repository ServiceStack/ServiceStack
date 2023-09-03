using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack;

public interface IRestServiceClient : IServiceClientAsync, IServiceClientSync, IHasSessionId, IHasBearerToken, IHasVersion {}
public interface IServiceClient : IRestServiceClient, IHttpRestClientAsync, IReplyClient, IOneWayClient, IRestClient {}

public interface IJsonServiceClient : IServiceClient
{
    string BaseUri { get; }
}

public interface IReplyClient : IServiceGateway { }

public interface IServiceClientCommon : IDisposable
{
    void SetCredentials(string userName, string password);
}

public interface IServiceClientAsync : IServiceGatewayAsync, IRestClientAsync {}
public interface IServiceClientSync : IServiceGateway, IRestClientSync {}