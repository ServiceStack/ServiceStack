using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.Common.Support;
using ServiceStack.DataAccess;
using ServiceStack.DesignPatterns.Command;
using ServiceStack.Logging;
using ServiceStack.LogicFacade;
using ServiceStack.SakilaDb4o.Logic.LogicCommands;
using ServiceStack.SakilaDb4o.Logic.LogicInterface;
using ServiceStack.SakilaDb4o.Logic.LogicInterface.Requests;

namespace ServiceStack.SakilaDb4o.Logic
{
	public class SakilaDb4oServiceFacade : LogicFacadeBase, ISakilaDb4oServiceFacade
	{
		private readonly ILog log = LogManager.GetLogger(typeof(SakilaDb4oServiceFacade));

		private IOperationContext AppContext { get; set; }

		private IPersistenceProvider persistenceProvider;
		private IPersistenceProvider PersistenceProvider
		{
			get
			{
				if (this.persistenceProvider == null)
				{
					this.persistenceProvider = this.AppContext.Factory.Resolve<IPersistenceProviderManager>().CreateProvider();
				}
				return this.persistenceProvider;
			}
		}

		public SakilaDb4oServiceFacade(IOperationContext appContext)
		{
			this.AppContext = appContext;
		}

		public IList<Customer> GetAllCustomers()
		{
			return Execute(new GetAllCustomersLogicCommand());
		}

		public IList<Customer> GetCustomers(CustomersRequest request)
		{
			return Execute(new GetCustomersLogicCommand { Request = request });
		}

		public void StoreCustomer(Customer entity)
		{
			Execute(new StoreCustomerLogicCommand { Customer = entity });
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
			action.Provider = this.PersistenceProvider;
		}
	}
}
