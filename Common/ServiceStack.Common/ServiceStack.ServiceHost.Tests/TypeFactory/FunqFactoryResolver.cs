using System;
using Funq;
using ServiceStack.Configuration;

namespace ServiceStack.ServiceHost.Tests.TypeFactory
{
	public class FunqFactoryResolver
		: IFactoryResolver
	{
		private readonly Container container;

		public FunqFactoryResolver(Container container)
		{
			this.container = container;
		}

		public T Resolve<T>()
		{
			return this.container.TryResolve<T>();
		}
	}
}