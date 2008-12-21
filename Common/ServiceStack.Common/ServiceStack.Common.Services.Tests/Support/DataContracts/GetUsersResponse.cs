using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Common.Services.Tests.Support.DataContracts
{
	[DataContract(Namespace = "http://servicestack.net/types/")]
    public class GetUsersResponse
    {
        public GetUsersResponse()
        {
            this.Version = 100;
            this.Users = new List<User>();
        }

        [DataMember]
        public int Version { get; set; }
        [DataMember]
        public List<User> Users { get; set; }
    }
}