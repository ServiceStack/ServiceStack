using ServiceStack.SakilaDb4oDemo.DataAccess.Base;

namespace ServiceStack.SakilaDb4oDemo.DataAccess.DataModel
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