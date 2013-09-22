using System;

namespace ServiceStack.ServiceModel.Serialization
{
    public interface IStringDeserializer
    {
        To Parse<To>(string serializedText);
        object Parse(string serializedText, Type type);
    }
}