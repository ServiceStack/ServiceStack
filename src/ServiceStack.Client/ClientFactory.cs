using System;
using ServiceStack.Text;

namespace ServiceStack
{
    public static class ClientFactory
    {
         public static IOneWayClient Create(string endpointUrl)
        {
             if (endpointUrl.IsNullOrEmpty() || !endpointUrl.StartsWith("http"))
                return null;

             if (endpointUrl.IndexOf("format=") == -1 || endpointUrl.IndexOf("format=json") >= 0)
                 return new JsonServiceClient(endpointUrl);

             if (endpointUrl.IndexOf("format=jsv") >= 0)
                 return new JsvServiceClient(endpointUrl);

#if !LITE
            if (endpointUrl.IndexOf("format=xml") >= 0)
                return new XmlServiceClient(endpointUrl);
#endif
#if !(SL5 || XBOX || ANDROID || __IOS__ || __MAC__ || PCL || LITE)
            if (endpointUrl.IndexOf("format=soap11") >= 0)
                 return new Soap11ServiceClient(endpointUrl);

             if (endpointUrl.IndexOf("format=soap12") >= 0)
                 return new Soap12ServiceClient(endpointUrl);
#endif

            throw new NotImplementedException("could not find service client for " + endpointUrl);
         }
    }
}