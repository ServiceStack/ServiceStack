using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Funq;

namespace ServiceStack.ServiceHost
{
	public abstract class FunqletBase
		: IFunqlet
	{
		protected IEnumerable<Type> ServiceTypes { get; set; }
		protected Container Container { get; set; }


		protected FunqletBase(IEnumerable<Type> serviceTypes)
		{
			this.ServiceTypes = serviceTypes;
		}

		protected FunqletBase(params Type[] serviceTypes)
			: this((IEnumerable<Type>)serviceTypes) {}

		public void Configure(Container container)
		{
			this.Container = container;
			Run();
		}

		protected abstract void Run();

	}
}