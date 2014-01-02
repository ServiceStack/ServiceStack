//Copyright (c) Service Stack LLC. All Rights Reserved.
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