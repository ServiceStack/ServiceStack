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
                return "http://localhost/ServiceStack.Sakila.Host.WebService/Public/Soap12/SyncReply.svc";
            }
        }

        public string BasicHttpSyncReplyUri
        {
            get
            {
				return "http://localhost/ServiceStack.Sakila.Host.WebService/Public/Soap11/SyncReply.svc";
            }
        }

        public string JsonSyncReplyBaseUri
        {
            get
            {
                return "http://localhost/ServiceStack.Sakila.Host.WebService/Public/Json/SyncReply";
            }
        }

        public string XmlSyncReplyBaseUri
        {
            get
            {
				return "http://localhost/ServiceStack.Sakila.Host.WebService/Public/Xml/SyncReply";
            }
        }
    }
}