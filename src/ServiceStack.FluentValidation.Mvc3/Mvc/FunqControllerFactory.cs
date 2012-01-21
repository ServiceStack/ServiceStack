using System;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;
using Funq;
using ServiceStack.ServiceHost;

namespace ServiceStack.Mvc
{
	public class FunqControllerFactory : DefaultControllerFactory
	{
		private readonly AutoWireContainer funqBuilder;

		public FunqControllerFactory(Container container)
		{
			this.funqBuilder = new AutoWireContainer(container) {
				Scope = ReuseScope.None //don't re-use instances
			};

			// Also register all the controller types as transient
			var controllerTypes =
                from type in Assembly.GetCallingAssembly().GetTypes()
				where typeof(IController).IsAssignableFrom(type)
				select type;

			funqBuilder.RegisterTypes(controllerTypes);
		}

		protected override IController GetControllerInstance(
			RequestContext requestContext, Type controllerType)
		{
			if (controllerType == null)
				return base.GetControllerInstance(requestContext, null);

			var controller = funqBuilder.CreateInstance(controllerType) as IController;
			return controller ?? base.GetControllerInstance(requestContext, controllerType);
		}
	}
}