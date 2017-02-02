//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;

namespace ServiceStack.Model
{
    public interface ICacheByDateModified
    {
        DateTime? LastModified { get; }
    }
}