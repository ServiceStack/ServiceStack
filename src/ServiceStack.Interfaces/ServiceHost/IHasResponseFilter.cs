using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.ServiceHost
{
    public interface IHasResponseFilter
    {
        void ResponseFilter(IHttpRequest req, IHttpResponse res, object responseDto);
    }
}
