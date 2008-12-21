using System;

namespace ServiceStack.Messaging.Tests.Objects.Exceptions
{
    public class MessagingException : Exception, IXmlSerializable
    {
        public MessagingException(string message) : base(message)
        {
        }

        public string ToXml()
        {
            return string.Format("<Message>{0}</Message>", this.Message);
        }

        public void FromXml(string xml)
        {
            throw new NotImplementedException();
        }
    }
}