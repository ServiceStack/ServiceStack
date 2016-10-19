using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public partial class CsvItem : IReturn<CsvItem>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<int> Ints { get; set; }
        public List<string> Strings { get; set; }
        public List<CsvPoco> CsvPocos { get; set; }
        public Dictionary<string, CsvPoco> CsvPocoMap { get; set; }
    }

    public partial class CsvPoco
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }


    [Route("/csvlist")]
    public class CsvList : List<CsvItem>, IReturn<CsvList>
    {
        public CsvList() {}
        public CsvList(IEnumerable<CsvItem> collection) : base(collection) {}
    }

    [Route("/csvfirst")]
    [Csv(CsvBehavior.FirstEnumerable)]
    public class CsvFirstEnumerable : IReturn<CsvFirstEnumerable>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<CsvItem> Items { get; set; }
    }

    [Route("/csvdto")]
    [DataContract]
    public class CsvDtoEnumerable : IReturn<CsvDtoEnumerable>
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<CsvItem> Items { get; set; }
    }

    public class CsvServices : Service
    {
        public object Any(CsvItem request)
        {
            return request;
        }

        public object Any(CsvList request)
        {
            return request;
        }

        public object Any(CsvFirstEnumerable request)
        {
            return request;
        }

        public object Any(CsvDtoEnumerable request)
        {
            return request;
        }
    }

    [TestFixture]
    public class CsvServiceClientTests
    {
        private readonly ServiceStackHost appHost;
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(typeof(CsvServiceClientTests).Name, typeof(CsvServices).GetAssembly()) {}

            public override void Configure(Container container) {}
        }

        public CsvServiceClientTests()
        {
            appHost = new AppHost().Init().Start(Config.ListeningOn);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        CsvItem CreateCsvItem(int i)
        {
            return new CsvItem
            {
                Id = i,
                Name = "Name" + i,
                Ints = i.Times(x => x),
                Strings = i.Times(x => "Name" + x),
                CsvPocos = new List<CsvPoco>
                {
                    new CsvPoco { Id = 10 + i, Name = "CsvPoco" + i },
                },
                CsvPocoMap = new Dictionary<string, CsvPoco>
                {
                    { "Key" + i, new CsvPoco { Id = 10 + i, Name = "CsvPoco" + i } }
                }
            };
        }

        [Test]
        public void Can_SendAll_CsvItem()
        {
            var client = new CsvServiceClient(Config.ListeningOn);

            var dtos = 3.Times(x => CreateCsvItem(x));

            var response = client.SendAll(dtos);
            Assert.That(response, Is.EquivalentTo(dtos));

            response = Config.ListeningOn.CombineWith("csv/reply/CsvItem[]")
                .PostCsvToUrl(dtos)
                .FromCsv<List<CsvItem>>();
            Assert.That(response, Is.EquivalentTo(dtos));
        }

        [Test]
        public void Can_POST_CsvList()
        {
            var client = new CsvServiceClient(Config.ListeningOn);

            var dtos = 3.Times(x => CreateCsvItem(x));

            var response = client.Post(new CsvList(dtos));
            Assert.That(response, Is.EquivalentTo(dtos));

            response = Config.ListeningOn.CombineWith("csvlist")
                .PostCsvToUrl(dtos)
                .FromCsv<CsvList>();
            Assert.That(response, Is.EquivalentTo(dtos));

            var csv = dtos.ToCsv();
            response = Config.ListeningOn.CombineWith("csvlist")
                .PostCsvToUrl(csv)
                .FromCsv<CsvList>();
            Assert.That(response, Is.EquivalentTo(dtos));
        }

        [Test]
        public void Can_POST_CsvFirstEnumerable()
        {
            var client = new CsvServiceClient(Config.ListeningOn);

            var dto = new CsvFirstEnumerable
            {
                Id = 1,
                Name = "Name",
                Items = 3.Times(x => CreateCsvItem(x))
            };

            var response = client.Post(dto);
            Assert.That(response.Id, Is.EqualTo(0));
            Assert.That(response.Name, Is.Null);
            Assert.That(response.Items, Is.EquivalentTo(dto.Items));

            response = Config.ListeningOn.CombineWith("csvfirst")
                .PostCsvToUrl(dto)
                .FromCsv<CsvFirstEnumerable>();
            Assert.That(response.Id, Is.EqualTo(0));
            Assert.That(response.Name, Is.Null);
            Assert.That(response.Items, Is.EquivalentTo(dto.Items));
        }

        [Test]
        public void Can_POST_CsvDto()
        {
            var client = new CsvServiceClient(Config.ListeningOn);

            var dto = new CsvDtoEnumerable
            {
                Id = 1,
                Name = "Name",
                Items = 3.Times(x => CreateCsvItem(x))
            };

            var response = client.Post(dto);
            Assert.That(response.Id, Is.EqualTo(0));
            Assert.That(response.Name, Is.Null);
            Assert.That(response.Items, Is.EquivalentTo(dto.Items));

            response = Config.ListeningOn.CombineWith("csvdto")
                .PostCsvToUrl(dto)
                .FromCsv<CsvDtoEnumerable>();
            Assert.That(response.Id, Is.EqualTo(0));
            Assert.That(response.Name, Is.Null);
            Assert.That(response.Items, Is.EquivalentTo(dto.Items));
        }
    }

    public partial class CsvItem : IEquatable<CsvItem>
    {
        public bool Equals(CsvItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id
                && string.Equals(Name, other.Name)
                && Ints.EquivalentTo(other.Ints)
                && Strings.EquivalentTo(other.Strings)
                && CsvPocos.EquivalentTo(other.CsvPocos)
                && CsvPocoMap.EquivalentTo(other.CsvPocoMap);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CsvItem)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Ints != null ? Ints.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Strings != null ? Strings.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CsvPocos != null ? CsvPocos.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CsvPocoMap != null ? CsvPocoMap.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public partial class CsvPoco : IEquatable<CsvPoco>
    {
        public bool Equals(CsvPoco other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CsvPoco)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }
    }
}