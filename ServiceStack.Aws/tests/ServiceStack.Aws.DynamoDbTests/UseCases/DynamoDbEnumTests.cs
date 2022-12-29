using System;
using Amazon.DynamoDBv2;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests.UseCases
{
    public class ModelWithEnums
    {
        public int Id { get; set; }
        public DefaultEnum DefaultEnum { get; set; }
        public FlagsEnum FlagsEnum { get; set; }
        public EnumsAsInt EnumsAsInt { get; set; }
    }

    public enum DefaultEnum
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 3
    }

    [Flags]
    public enum FlagsEnum
    {
        None = 0x0,
        FlagOne = 1 << 0,
        FlagTwo = 1 << 1,
        FlagThree = 1 << 2
    }

    [EnumAsInt]
    public enum EnumsAsInt
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 3,
    }

    public class DynamoDbEnumTests : DynamoTestBase
    {
        private readonly IAmazonDynamoDB awsDb;
        private readonly IPocoDynamo db;

        public DynamoDbEnumTests()
        {
            awsDb = CreateDynamoDbClient();
            db = new PocoDynamo(awsDb);
        }

        [Test]
        public void Does_store_FlagsEnum_and_EnumAsInt_as_integers()
        {
            db.RegisterTable<ModelWithEnums>();
            db.InitSchema();

            var request = new ModelWithEnums
            {
                Id = 1,
                DefaultEnum = DefaultEnum.Value2,
                EnumsAsInt = EnumsAsInt.Value2,
                FlagsEnum = FlagsEnum.FlagOne | FlagsEnum.FlagThree,
            };

            var attrValues = db.Converters.ToAttributeValues(db, request, DynamoMetadata.GetTable<ModelWithEnums>());

            Assert.That(attrValues["DefaultEnum"].S, Is.EqualTo("Value2"));
            Assert.That(attrValues["EnumsAsInt"].N, Is.EqualTo("2"));
            Assert.That(attrValues["FlagsEnum"].N, Is.EqualTo("5"));

            db.PutItem(request);

            var dto = db.GetItem<ModelWithEnums>(1);

            Assert.That(dto.DefaultEnum, Is.EqualTo(DefaultEnum.Value2));
            Assert.That(dto.EnumsAsInt, Is.EqualTo(EnumsAsInt.Value2));
            Assert.That(dto.FlagsEnum, Is.EqualTo(FlagsEnum.FlagOne | FlagsEnum.FlagThree));
        }
    }
}