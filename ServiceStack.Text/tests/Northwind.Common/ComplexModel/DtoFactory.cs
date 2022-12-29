using System;
using System.Collections.Generic;
using Northwind.Common.ServiceModel;

namespace Northwind.Common.ComplexModel
{
	public class DtoFactory
	{
		public static CustomerDto CustomerDto
		{
			get
			{
				return NorthwindDtoFactory.Customer(
					1.ToString("x"), "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57",
					"Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null);
			}
		}

		public static SupplierDto SupplierDto
		{
			get
			{
				return NorthwindDtoFactory.Supplier(
					1, "Exotic Liquids", "Charlotte Cooper", "Purchasing Manager", "49 Gilbert St.", "London", null,
					"EC1 4SD", "UK", "(171) 555-2222", null, null);
			}
		}

		public static OrderDto OrderDto
		{
			get
			{
				return NorthwindDtoFactory.Order(
					1, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
					3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France");
			}
		}

		public static MultiOrderProperties MultiOrderProperties
		{
			get
			{
				return new MultiOrderProperties {
					Orders1 = NorthwindDtoFactory.Order(
						1, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
						3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
					Orders2 = NorthwindDtoFactory.Order(
						2, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
						3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
					Orders3 = NorthwindDtoFactory.Order(
						3, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
						3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
					Orders4 = NorthwindDtoFactory.Order(
						4, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
						3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
				};
			}
		}

		public static MultiCustomerProperties MultiCustomerProperties
		{
			get
			{
				return new MultiCustomerProperties {
					Customer1 = NorthwindDtoFactory.Customer(
						1.ToString("x"), "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57",
						"Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null),
					Customer2 = NorthwindDtoFactory.Customer(
						1.ToString("x"), "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57",
						"Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null),
					Customer3 = NorthwindDtoFactory.Customer(
						1.ToString("x"), "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57",
						"Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null),
				};
			}
		}

		public static MultiDtoWithOrders MultiDtoWithOrders
		{
			get
			{
				return new MultiDtoWithOrders {
					Id = Guid.NewGuid(),
					Customer = NorthwindDtoFactory.Customer(
						1.ToString("x"), "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57",
						"Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null),
					Supplier = NorthwindDtoFactory.Supplier(
						1, "Exotic Liquids", "Charlotte Cooper", "Purchasing Manager", "49 Gilbert St.", "London", null,
						"EC1 4SD", "UK", "(171) 555-2222", null, null),
					Orders = new List<OrderDto> {
                    	NorthwindDtoFactory.Order(
                    		1, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
                    		3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
                    	NorthwindDtoFactory.Order(
                    		2, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
                    		3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
                    	NorthwindDtoFactory.Order(
                    		3, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
                    		3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
                    	NorthwindDtoFactory.Order(
                    		4, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
                    		3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
                    },
				};
			}
		}

		public static ArrayDtoWithOrders ArrayDtoWithOrders
		{
			get
			{
				return new ArrayDtoWithOrders {
					Id = Guid.NewGuid(),
					Customer = NorthwindDtoFactory.Customer(
						1.ToString("x"), "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57",
						"Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null),
					Supplier = NorthwindDtoFactory.Supplier(
						1, "Exotic Liquids", "Charlotte Cooper", "Purchasing Manager", "49 Gilbert St.", "London", null,
						"EC1 4SD", "UK", "(171) 555-2222", null, null),
					Orders = new[] {
  	               			NorthwindDtoFactory.Order(
  	               				1, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
  	               				3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
  	               			NorthwindDtoFactory.Order(
  	               				2, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
  	               				3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
  	               			NorthwindDtoFactory.Order(
  	               				3, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
  	               				3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
  	               			NorthwindDtoFactory.Order(
  	               				4, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
  	               				3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
  					   },
				};
			}
		}

		public static CustomerOrderListDto CustomerOrderListDto
		{
			get
			{
				return new CustomerOrderListDto {
					Customer = NorthwindDtoFactory.Customer(
						1.ToString("x"), "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57",
						"Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null),
					Orders = new List<FullOrderDto> {
    					new FullOrderDto
    					{
    						Order = NorthwindDtoFactory.Order(
    							1, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
    							3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
    						OrderDetails = new List<OrderDetailDto>
							   {
               						NorthwindDtoFactory.OrderDetail(1, 1, 10.00m, 1, 0),
               						NorthwindDtoFactory.OrderDetail(1, 2, 10.00m, 1, 0),
               						NorthwindDtoFactory.OrderDetail(1, 3, 10.00m, 1, 0),
							   }
    					},
    					new FullOrderDto
    					{
    						Order = NorthwindDtoFactory.Order(
    							2, "VINET", 5, new DateTime(1997, 7, 4), new DateTime(1997, 1, 8), new DateTime(1997, 7, 16),
    							3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
    						OrderDetails = new List<OrderDetailDto>
							   {
               						NorthwindDtoFactory.OrderDetail(2, 1, 10.00m, 1, 0),
               						NorthwindDtoFactory.OrderDetail(2, 2, 10.00m, 1, 0),
               						NorthwindDtoFactory.OrderDetail(2, 3, 10.00m, 1, 0),
							   }
    					},
    					new FullOrderDto
    					{
    						Order = NorthwindDtoFactory.Order(
    							3, "VINET", 5, new DateTime(1998, 7, 4), new DateTime(1998, 1, 8), new DateTime(1998, 7, 16),
    							3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
    						OrderDetails = new List<OrderDetailDto>
							   {
               						NorthwindDtoFactory.OrderDetail(3, 1, 10.00m, 1, 0),
               						NorthwindDtoFactory.OrderDetail(3, 2, 10.00m, 1, 0),
               						NorthwindDtoFactory.OrderDetail(3, 3, 10.00m, 1, 0),
							   }
    					},
					}
				};
			}
		}


		public static CustomerOrderArrayDto CustomerOrderArrayDto
		{
			get
			{
				return new CustomerOrderArrayDto {
					Customer = NorthwindDtoFactory.Customer(
						1.ToString("x"), "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57",
						"Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null),
					Orders = new[] {
 	                	new FullOrderDto
 	                	{
 	                		Order = NorthwindDtoFactory.Order(
 	                			1, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16),
 	                			3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
 	                		OrderDetails = new List<OrderDetailDto>
        		               {
        		               		NorthwindDtoFactory.OrderDetail(1, 1, 10.00m, 1, 0),
        		               		NorthwindDtoFactory.OrderDetail(1, 2, 10.00m, 1, 0),
        		               		NorthwindDtoFactory.OrderDetail(1, 3, 10.00m, 1, 0),
        		               }
 	                	},
 	                	new FullOrderDto
 	                	{
 	                		Order = NorthwindDtoFactory.Order(
 	                			2, "VINET", 5, new DateTime(1997, 7, 4), new DateTime(1997, 1, 8), new DateTime(1997, 7, 16),
 	                			3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
 	                		OrderDetails = new List<OrderDetailDto>
        		               {
        		               		NorthwindDtoFactory.OrderDetail(2, 1, 10.00m, 1, 0),
        		               		NorthwindDtoFactory.OrderDetail(2, 2, 10.00m, 1, 0),
        		               		NorthwindDtoFactory.OrderDetail(2, 3, 10.00m, 1, 0),
        		               }
 	                	},
 	                	new FullOrderDto
 	                	{
 	                		Order = NorthwindDtoFactory.Order(
 	                			3, "VINET", 5, new DateTime(1998, 7, 4), new DateTime(1998, 1, 8), new DateTime(1998, 7, 16),
 	                			3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"),
 	                		OrderDetails = new List<OrderDetailDto>
        		               {
        		               		NorthwindDtoFactory.OrderDetail(3, 1, 10.00m, 1, 0),
        		               		NorthwindDtoFactory.OrderDetail(3, 2, 10.00m, 1, 0),
        		               		NorthwindDtoFactory.OrderDetail(3, 3, 10.00m, 1, 0),
        		               }
 	                	},
 	                }
				};
			}
		}

	}
}