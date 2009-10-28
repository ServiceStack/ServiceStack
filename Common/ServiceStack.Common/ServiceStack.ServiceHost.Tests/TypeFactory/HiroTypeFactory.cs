using System;
using System.Collections.Generic;
using Hiro.Containers;
using ServiceStack.Configuration;

namespace ServiceStack.ServiceHost.Tests.TypeFactory
{
	public class HiroTypeFactory
		: ITypeFactory
	{
		private readonly IMicroContainer container;

		public HiroTypeFactory(IMicroContainer container)
		{
			this.container = container;
		}

		public object CreateInstance(Type type)
		{
			return container.GetInstance(type, null);
		}
	}
}