using System;
using System.Collections.Generic;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.Service;

namespace ServiceStack.Examples.Clients.Silverlight
{
	public class ExampleContext : ContextBase<AppContext>
	{
		public ExampleContext(IServiceClient serviceClient, AppContext appContext)
			: base(serviceClient, appContext)
		{
		}

		//Note: As 'Web Service' requests is a synchronous request it should always be run in a backround thread.
		public void GetFactorialAsync(long forNumber)
		{
			InvokeAsync(() => GetFactorial(forNumber));
		}

		protected void GetFactorial(long forNumber)
		{
			var request = new GetFactorial { ForNumber = forNumber };

			var response = this.ServiceClient.Send<GetFactorialResponse>(request);

			OnDataLoaded(new DataEventArgs(response));
		}


		public void GetFibonacciNumbersAsync(int skip, int take)
		{
			InvokeAsync(() => GetFibonacciNumbers(skip, take));
		}

		protected void GetFibonacciNumbers(int skip, int take)
		{
			var response = base.Send<GetFibonacciNumbersResponse>(
				new GetFibonacciNumbers { Skip = skip, Take = take },
				x => new ResponseStatus()
			);

			OnDataLoaded(new DataEventArgs(response));
		}


		public void StoreNewUserAsync(string userName, string password, string email)
		{
			InvokeAsync(() => StoreNewUser(userName, password, email));
		}

		protected void StoreNewUser(string userName, string password, string email)
		{
			try
			{
				var request = new StoreNewUser { UserName = userName, Password = password, Email = email };
				var response = base.Send<StoreNewUserResponse>(request, x => x.ResponseStatus);

				OnDataLoaded(new DataEventArgs(response));
			}
			catch (Exception ex)
			{
				OnDataLoaded(new DataEventArgs(new StoreNewUserResponse(), ex));
			}
		}


		public void DeleteAllUsersAsync()
		{
			InvokeAsync(DeleteAllUsers);
		}

		protected void DeleteAllUsers()
		{
			try
			{
				var response = base.Send<DeleteAllUsersResponse>(new DeleteAllUsers(), x => x.ResponseStatus);

				OnDataLoaded(new DataEventArgs(response));
			}
			catch (Exception ex)
			{
				OnDataLoaded(new DataEventArgs(new DeleteAllUsersResponse(), ex));
			}
		}


		public void GetUsersAsync(List<long> userIds)
		{
			InvokeAsync(() => GetUsers(userIds));
		}

		protected void GetUsers(List<long> userIds)
		{
			var response = base.Send<GetUsersResponse>(
				new GetUsers { UserIds = new ArrayOfLong(userIds) },
				x => new ResponseStatus()
			);

			OnDataLoaded(new DataEventArgs(response));
		}

	}
}