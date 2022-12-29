using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.ServiceHost.Tests.UseCase.Operations
{
    [DataContract]
    public class StoreCustomers
    {
        public StoreCustomers()
        {
            Customers = new List<Customer>();
        }

        [DataMember]
        public List<Customer> Customers { get; set; }
    }
}