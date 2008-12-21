using System;
using System.Collections.Generic;
using System.Text;
using Bbc.Ww.Services.Messaging.Serialization;

namespace Bbc.Ww.Services.Messaging.ActiveMq.Support.Async
{
    /// <summary>
    /// TODO: NOT IN USE YET:
    /// </summary>
    internal class SerializableReplyAsyncResult : MessageReplyAsyncResult
    {
        private readonly IXmlSerializer xmlSerializer;

        public SerializableReplyAsyncResult(string correlationId, IXmlSerializer xmlSerializer) : base(correlationId)
        {
            this.xmlSerializer = xmlSerializer;
        }

        public IXmlSerializer XmlSerializer
        {
            get { return xmlSerializer; }
        }
    }
}
