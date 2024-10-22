#if !MONO && !IOS
using System;
using System.Collections.Generic;
using System.Globalization;
using Northwind.Common.ComplexModel;
using Northwind.Common.DataModel;
using Northwind.Common.ServiceModel;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
#if !NETFRAMEWORK
    [Ignore("Fix Northwind.dll")]
#endif
    public class StringSerializerTranslationTests
        : TestBase
    {
        public StringSerializerTranslationTests()
        {
            NorthwindData.LoadData(false);
        }

        [Test]
        public void Can_convert_from_Customer_to_Dictionary()
        {
            var model = DtoFactory.CustomerDto;

            var modelString = TypeSerializer.SerializeToString(model);
            var translateToModel = TypeSerializer.DeserializeFromString<Dictionary<string, string>>(modelString);

            AssertDictonaryIsEqualToCustomer(model, translateToModel);
        }

        private static void AssertDictonaryIsEqualToCustomer(CustomerDto model, IDictionary<string, string> translateToModel)
        {
            Assert.That(translateToModel["Id"], Is.EqualTo(model.Id));
            Assert.That(translateToModel["Address"], Is.EqualTo(model.Address));
            Assert.That(translateToModel["City"], Is.EqualTo(model.City));
            Assert.That(translateToModel["CompanyName"], Is.EqualTo(model.CompanyName));
            Assert.That(translateToModel["ContactName"], Is.EqualTo(model.ContactName));
            Assert.That(translateToModel["ContactTitle"], Is.EqualTo(model.ContactTitle));
            Assert.That(translateToModel["Country"], Is.EqualTo(model.Country));
            Assert.That(translateToModel["Fax"], Is.EqualTo(model.Fax));
            Assert.That(translateToModel["Phone"], Is.EqualTo(model.Phone));
            Assert.That(translateToModel["PostalCode"], Is.EqualTo(model.PostalCode));

            Assert.That(model.Picture, Is.Null);
            Assert.That(translateToModel.ContainsKey("Picture"), Is.False);
        }

        [Test]
        public void Can_convert_ModelWithFieldsOfDifferentTypes_to_string_Dictionary()
        {
            var model = ModelWithFieldsOfDifferentTypes.Create(1);
            var modelString = TypeSerializer.SerializeToString(model);
            var translateToModel = TypeSerializer.DeserializeFromString<Dictionary<string, string>>(modelString);

            Assert.That(translateToModel["Id"], Is.EqualTo(model.Id.ToString()));
            Assert.That(translateToModel["Name"], Is.EqualTo(model.Name));
            Assert.That(translateToModel["Bool"], Is.EqualTo(model.Bool.ToString()));
            Assert.That(translateToModel["DateTime"], Is.EqualTo(DateTimeSerializer.ToShortestXsdDateTimeString(model.DateTime)));
            Assert.That(translateToModel["Double"], Is.EqualTo(model.Double.ToString(CultureInfo.InvariantCulture)));
            Assert.That(translateToModel["Guid"], Is.EqualTo(model.Guid.ToString("N")));
            Assert.That(translateToModel["LongId"], Is.EqualTo(model.LongId.ToString()));
        }

        [Test]
        public void Can_convert_string_Dictionary_to_ModelWithFieldsOfDifferentTypes()
        {
            var model = new Dictionary<string, string>
            {
                { "Id", "1" },
                { "Name", "Name1" },
                { "Bool", "False" },
                { "DateTime", "2008-01-10" },
                { "Double", "1.11" },
                { "Guid", "99161EEC-2857-4031-8CED-EAE21F954496" },
                { "LongId", "999" },
            };

            var modelString = TypeSerializer.SerializeToString(model);
            var translateToModel = TypeSerializer.DeserializeFromString<ModelWithFieldsOfDifferentTypes>(modelString);

            Assert.That(translateToModel.Id, Is.EqualTo(int.Parse(model["Id"])));
            Assert.That(translateToModel.Name, Is.EqualTo(model["Name"]));
            Assert.That(translateToModel.Bool.ToString(), Is.EqualTo(model["Bool"]));
            Assert.That(translateToModel.DateTime, Is.EqualTo(new DateTime(2008, 1, 10)));
            Assert.That(translateToModel.Double, Is.EqualTo(double.Parse(model["Double"], CultureInfo.InvariantCulture)));
            Assert.That(translateToModel.Guid, Is.EqualTo(new Guid("99161EEC-2857-4031-8CED-EAE21F954496")));
            Assert.That(translateToModel.LongId, Is.EqualTo(long.Parse(model["LongId"])));
        }

        [Test]
        public void Can_convert_ModelWithFieldsOfDifferentTypes_to_object_Dictionary()
        {
            var model = ModelWithFieldsOfDifferentTypes.Create(1);
            var modelString = TypeSerializer.SerializeToString(model);
            var translateToModel = TypeSerializer.DeserializeFromString<Dictionary<string, object>>(modelString);

            Assert.That(translateToModel["Id"], Is.EqualTo(model.Id.ToString()));
            Assert.That(translateToModel["Bool"], Is.EqualTo(model.Bool.ToString()));
            Assert.That(translateToModel["DateTime"], Is.EqualTo(DateTimeSerializer.ToShortestXsdDateTimeString(model.DateTime)));
            Assert.That(translateToModel["Double"], Is.EqualTo(model.Double.ToString(CultureInfo.InvariantCulture)));
            Assert.That(translateToModel["Guid"], Is.EqualTo(model.Guid.ToString("N")));
            Assert.That(translateToModel["LongId"], Is.EqualTo(model.LongId.ToString()));
            Assert.That(translateToModel["Name"], Is.EqualTo(model.Name));
        }

        [Test]
        public void Can_convert_Dictionary_to_ModelWithIdAndName()
        {
            var model = new Dictionary<string, string> { { "Id", "1" }, { "Name", "Name" } };
            var modelString = TypeSerializer.SerializeToString(model);
            var translateToModel = TypeSerializer.DeserializeFromString<ModelWithIdAndName>(modelString);

            Assert.That(translateToModel.Id, Is.EqualTo(int.Parse(model["Id"])));
            Assert.That(translateToModel.Name, Is.EqualTo(model["Name"]));
        }

        public class MultiCustomerDictionaries
        {
            public Dictionary<string, string> Customer1 { get; set; }
            public Dictionary<string, string> Customer2 { get; set; }
            public Dictionary<string, string> Customer3 { get; set; }
        }

        [Test]
        public void Can_convert_MultiCustomerProperties_to_MultiCustomerDictionaries()
        {
            var model = DtoFactory.MultiCustomerProperties;
            var modelString = TypeSerializer.SerializeToString(model);
            var translateToModel = TypeSerializer.DeserializeFromString<MultiCustomerDictionaries>(modelString);

            AssertDictonaryIsEqualToCustomer(model.Customer1, translateToModel.Customer1);
            AssertDictonaryIsEqualToCustomer(model.Customer2, translateToModel.Customer2);
            AssertDictonaryIsEqualToCustomer(model.Customer3, translateToModel.Customer3);
        }

    }
}

#endif