﻿#if !NETSTANDARD2_0

//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Reflection;

namespace ServiceStack 
{
    public abstract class AppSelfHostBase : AppHostHttpListenerPoolBase
    {
        protected AppSelfHostBase(string serviceName, params Assembly[] assembliesWithServices) 
            : base(serviceName, assembliesWithServices) { }
        
        protected AppSelfHostBase(string serviceName, string handlerPath, params Assembly[] assembliesWithServices) 
            : base(serviceName, handlerPath, assembliesWithServices) { }
    }
}

#endif