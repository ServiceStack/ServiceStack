using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.Common.Support;
using ServiceStack.DataAccess;
using ServiceStack.ServiceInterface;
using ServiceStack.DesignPatterns.Command;
using ServiceStack.Logging;
using ServiceStack.Sakila.DataAccess;
using ServiceStack.Sakila.Logic.LogicCommands;
using ServiceStack.Sakila.Logic.LogicInterface;
using ServiceStack.Sakila.Logic.LogicInterface.Requests;

namespace ServiceStack.Sakila.Logic
{
	public class SakilaServiceFacade : LogicFacadeBase, ISakilaServiceFacade
	{
		private readonly ILog log = LogManager.GetLogger(typeof(SakilaServiceFacade));

		private AppContext AppContext { get; set; }

		private IPersistenceProvider PersistenceProvider { get; set; }

		private SakilaServiceDataAccessProvider Provider { get; set; }

		public SakilaServiceFacade(AppContext appContext, IPersistenceProviderManager providerManager)
		{
			this.AppContext = appContext;

			// Create new connection
			this.PersistenceProvider = providerManager.CreateProvider();

			// Wrap connection in Data Access Provider
			this.Provider = new SakilaServiceDataAccessProvider(PersistenceProvider);
		}

		public List<Customer> GetCustomers(CustomersRequest request)
		{
			return Execute(new GetCustomersLogicCommand {
				Request = request
			});
		}

		public void StoreCustomer(Customer customer)
		{
			Execute(new StoreCustomersLogicCommand {
				Customer = customer,
			});
		}

		public override void Dispose()
		{
			// Close the connection
			this.PersistenceProvider.Dispose();
		}

		protected override void Init<T>(ICommand<T> command)
		{
			var action = (IAction<T>)command;
			action.AppContext = this.AppContext;
			action.Provider = this.Provider;
		}
	}
}
