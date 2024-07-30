#if !MONO && !IOS
using System;
using System.Collections.Generic;
using System.IO;
using Northwind.Common.ComplexModel;
using Northwind.Common.DataModel;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
#if !NETFRAMEWORK
    [Ignore("Fix Northwind.dll")]
#endif
    public class StringSerializerTests
        : TestBase
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            NorthwindData.LoadData(false);
        }

        [Test]
        public void Can_convert_CustomerOrderListDto()
        {
            var dto = DtoFactory.CustomerOrderListDto;

            Serialize(dto);
        }

        [Test]
        public void Can_convert_to_CustomerOrderListDto()
        {
            var dto = DtoFactory.CustomerOrderListDto;

            Serialize(dto);
        }

        [Test]
        public void Can_convert_to_Customers()
        {
            var dto = NorthwindData.Customers;

            Serialize(dto);
        }

        [Test]
        public void Can_convert_to_Orders()
        {
            NorthwindData.LoadData(false);
            var dto = NorthwindData.Orders;

            Serialize(dto);
        }

        [Test]
        public void Can_serialize_null_object_to_Stream()
        {
            using (var ms = new MemoryStream())
            {
                JsonSerializer.SerializeToStream((object)null, ms);
                TypeSerializer.SerializeToStream((object)null, ms);
                XmlSerializer.SerializeToStream((object)null, ms);
            }
        }

    }
}

#endif
