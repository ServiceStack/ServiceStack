using ServiceStack.Web;

namespace ServiceStack.Common.Tests
{
    public static class IocExtensions
    {
        public static void InjectRequestIntoDependencies(this object instance, IRequest req)
        {
            foreach (var pi in instance.GetType().GetPublicProperties())
            {
                var mi = pi.GetGetMethod();
                if (mi == null)
                    continue;

                var dep = mi.Invoke(instance, new object[0]);
                var requiresRequest = dep as IRequiresRequest;
                if (requiresRequest != null)
                {
                    requiresRequest.Request = req;
                    requiresRequest.InjectRequestIntoDependencies(req);
                }
            }
        }
    }
}