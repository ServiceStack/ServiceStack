using System.Runtime.Serialization;

namespace ServiceStack.ServiceHost.Tests.UseCase.Operations
{
    [DataContract]
    public class GetCustomer
    {
        [DataMember]
        public long CustomerId { get; set; }
    }

    [DataContract]
    public class GetCustomerResponse
    {
        [DataMember]
        public Customer Customer { get; set; }
    }

    [DataContract]
    public class Customer
    {
        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public string FirstName { get; set; }

        [DataMember]
        public string LastName { get; set; }
    }
}