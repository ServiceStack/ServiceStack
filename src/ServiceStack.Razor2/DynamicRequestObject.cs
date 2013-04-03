using System.Dynamic;
using ServiceStack.ServiceHost;

namespace ServiceStack.Razor
{
    public class DynamicRequestObject : DynamicObject
    {
        public IHttpRequest httpReq { get; set; }

        public DynamicRequestObject() { }
        public DynamicRequestObject(IHttpRequest httpReq)
        {
            this.httpReq = httpReq;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = httpReq.GetParam(binder.Name);
            return result != null;
        }
    }
}