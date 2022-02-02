using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Web;

namespace ServiceStack.Common.Tests.ServiceClient.Web
{
    public class ServiceClientBaseTester : ServiceClientBase
    {
        public override string ContentType { get { return String.Format("application/{0}", Format); } }

        public override void SerializeToStream(IRequest req, object request, System.IO.Stream stream)
        {
            throw new NotImplementedException();
        }

        public override T DeserializeFromStream<T>(System.IO.Stream stream)
        {
            throw new NotImplementedException();
        }

        public override StreamDeserializerDelegate StreamDeserializer
        {
            get
            {
                return null;
            }
        }

        public override string Format
        {
            get { return "TestFormat"; }
        }
    }
}
