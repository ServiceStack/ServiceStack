//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

namespace ServiceStack.Model
{
    public interface ICacheByEtag
    {
        string Etag { get; }
    }
}