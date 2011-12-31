using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.ServiceHost
{
    public interface IHasRequestFilter
    {
        void RequestFilter(IHttpRequest req, IHttpResponse res, object requestDto);
    }
}
