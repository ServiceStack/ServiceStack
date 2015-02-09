﻿using System;
using ServiceStack.Web;

namespace ServiceStack
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class ClientCanSwapTemplatesAttribute : RequestFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            req.Items["View"] = req.GetParam("View");
            req.Items["Template"] = req.GetParam("Template");
        }
    }
}