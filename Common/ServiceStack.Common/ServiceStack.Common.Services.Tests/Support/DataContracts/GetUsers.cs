using System.Runtime.Serialization;

namespace ServiceStack.Common.Services.Tests.Support.DataContracts
{
	[DataContract(Namespace = "http://servicestack.net/types/")]
    public class GetUsers
    {
        public GetUsers()
        {
            this.Version = 100;
            this.Ids = new ArrayOfIntId();
        }

        [DataMember]
        public int Version { get; set; }
        [DataMember]
        public ArrayOfIntId Ids { get; set; }
    }
}