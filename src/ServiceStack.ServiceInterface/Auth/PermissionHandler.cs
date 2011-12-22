using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.Auth
{
    public static class PermissionHandler
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
                IOAuthSession session = appHost.GetCacheClient().GetSession(sessionId);

                ApplyTo httpMethod = ApplyTo.None;
                if (req.HttpMethod == HttpMethods.Get)
                    httpMethod = ApplyTo.Get;
                else if (req.HttpMethod == HttpMethods.Post)
                    httpMethod = ApplyTo.Post;
                else if (req.HttpMethod == HttpMethods.Put)
                    httpMethod = ApplyTo.Put;
                else if (req.HttpMethod == HttpMethods.Delete)
                    httpMethod = ApplyTo.Delete;
                else if (req.HttpMethod == HttpMethods.Patch)
                    httpMethod = ApplyTo.Patch;
                else if (req.HttpMethod == HttpMethods.Options)
                    httpMethod = ApplyTo.Options;
                else if (req.HttpMethod == HttpMethods.Head)
                    httpMethod = ApplyTo.Head;

                RequiredPermissionAttribute[] attributes = (RequiredPermissionAttribute[])dto.GetType().GetCustomAttributes(typeof(RequiredPermissionAttribute), true);
                foreach (RequiredPermissionAttribute attribute in attributes)
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
