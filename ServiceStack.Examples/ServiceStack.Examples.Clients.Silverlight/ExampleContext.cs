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


		public void GetFactorialAsync(long forNumber)
		{
			InvokeAsync(() => GetFactorial(forNumber));
		}

		public void GetFactorial(long forNumber)
		{
			var response = base.Send<GetFactorialResponse>(
				new GetFactorial { ForNumber = forNumber }, x => new ResponseStatus());

			OnDataLoaded(new DataEventArgs(response));
		}


		public void GetFibonacciNumbersAsync(int skip, int take)
		{
			InvokeAsync(() => GetFibonacciNumbers(skip, take));
		}

		public void GetFibonacciNumbers(int skip, int take)
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

		public void StoreNewUser(string userName, string password, string email)
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

		public void DeleteAllUsers()
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

		public void GetUsers(List<long> userIds)
		{
			var response = base.Send<GetUsersResponse>(
				new GetUsers { UserIds = new ArrayOfLong(userIds) },
				x => new ResponseStatus()
			);

			OnDataLoaded(new DataEventArgs(response));
		}

	}
}