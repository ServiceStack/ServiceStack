using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Common.Host.Utils;
using ServiceStack.Common.Services.Serialization;
using ServiceStack.Common.Services.Utils;
using NUnit.Framework;

namespace ServiceStack.Common.Host.Tests.Endpoints
{
    [TestFixture]
    public class ObjectReflectionUtilsTests
    {
        [Test]
        public void populate_request_data_contract()
        {
            var type = typeof(GetUsers);
            var result = ObjectReflectionUtils.PopulateType(type);
            //ObjectDumperUtils.Write(result);
            var json = JsonDataContractSerializer.Instance.Parse(result);
            Logging.LogManager.GetLogger(GetType()).Debug(json);
        }

        [Test]
        public void populate_response_data_contract()
        {
            var type = typeof(GetUsersResponse);
            var result = ObjectReflectionUtils.PopulateType(type);
            var json = JsonDataContractSerializer.Instance.Parse(result);
            Logging.LogManager.GetLogger(GetType()).Debug(json);
        }
    }

    [DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
    public class GetUsers
    {
        public GetUsers()
        {
            this.Version = 100;
            Ids = new ArrayOfIntId();
        }

        [DataMember]
        public int Version { get; set; }
        [DataMember]
        public ArrayOfIntId Ids { get; set; }
    }

    [CollectionDataContract(Namespace = "http://schemas.ddnglobal.com/types/", ItemName = "Id")]
    public class ArrayOfIntId : List<int>
    {
        public ArrayOfIntId() { }
        public ArrayOfIntId(IEnumerable<int> collection) : base(collection) { }
    }

        [DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
    public class GetUsersResponse
    {
        public GetUsersResponse()
        {
            this.Version = 100;
            Users = new List<User>();
        }

        [DataMember]
        public int Version { get; set; }
        [DataMember]
        public List<User> Users { get; set; }
    }

    public class User
    {
        public string Id { get; set; }
        public Guid GlobalId { get; set; }
        public DateTime DateTime { get; set; }
        public TimeSpan TimeSpan { get; set; }
    }
}