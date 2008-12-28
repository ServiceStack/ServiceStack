using System;
using System.Collections.Generic;

namespace @ServiceNamespace@.LogicInterface.Requests
{
	public class Get@ModelName@sRequest
	{
		public List<int> @ModelName@Ids { get; set; }
		public List<Guid> GlobalIds { get; set; }
		public List<string> @ModelName@Names { get; set; }
	}
}