using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using NUnit.Framework;
using ProtoBuf.Meta;
using ServiceStack.Text;

namespace ServiceStack.Extensions.Tests
{
    [DataContract]
    public class Query
    {
        [DataMember(Order = 1)]
        public virtual string Include { get; set; }
    }

    [DataContract]
    public class NonGenericQueryBase : QueryBase {}

    [DataContract]
    public class NonGenericMyQueryBase : MyQueryBase {}

    [DataContract]
    public abstract class MyQueryBase
    {
        [DataMember(Order = 1)]
        public virtual string Include { get; set; }
    }
    
    [DataContract]
    public abstract class HiddenBase
    {
        [DataMember(Order = 1)]
        public string FirstName { get; set; }
        [DataMember(Order = 2)]
        public LivingStatus LivingStatus { get; set; }
    }

    [DataContract]
    public class Shadowed : HiddenBase
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public new string FirstName { get; set; }
        [DataMember(Order = 3)]
        public new LivingStatus? LivingStatus { get; set; } //overridden property
    }

    public class ProtobufTests
    {
        public T Serialize<T>(T dto, TypeModel model = null)
        {
            if (model != null)
            {
                byte[] bytes = null;
                using (var ms = new MemoryStream())
                {
                    model.Serialize(ms, dto);
                    bytes = ms.ToArray();
                }
                using (var ms = new MemoryStream(bytes))
                {
                    var to = (T) model.Deserialize(ms, (object)null, typeof(T));
                    return to;
                }
            }
            else
            {
                var bytes = GrpcMarshaller<T>.Instance.Serializer(dto);
                var to = GrpcMarshaller<T>.Instance.Deserializer(bytes);
                return to;
            }
        }

        public T SerializeGrpc<T>(T dto)
        {
            var bytes = GrpcMarshaller<T>.Instance.Serializer(dto);
            var to =  GrpcMarshaller<T>.Instance.Deserializer(bytes);
            return to;
        }

        [Test]
        public void Can_Serialize_Query()
        {
            var dto = new Query { Include = "Total" };
            var to = Serialize(dto);
            Assert.That(to.Include, Is.EqualTo(dto.Include));
        }

        [Test]
        public void Can_Serialize_QueryRockstars_TypeModel()
        {
            var model = RuntimeTypeModel.Create();
            
            //var metaType = model.Add(typeof(QueryBase), true);
            model[typeof(QueryBase)].AddSubType(101, typeof(QueryDb<Rockstar>));
            model[typeof(QueryDb<Rockstar>)].AddSubType(101, typeof(QueryRockstars));

            var dto = new QueryRockstars { Include = "Total" };
            var to = Serialize(dto, model);
            Assert.That(to.Include, Is.EqualTo(dto.Include));
        }

        [Test]
        public void Can_Serialize_QueryRockstars()
        {
            var dto = new QueryRockstars { Include = "Total" };
            var to = SerializeGrpc(dto);
            Assert.That(to.Include, Is.EqualTo(dto.Include));
        }

        [Test]
        public void Can_Serialize_QueryResponse_NamedRockstar()
        {
//            GrpcUtils.Register<NamedRockstar>();
            var dto = new QueryResponse<NamedRockstar> {
                Total = 1,
                Results = new List<NamedRockstar> {
                    new NamedRockstar {
                        Id = 1,
                        FirstName = "Microsoft",
                        LastName = "SQL Server",
                        Age = 27,
                        DateOfBirth = new DateTime(1989,1,1),
                        LivingStatus = LivingStatus.Alive,
                    }
                }
            };
            var to = SerializeGrpc(dto);
            to.PrintDump();
            Assert.That(to.Results[0].LastName, Is.EqualTo("SQL Server"));
        }

        [Test]
        public void Can_serialize_bytes()
        {
            var dto = new FileContent {
                Body = "abc".ToUtf8Bytes(),
            };
            var toDto = SerializeGrpc(dto);
            Assert.That(toDto.Body, Is.EqualTo(dto.Body));
        }

        [Test]
        public void Can_serialize_hidden_property()
        {
            var dto = new Shadowed {
                Id = 1,
                FirstName = "Updated",
                LivingStatus = LivingStatus.Dead,
            };
            var toDto = SerializeGrpc(dto);
            Assert.That(toDto.Id, Is.EqualTo(dto.Id));
            Assert.That(toDto.FirstName, Is.EqualTo(dto.FirstName));
            Assert.That(toDto.LivingStatus, Is.EqualTo(dto.LivingStatus));
        }
    }
}