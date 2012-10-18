using System;

namespace ServiceStack.Configuration
{
	public interface ITypeFactory
	{
		object CreateInstance(Type type);
	}
}