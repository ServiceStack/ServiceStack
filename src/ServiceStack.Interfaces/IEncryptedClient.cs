// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

namespace ServiceStack
{
    public interface IEncryptedClient : IReplyClient, IHasSessionId, IHasVersion
    {
        string PublicKeyPath { get; set; }
        string PublicKeyXml { get; set; }
        IServiceClient Client { get; }

        TResponse Send<TResponse>(string httpMethod, object request);
        TResponse Send<TResponse>(string httpMethod, IReturn<TResponse> request);
    }
}