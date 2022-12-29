using System;

namespace ServiceStack.Common.Tests.Models{
	
	public class ModelWithIdOnly
	{
		public long Id
		{
			get;
			set;
		}
	
		public ModelWithIdOnly()
		{
		}
	
		public ModelWithIdOnly(long id)
		{
			this.Id = id;
		}
	}
}