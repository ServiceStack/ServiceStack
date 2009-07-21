using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ServiceStack.Examples.ServiceInterface.Types;

namespace ServiceStack.Examples.Clients.Silverlight
{
	public partial class MainPage : UserControl
	{
		public MainPage()
		{
			InitializeComponent();

			App.ExampleContext.DataLoaded += ExampleContext_DataLoaded;
		}

		void ExampleContext_DataLoaded(object sender, DataEventArgs e)
		{
			if (!e.IsSuccess)
			{
				base.Dispatcher.BeginInvoke(() => MessageBox.Show("Received Error: " + e.Exception.Message));
				return;
			}

			var factorialResponse = e.Data as GetFactorialResponse;
			if (factorialResponse != null)
			{
				base.Dispatcher.BeginInvoke(delegate {
					txtGetFactorialResult.Text = factorialResponse.Result.ToString();
				});
			}

			var fibResponse = e.Data as GetFibonacciNumbersResponse;
			if (fibResponse != null)
			{
				base.Dispatcher.BeginInvoke(delegate {
					itemsGetFibonacciNumbersResult.ItemsSource = fibResponse.Results;
				});
			}

			var newUserResponse = e.Data as StoreNewUserResponse;
			if (newUserResponse != null)
			{
				base.Dispatcher.BeginInvoke(delegate {
					txtStoreNewUserResult.Text = newUserResponse.UserId.ToString();

					if (!string.IsNullOrEmpty(txtUserIds.Text))
					{
						txtUserIds.Text += ",";
					}
					txtUserIds.Text += newUserResponse.UserId.ToString();
				});
			}

			var deleteResponse = e.Data as DeleteAllUsersResponse;
			if (deleteResponse != null)
			{
				base.Dispatcher.BeginInvoke(delegate {
					MessageBox.Show("All users were deleted");
					txtUserIds.Text = "";
				});
			}

			var usersResponse = e.Data as GetUsersResponse;
			if (usersResponse != null)
			{
				base.Dispatcher.BeginInvoke(delegate {
					itemsGetUsersResult.ItemsSource = usersResponse.Users;
				});
			}

		}

		private void btnGetFactorial_Click(object sender, RoutedEventArgs e)
		{
			App.ExampleContext.GetFactorialAsync(Convert.ToInt64(txtGetFactorial.Text));
		}

		private void btnGetFibonacciNumbers_Click(object sender, RoutedEventArgs e)
		{
			App.ExampleContext.GetFibonacciNumbersAsync(
				Convert.ToInt32(txtGetFibonacciNumbersSkip.Text), Convert.ToInt32(txtGetFibonacciNumbersTake.Text));
		}

		private void btnStoreNewUser_Click(object sender, RoutedEventArgs e)
		{
			App.ExampleContext.StoreNewUserAsync(txtUserName.Text, txtPassword.Text, txtEmail.Text);
		}

		private void btnDeleteAllUsers_Click(object sender, RoutedEventArgs e)
		{
			App.ExampleContext.DeleteAllUsersAsync();
		}

		private void btnGetUsers_Click(object sender, RoutedEventArgs e)
		{
			var userIds = new List<long>();
			foreach (var userId in txtUserIds.Text.Split(','))
			{
				userIds.Add(Convert.ToInt64(userId));
			}

			if (userIds.Count > 0)
			{
				App.ExampleContext.GetUsersAsync(userIds);
			}
		}
	}
}
