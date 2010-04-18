using System;
using System.Collections.Generic;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.ServiceHost;

namespace ServiceStack.Examples.ServiceInterface
{
	/// <summary>
	/// This is an example of a detailed web service to illustrate how its defined
	/// ServiceStack and to see what it looks like on the different endpoints.
	/// </summary>
	public class GetNorthwindCustomerOrdersService
		: IService<GetNorthwindCustomerOrders>
	{
		public object Execute(GetNorthwindCustomerOrders request)
		{
			return new GetNorthwindCustomerOrdersResponse
			{
				CustomerOrders = GetCustomerOrders(request.CustomerId)
			};			
		}

		/// <summary>
		/// For simplicity, this just returns a static result.
		/// </summary>
		/// <param name="customerId"></param>
		/// <returns></returns>
		public static CustomerOrders GetCustomerOrders(string customerId)
		{
			return new CustomerOrders
			{
				Customer = new Customer(
					customerId, "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57",
					"Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null),

				Orders = new List<Order> {
             		new Order
             			{
             				OrderHeader = new OrderHeader(
             					1, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
             					3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
             				OrderDetails = new List<OrderDetail>
		               		{
		               			new OrderDetail(1, 1, 10.00m, 1, 0),
		               			new OrderDetail(1, 2, 10.00m, 1, 0),
		               			new OrderDetail(1, 3, 10.00m, 1, 0),
		               		}
             			},
             		new Order
             			{
             				OrderHeader = new OrderHeader(
             					2, "VINET", 5, new DateTime(1997, 7, 4), new DateTime(1997, 1, 8), new DateTime(1997, 7, 16),
             					3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
             				OrderDetails = new List<OrderDetail>
		               		{
		               			new OrderDetail(2, 1, 10.00m, 1, 0),
		               			new OrderDetail(2, 2, 10.00m, 1, 0),
		               			new OrderDetail(2, 3, 10.00m, 1, 0),
		               		}
             			},
             		new Order
             			{
             				OrderHeader = new OrderHeader(
             					3, "VINET", 5, new DateTime(1998, 7, 4), new DateTime(1998, 1, 8), new DateTime(1998, 7, 16),
             					3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
             				OrderDetails = new List<OrderDetail>
		               		{
		               			new OrderDetail(3, 1, 10.00m, 1, 0),
		               			new OrderDetail(3, 2, 10.00m, 1, 0),
		               			new OrderDetail(3, 3, 10.00m, 1, 0),
		               		}
             			},
				 }
			};
		}
	}


}