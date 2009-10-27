using System;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.ServiceHost.Tests.UseCase.Services;

namespace ServiceStack.ServiceHost.Tests.TypeFactory
{
	public class FuncTypeFactory 
		: ITypeFactory
	{
		private readonly Container container;

		public FuncTypeFactory(Container container)
		{
			this.container = container;
		}

		public object CreateInstance(Type type)
		{
			if (type == typeof(GetCustomerService))
				return this.container.Resolve<GetCustomerService>();

			if (type == typeof(StoreCustomersService))
				return this.container.Resolve<StoreCustomersService>();

			throw new NotSupportedException(type.Name);
		}
	}
}