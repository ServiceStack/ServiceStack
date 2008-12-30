using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.UsageExamples.Support
{
    public class TestBase
    {
        public int CustomerId
        {
            get { return 1; }
        }

        public List<int> CustomerIds
        {
			get { return new[] { 1, 2 }.ToList(); }
        }

        public string WsSyncReplyUri
        {
            get
            {
                return "http://localhost/ServiceStack.Sakila.Host.WebService/Endpoints/Soap12/SyncReply.svc";
            }
        }

        public string BasicHttpSyncReplyUri
        {
            get
            {
                return "http://localhost/ServiceStack.Sakila.Host.WebService/Endpoints/Soap11/SyncReply.svc";
            }
        }

        public string JsonSyncReplyBaseUri
        {
            get
            {
                return "http://localhost/ServiceStack.Sakila.Host.WebService/Endpoints/Json/SyncReply.ashx";
            }
        }

        public string XmlSyncReplyBaseUri
        {
            get
            {
                return "http://localhost/ServiceStack.Sakila.Host.WebService/Endpoints/Xml/SyncReply.ashx";
            }
        }

        protected static string RestBaseUri
        {
            get
            {
                return "http://localhost/ServiceStack.Sakila.Host.WebService/Rest/";
            }
        }

        protected Uri ProxyAddress
        {
            get
            {
                return new Uri("http://127.0.0.1:8888");
            }
        }
    }
}