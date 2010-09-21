using System;
using System.Collections.Generic;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.Service;

namespace ServiceStack.Examples.Clients.Silverlight
{
	public class ExampleContext : ContextBase<AppContext>
	{
		public ExampleContext(IAsyncServiceClient serviceClient, AppContext appContext)
			: base(serviceClient, appContext)
		{
		}

		//Note: As 'Web Service' requests is a synchronous request it should always be run in a backround thread.
		public void GetFactorialAsync(long forNumber)
		{
			var request = new GetFactorial { ForNumber = forNumber };

			try
			{
				this.ServiceClient.Send<GetFactorialResponse>(request,
					response => OnDataLoaded(new DataEventArgs(response)));
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}
		}

		public void GetFibonacciNumbersAsync(int skip, int take)
		{
			base.Send<GetFibonacciNumbersResponse>(
				new GetFibonacciNumbers { Skip = skip, Take = take },
				x => new ResponseStatus(),
				response => OnDataLoaded(new DataEventArgs(response))
			);
		}

		public void StoreNewUserAsync(string userName, string password, string email)
		{
			var request = new StoreNewUser { UserName = userName, Password = password, Email = email };
			base.Send<StoreNewUserResponse>(
				request,
				x => x.ResponseStatus,
				response => OnDataLoaded(new DataEventArgs(response)),
				ex => OnDataLoaded(new DataEventArgs(new StoreNewUserResponse(), ex)));
		}


		public void DeleteAllUsersAsync()
		{
			base.Send<DeleteAllUsersResponse>(
				new DeleteAllUsers(),
				x => x.ResponseStatus,
				response => OnDataLoaded(new DataEventArgs(response)),
				ex => OnDataLoaded(new DataEventArgs(new DeleteAllUsersResponse(), ex)));
		}

		public void GetUsersAsync(List<long> userIds)
		{
			base.Send<GetUsersResponse>(
				new GetUsers { UserIds = new ArrayOfLong(userIds) },
				x => new ResponseStatus(),
				response => OnDataLoaded(new DataEventArgs(response))
			);
		}

	}
}