using System;
using ServiceStack.Configuration;

namespace ServiceStack.ServiceInterface
{
	public class FactoryProviderHandlerFactory 
		: ITypeFactory
	{
		private readonly CreateFromLargestConstructorTypeFactory constructorTypeFactory;

		public FactoryProviderHandlerFactory(FactoryProvider factoryProvider)
		{
			this.constructorTypeFactory = new CreateFromLargestConstructorTypeFactory(factoryProvider);
		}

		public object CreateInstance(Type type)
		{
			return this.constructorTypeFactory.Create(type);
		}
	}

}