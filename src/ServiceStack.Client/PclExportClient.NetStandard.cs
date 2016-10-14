//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if NETSTANDARD1_1 || NETSTANDARD1_6
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using ServiceStack;
using ServiceStack.Web;
using ServiceStack.Pcl;


namespace ServiceStack
{
    using System.Web;

    public class NetStandardPclExportClient : PclExportClient
    {
        public static NetStandardPclExportClient Provider = new NetStandardPclExportClient();

        public static PclExportClient Configure()
        {
            Configure(Provider ?? (Provider = new NetStandardPclExportClient()));
            NetStandardPclExport.Configure();
            return Provider;
        }

        public override INameValueCollection NewNameValueCollection()
        {
            return new NameValueCollectionWrapper(new NameValueCollection());
        }

    }
}
#endif
