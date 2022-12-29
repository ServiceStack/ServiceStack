//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if XBOX
using System.Collections.Specialized;
using System.Web;
using ServiceStack.Pcl;
using ServiceStack.Web;

namespace ServiceStack
{
    public class XboxExportClient : PclExportClient
    {
        public static XboxExportClient Provider = new XboxExportClient();

        public static PclExportClient Configure()
        {
            Configure(Provider);
            XboxPclExport.Configure();
            return Provider;
        }

        public override INameValueCollection NewNameValueCollection()
        {
            return new NameValueCollectionWrapper(new NameValueCollection());
        }

        public override INameValueCollection ParseQueryString(string query)
        {
            return HttpUtility.ParseQueryString(query).InWrapper();
        }
    }
}
#endif