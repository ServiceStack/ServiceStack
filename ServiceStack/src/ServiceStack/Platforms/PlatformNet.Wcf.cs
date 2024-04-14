#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel.Channels;

namespace ServiceStack.Platforms;

public partial class PlatformNet : Platform
{

    public RequestAttributes GetRequestAttributes(System.ServiceModel.OperationContext operationContext)
    {
        if (!HostContext.Config.EnableAccessRestrictions) return default(RequestAttributes);

        var portRestrictions = default(RequestAttributes);
        var ipAddress = GetIpAddress(operationContext);

        portRestrictions |= HttpRequestExtensions.GetAttributes(ipAddress);

        //TODO: work out if the request was over a secure channel			
        //portRestrictions |= request.IsSecureConnection ? PortRestriction.Secure : PortRestriction.InSecure;

        return portRestrictions;
    }

    public static IPAddress GetIpAddress(System.ServiceModel.OperationContext context)
    {
#if !MONO
        var prop = context.IncomingMessageProperties;
        if (context.IncomingMessageProperties.ContainsKey(RemoteEndpointMessageProperty.Name))
        {
            if (prop[RemoteEndpointMessageProperty.Name] is RemoteEndpointMessageProperty endpoint)
                return IPAddress.Parse(endpoint.Address);
        }
#endif
        return null;
    }
}

#endif
