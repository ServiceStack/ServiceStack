//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;

namespace ServiceStack
{
    [Flags]
    public enum Feature : int
    {
        None         = 0,
        All          = int.MaxValue,
        Soap         = Soap11 | Soap12,

        Metadata         = 1 << 0,
        PredefinedRoutes = 1 << 1,
        RequestInfo      = 1 << 2,
        
        Json         = 1 << 3,
        Xml          = 1 << 4,
        Jsv          = 1 << 5,
        Soap11       = 1 << 6,
        Soap12       = 1 << 7,
        Csv          = 1 << 8,
        Html         = 1 << 9,
        CustomFormat = 1 << 10,
        Markdown     = 1 << 11,
        Razor        = 1 << 12,
        ProtoBuf     = 1 << 13,
        MsgPack      = 1 << 14,

        ServiceDiscovery = 1 << 15,
    }
}