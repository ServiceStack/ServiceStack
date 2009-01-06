using System.Collections.Generic;
using @DomainModelNamespace@;
using ServiceStack.Common.Support;
using ServiceStack.DataAccess;
using ServiceStack.DesignPatterns.Command;
using ServiceStack.Logging;
using ServiceStack.LogicFacade;
using @ServiceNamespace@.Logic.LogicCommands;
using @ServiceNamespace@.Logic.LogicInterface;
using @ServiceNamespace@.Logic.LogicInterface.Requests;

namespace @ServiceNamespace@.Logic
{
	public class @ServiceName@Facade : LogicFacadeBase, I@ServiceName@Facade
	{
		private readonly ILog log = LogManager.GetLogger(typeof(@ServiceName@Facade));

		private IApplicationContext AppContext { get; set; }

		private IPersistenceProvider provider;
		private IPersistenceProvider Provider
		{
			get
			{
				if (this.provider == null)
				{
					this.provider = this.AppContext.Factory.Resolve<IPersistenceProviderManager>().CreateProvider();
				}
				return this.provider;
			}
		}

		public @ServiceName@Facade(IApplicationContext appContext)
		{
			this.AppContext = appContext;
		}

		public IList<@ModelName@> GetAll@ModelName@s()
		{
			return Execute(new GetAll@ModelName@sLogicCommand());
		}

		public IList<@ModelName@> Get@ModelName@s(@ModelName@sRequest request)
		{
			return Execute(new Get@ModelName@sLogicCommand { Request = request });
		}

		public void Store@ModelName@(@ModelName@ entity)
		{
			Execute(new Store@ModelName@LogicCommand { @ModelName@ = entity });
		}

		public override void Dispose()
		{
			this.Provider.Dispose();
		}

		protected override void Init<T>(ICommand<T> command)
		{
			var action = (IAction<T>)command;
			action.AppContext = this.AppContext;
			action.Provider = this.Provider;
		}
	}
}
