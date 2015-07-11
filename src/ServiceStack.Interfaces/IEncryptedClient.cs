﻿// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

namespace ServiceStack
{
    public interface IEncryptedClient : IReplyClient, IHasSessionId, IHasVersion
    {
        string ServerPublicKeyXml { get; }
        IJsonServiceClient Client { get; }

        TResponse Send<TResponse>(string httpMethod, object request);
        TResponse Send<TResponse>(string httpMethod, IReturn<TResponse> request);
    }
}