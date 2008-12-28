using System;
using System.Collections.Generic;

namespace ServiceStack.Sakila.Logic.LogicInterface.Requests
{
	public class CustomersRequest
	{
		public List<int> CustomerIds { get; set; }
	}
}