//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if ANDROID
using System.Collections.Specialized;
using ServiceStack.Web;

namespace ServiceStack
{
    public class AndroidPclExportClient : PclExportClient
    {
        public override INameValueCollection NewNameValueCollection()
        {
            return new NameValueCollectionWrapper(new NameValueCollection());
        }

        public override INameValueCollection ParseQueryString(string query)
        {
            return ServiceStack.Pcl.HttpUtility.ParseQueryString(query).InWrapper();
        }
    }
}
#endif
