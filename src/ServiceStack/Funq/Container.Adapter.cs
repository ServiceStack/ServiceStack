using ServiceStack.Configuration;
using ServiceStack.ServiceHost;

namespace Funq
{
	public partial class Container
	{
		public IContainerAdapter Adapter { get; set; }

		/// <summary>
		/// Register an autowired dependency
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public void RegisterAutoWired<T>()
		{
			var autoWired = new ExpressionTypeFunqContainer(this);
			autoWired.Register<T>();
		}

		/// <summary>
		/// Register an autowired dependency as a separate type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public void RegisterAutoWiredAs<T, TAs>()
			where T : TAs
		{
			var autoWired = new ExpressionTypeFunqContainer(this);
			autoWired.RegisterAs<T, TAs>();
		}

		/// <summary>
		/// Alias for RegisterAutoWiredAs
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public void RegisterAs<T, TAs>()
			where T : TAs
		{
			var autoWired = new ExpressionTypeFunqContainer(this);
			autoWired.RegisterAs<T, TAs>();
		}
	}

}