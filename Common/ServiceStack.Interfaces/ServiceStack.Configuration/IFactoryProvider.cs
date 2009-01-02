using System;

namespace ServiceStack.Configuration
{
	public interface IFactoryProvider : IDisposable
	{
		void Register<T>(T provider);

		T Resolve<T>();

		T Resolve<T>(string name);

		T ResolveOptional<T>(string name, T defaultValue);

		T Create<T>(string name);
	}
}