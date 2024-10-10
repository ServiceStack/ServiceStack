// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if NETFRAMEWORK
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.MsgPack;
using ServiceStack.ProtoBuf;

namespace ServiceStack.Common.Tests
{
    [DataContract]
    public class TestModel
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string Name { get; set; }
    }

    [TestFixture]
    public class FormatTests
    {
        [Test]
        public void Can_serialize_ProtoBuf()
        {
            var dto = new TestModel { Id = 1, Name = "Name" };

            var bytes = dto.ToProtoBuf();

            var fromBytes = bytes.FromProtoBuf<TestModel>();

            Assert.That(fromBytes.Id, Is.EqualTo(dto.Id));
            Assert.That(fromBytes.Name, Is.EqualTo(dto.Name));
        }

        [Test]
        public void Can_serialize_MsgPack()
        {
            var dto = new TestModel { Id = 1, Name = "Name" };

            var bytes = dto.ToMsgPack();

            var fromBytes = bytes.FromMsgPack<TestModel>();

            Assert.That(fromBytes.Id, Is.EqualTo(dto.Id));
            Assert.That(fromBytes.Name, Is.EqualTo(dto.Name));
        }
    }
}
#endif
