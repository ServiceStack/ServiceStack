using System;
using System.Collections.Generic;
using @DomainModelNamespace@;
using @DomainModelNamespace@.Validation.Attributes;

namespace @ServiceNamespace@.Logic.LogicInterface.Requests
{
	public class @ModelName@sRequest
	{
		public @ModelName@sRequest()
		{
			this.ValidateSession = true;
		}

		public int @ModelName@Id { get; set; }
		[NotNull]
		public SessionId SessionId { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether [validate session].
		/// 
		/// Allows Get@ModelName@s and Get@ModelName@sPublicProfile to call the same LogicCommand.
		/// </summary>
		/// <value><c>true</c> if [validate session]; otherwise, <c>false</c>.</value>
		public bool ValidateSession { get; set; }

		public List<int> @ModelName@Ids { get; set; }
		public List<Guid> GlobalIds { get; set; }
		public List<string> @ModelName@Names { get; set; }
	}
}