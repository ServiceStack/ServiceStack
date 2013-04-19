using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Funq;
using ServiceStack.ServiceHost;
using System.Collections.Generic;

namespace ServiceStack.Mvc
{
    public class FunqControllerFactory : DefaultControllerFactory
	{
		private readonly ContainerResolveCache funqBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="FunqControllerFactory" /> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="assemblies">The assemblies to reflect for IController discovery.</param>
        public FunqControllerFactory(Container container, params Assembly[] assemblies)
		{
			this.funqBuilder = new ContainerResolveCache(container);

            // aggregate the local and external assemblies for processing (unless ignored)
            IEnumerable<Assembly> targetAssemblies = assemblies.Concat(new[] { Assembly.GetCallingAssembly() });

            foreach (var assembly in targetAssemblies)
            {
                // Also register all the controller types as transient
                var controllerTypes =
                    (from type in assembly.GetTypes()
                     where typeof(IController).IsAssignableFrom(type)
                     select type).ToList();

                container.RegisterAutoWiredTypes(controllerTypes);
            }
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