using System;

namespace ServiceStack.Serialization
{
    public interface IStringDeserializer
    {
        To Parse<To>(string serializedText);
        object Parse(string serializedText, Type type);
    }
}