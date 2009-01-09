using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.LogicFacade;

namespace @ServiceNamespace@.ServiceInterface
{
	public class @DatabaseName@OperationContext : IOperationContext
	{
		public @DatabaseName@OperationContext(IApplicationContext application, IRequestContext request)
		{
			this.Application = application;
			this.Request = request;
		}

		public IApplicationContext Application { get; private set; }
		public IRequestContext Request  { get; private set; }

		public void Dispose()
		{
			if (this.Request != null)
			{
				this.Request.Dispose();
			}
		}

		IApplicationContext IOperationContext.Application { get { return this.Application; } }

		private IPersistenceProvider provider;
		public IPersistenceProvider Provider
		{
			get
			{
				if (this.provider == null)
				{
					this.provider = this.Application.Factory.Resolve<IPersistenceProvider>();
				}
				return this.provider;
			}
		}

		private IResourceManager resources;
		public IResourceManager Resources
		{
			get
			{
				if (this.resources == null)
				{
					this.resources = this.Application.Factory.Resolve<IResourceManager>();
				}
				return this.resources;
			}
		}
	}
}