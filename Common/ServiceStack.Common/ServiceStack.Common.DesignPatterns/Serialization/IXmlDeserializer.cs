using System;

namespace ServiceStack.Common.DesignPatterns.Serialization
{
    public interface IXmlDeserializer
    {
        To Parse<To>(string xml);
        object Parse(string xml, Type type);
    }
}