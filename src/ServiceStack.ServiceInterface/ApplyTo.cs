using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface
{
    [Flags]
    public enum ApplyTo
    {
        None = 0,
        All = Get | Post | Put | Delete | Patch | Options | Head,
        Get = 1 << 0,
        Post = 1 << 1,
        Put = 1 << 2,
        Delete = 1 << 3,
        Patch = 1 << 4,
        Options = 1 << 5,
        Head = 1 << 6
    }

    public static class HttpRequestApplyToExtensions
    {
        public static ApplyTo HttpMethodAsApplyTo(this IHttpRequest req)
        {
            if (req.HttpMethod == HttpMethods.Get)
                return ApplyTo.Get;
            else if (req.HttpMethod == HttpMethods.Post)
                return ApplyTo.Post;
            else if (req.HttpMethod == HttpMethods.Put)
                return ApplyTo.Put;
            else if (req.HttpMethod == HttpMethods.Delete)
                return ApplyTo.Delete;
            else if (req.HttpMethod == HttpMethods.Patch)
                return ApplyTo.Patch;
            else if (req.HttpMethod == HttpMethods.Options)
                return ApplyTo.Options;
            else if (req.HttpMethod == HttpMethods.Head)
                return ApplyTo.Head;

            return ApplyTo.None;
        }
    }
}
