using System;
using System.IO;

namespace ServiceStack.DesignPatterns.Serialization
{
	public interface ITextSerializer
	{
		object DeserializeFromString(string json, Type returnType);
		T DeserializeFromString<T>(string json);
		T DeserializeFromStream<T>(Stream stream);
		object DeserializeFromStream(Type type, Stream stream);

		string SerializeToString<T>(T obj);
		void SerializeToStream<T>(T obj, Stream stream);
	}
}