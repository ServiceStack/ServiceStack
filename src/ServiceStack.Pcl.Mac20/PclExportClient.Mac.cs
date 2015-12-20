//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if __MAC__
using System;
using System.Collections.Specialized;
using System.Threading;
using ServiceStack.Web;

namespace ServiceStack
{
    public class MacPclExportClient : PclExportClient
    {
		public static MacPclExportClient Provider = new MacPclExportClient();

        public override INameValueCollection NewNameValueCollection()
        {
            return new NameValueCollectionWrapper(new NameValueCollection());
        }

        public override INameValueCollection ParseQueryString(string query)
        {
            return ServiceStack.Pcl.HttpUtility.ParseQueryString(query).InWrapper();
        }

        public static PclExportClient Configure()
        {
            Configure(Provider ?? (Provider = new MacPclExportClient()));
            MacPclExport.Configure();
            return Provider;
        }
    }
}
#endif