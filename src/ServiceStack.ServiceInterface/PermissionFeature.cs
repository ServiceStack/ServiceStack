using System.Net;
using ServiceStack.Common;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
    public static class PermissionFeature
    {
        /// <summary>
        /// Adds a request filter which checks if the required permission is given
        /// </summary>
        /// <param name="appHost"></param>
        public static void Init(AppHostBase appHost)
        {
            appHost.RequestFilters.Add((req, res, dto) =>
            {
                string sessionId = req.GetPermanentSessionId();
                IAuthSession session = appHost.GetCacheClient().GetSession(sessionId);

                ApplyTo httpMethod = req.HttpMethodAsApplyTo();

                var attributes = (RequiredPermissionAttribute[])dto.GetType().GetCustomAttributes(typeof(RequiredPermissionAttribute), true);
                foreach (var attribute in attributes)
                {
                    if (attribute.ApplyTo.Has(httpMethod))
                    {
                        foreach (string requiredPermission in attribute.RequiredPermissions)
                        {
                            if (!session.HasPermission(requiredPermission))
                            {
                                res.StatusCode = (int)HttpStatusCode.Unauthorized;
                                res.Close();
                                return;
                            }
                        }
                    }
                }
            });
        }
    }
}
