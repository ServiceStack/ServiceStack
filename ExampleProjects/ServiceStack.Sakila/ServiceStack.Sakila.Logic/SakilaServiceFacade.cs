using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.Common.Support;
using ServiceStack.DataAccess;
using ServiceStack.DesignPatterns.Command;
using ServiceStack.Logging;
using ServiceStack.LogicFacade;
using ServiceStack.Sakila.DataAccess;
using ServiceStack.Sakila.Logic.LogicCommands;
using ServiceStack.Sakila.Logic.LogicInterface;
using ServiceStack.Sakila.Logic.LogicInterface.Requests;

namespace ServiceStack.Sakila.Logic
{
	public class SakilaServiceFacade : LogicFacadeBase, ISakilaServiceFacade
	{
		private readonly ILog log = LogManager.GetLogger(typeof(SakilaServiceFacade));

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

		private SakilaServiceDataAccessProvider Provider { get; set; }

		public SakilaServiceFacade(IOperationContext appContext)
		{
			this.AppContext = appContext;

			// Wrap connection in Data Access Provider
			this.Provider = new SakilaServiceDataAccessProvider(PersistenceProvider);
		}

		public List<Customer> GetAllCustomers()
		{
			return Execute(new GetAllCustomersLogicCommand());
		}

		public List<Customer> GetCustomers(CustomersRequest request)
		{
			return Execute(new GetCustomersLogicCommand { Request = request });
		}

		public List<Film> GetFilms(List<int> filmIds)
		{
			return Execute(new GetFilmsLogicCommand { FilmIds = filmIds });
		}

		public void StoreCustomer(Customer customer)
		{
			Execute(new StoreCustomersLogicCommand { Customer = customer, });
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
