using System;

namespace ServiceStack.Text
{
    public interface IStringSerializer
    {
        To DeserializeFromString<To>(string serializedText);
        object DeserializeFromString(string serializedText, Type type);
        string SerializeToString<TFrom>(TFrom from);
    }
}