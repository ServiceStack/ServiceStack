namespace ServiceStack.DataAccess.Criteria
{
	public class PagingCriteria : IPagingCriteria
	{
		public uint ResultOffset { get; private set; }
		public uint ResultLimit { get; private set; }

		public PagingCriteria(uint resultOffset, uint resultLimit)
		{
			this.ResultOffset = resultOffset;
			this.ResultLimit = resultLimit;
		}
	}
}