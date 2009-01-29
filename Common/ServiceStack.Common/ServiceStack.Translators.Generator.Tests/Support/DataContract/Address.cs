namespace ServiceStack.Translators.Generator.Tests.Support.DataContract
{
	[Translate(typeof(Model.Address))]
	public partial class Address
	{
		public string Line1 { get; set; }
		public string Line2 { get; set; }
	}
}