using System;
using ServiceStack.Web;

namespace ServiceStack;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class ClientCanSwapTemplatesAttribute : RequestFilterAttribute
{
    public override void Execute(IRequest req, IResponse res, object requestDto)
    {
        req.SetItem(Keywords.View, req.GetParam(Keywords.View));
        req.SetItem(Keywords.Template, req.GetParam(Keywords.Template));
    }
}