namespace ServiceStack.Translators.Generator.Tests.Support.DataContract
{
	[TranslateModel(typeof(Model.Customer))]
	public partial class Customer
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public Address BillingAddress { get; set; }
	}
}