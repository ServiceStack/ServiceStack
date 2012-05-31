using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Funq;
using ServiceStack.ServiceHost;

namespace ServiceStack.Mvc
{
	public class FunqControllerFactory : DefaultControllerFactory
	{
		private readonly ContainerResolveCache funqBuilder;

		public FunqControllerFactory(Container container)
		{
			this.funqBuilder = new ContainerResolveCache(container);

			// Also register all the controller types as transient
			var controllerTypes =
                (from type in Assembly.GetCallingAssembly().GetTypes()
				 where typeof(IController).IsAssignableFrom(type)
				 select type).ToList();

			container.RegisterAutoWiredTypes(controllerTypes);
		}

		protected override IController GetControllerInstance(
			RequestContext requestContext, Type controllerType)
		{
			try
			{
				if (controllerType == null)
					return base.GetControllerInstance(requestContext, null);

				var controller = funqBuilder.CreateInstance(controllerType) as IController;

				return controller ?? base.GetControllerInstance(requestContext, controllerType);
			}
			catch (HttpException ex)
			{
				if (ex.GetHttpCode() == 404)
				{
					try
					{
						if (ServiceStackController.CatchAllController != null)
						{
							return ServiceStackController.CatchAllController(requestContext);
						}
					}
					catch { } //ignore not found CatchAllController
				}
				throw;
			}
		}
	}
}