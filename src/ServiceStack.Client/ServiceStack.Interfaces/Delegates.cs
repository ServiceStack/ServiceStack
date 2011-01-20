using System;
using System.IO;

namespace ServiceStack.ServiceHost
{
	public delegate string TextSerializerDelegate(object dto);

	public delegate void StreamSerializerDelegate(object dto, Stream toStream);

	public delegate object TextDeserializerDelegate(Type type, string dto);

	public delegate object StreamDeserializerDelegate(Type type, Stream fromStream);
}