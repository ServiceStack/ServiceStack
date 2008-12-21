using System;

namespace ServiceStack.Messaging
{
    public interface IXmlSerializable 
    {
        string ToXml();
        void FromXml(string xml);
    }
}