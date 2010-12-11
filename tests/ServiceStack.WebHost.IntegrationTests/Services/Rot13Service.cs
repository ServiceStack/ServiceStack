using System.Runtime.Serialization;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[DataContract]
	public class Rot13
	{
		[DataMember]
		public string Value { get; set; }
	}

	[DataContract]
	public class Rot13Response
	{
		[DataMember]
		public string Result { get; set; }
	}

	public class Rot13Service 
		: ServiceBase<Rot13>
	{
		protected override object Run(Rot13 request)
		{
			return new Rot13Response { Result = request.Value.ToRot13() };
		}
	}
}