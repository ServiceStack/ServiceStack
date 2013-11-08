using System;

namespace ServiceStack.Serialization
{
	public interface IStringSerializer
	{
        To DeserializeFromString<To>(string serializedText);
        object DeserializeFromString(string serializedText, Type type);
        string SerializeToString<TFrom>(TFrom from);
	}
}