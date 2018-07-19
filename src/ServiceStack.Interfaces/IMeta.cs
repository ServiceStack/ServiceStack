// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System.Collections.Generic;

namespace ServiceStack
{
    public interface IMeta
    {
        Dictionary<string, string> Meta { get; set; }
    }

    public interface IHasSessionId
    {
        string SessionId { get; set; }
    }

    public interface IHasBearerToken
    {
        string BearerToken { get; set; }
    }

    public interface IHasVersion
    {
        int Version { get; set; }
    }
}