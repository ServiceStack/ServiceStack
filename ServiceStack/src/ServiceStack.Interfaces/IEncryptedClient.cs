#nullable enable

namespace ServiceStack;

public interface IEncryptedClient : IReplyClient, IHasSessionId, IHasBearerToken, IHasVersion
{
    string ServerPublicKeyXml { get; }
    IJsonServiceClient Client { get; }

    TResponse Send<TResponse>(string httpMethod, object request);
    TResponse Send<TResponse>(string httpMethod, IReturn<TResponse> request);
}