using ServiceStack.SakilaDb4o.DataAccess.Base;

namespace ServiceStack.SakilaDb4o.DataAccess.DataModel
{
	public partial class Customer : BusinessBase<int>
	{

		public override int GetHashCode()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			sb.Append(this.GetType().FullName);

			return sb.ToString().GetHashCode();
		}
	}
}