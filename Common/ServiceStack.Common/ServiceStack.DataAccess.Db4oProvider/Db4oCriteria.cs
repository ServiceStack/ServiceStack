using ServiceStack.DataAccess.Db4oProvider.Criteria;

namespace ServiceStack.DataAccess.Db4oProvider
{
	public class Db4oCriteria : IOrderAscendingCriteria, IOrderDescendingCriteria, IPagingCriteria
	{
		public string OrderedAscendingBy { get; set; }
		public string OrderedDescendingBy { get; set; }
		public int ResultOffset { get; set; }
		public int ResultLimit { get; set; }
	}
}