using System;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;
using Funq;
using ServiceStack.ServiceHost;

namespace $rootnamespace$
{
	//Add the line below in Application_Start() if you want MVC and ServiceStack to share the same IOC
	//ControllerBuilder.Current.SetControllerFactory(new FunqControllerFactory(container));

	/// <summary>
	/// Get MVC and ServiceStack to share the same Funq IOC
	/// </summary>
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
                from type in Assembly.GetExecutingAssembly().GetTypes()
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