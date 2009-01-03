using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.Common.Support;
using ServiceStack.DataAccess;
using ServiceStack.DesignPatterns.Command;
using ServiceStack.Logging;
using ServiceStack.LogicFacade;
using ServiceStack.SakilaNHibernate.DataAccess;
using ServiceStack.SakilaNHibernate.Logic.LogicCommands;
using ServiceStack.SakilaNHibernate.Logic.LogicInterface;
using ServiceStack.SakilaNHibernate.Logic.LogicInterface.Requests;

namespace ServiceStack.SakilaNHibernate.Logic
{
	public class SakilaNHibernateServiceFacade : LogicFacadeBase, ISakilaNHibernateServiceFacade
	{
		private readonly ILog log = LogManager.GetLogger(typeof(SakilaNHibernateServiceFacade));

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

		private SakilaNHibernateServiceDataAccessProvider Provider { get; set; }

		public SakilaNHibernateServiceFacade(IOperationContext appContext)
		{
			this.AppContext = appContext;
			this.Provider = new SakilaNHibernateServiceDataAccessProvider(PersistenceProvider);
		}

		public List<Customer> GetAllCustomers()
		{
			return Execute(new GetAllCustomersLogicCommand());
		}

		public List<Customer> GetCustomers(CustomersRequest request)
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
			action.Provider = this.Provider;
		}
	}
}
