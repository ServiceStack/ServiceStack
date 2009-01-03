using System;
using System.Collections.Generic;

namespace ServiceStack.SakilaNHibernate.Logic.LogicInterface.Requests
{
	public class CustomersRequest
	{
		public List<int> CustomerIds { get; set; }
	}
}