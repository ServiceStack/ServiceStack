using System;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.JsonTests
{
    [Flags]
    public enum DbModelComparisonTypesEnum : int
    {
        /// <summary>
        /// Compare only the PrimaryKey values
        /// </summary>
        PkOnly = 1,
        /// <summary>
        /// Compare only the non PrimaryKey values
        /// </summary>
        NonPkOnly = 2,
        /// <summary>
        /// Compare all values
        /// (The PrimaryKey and non PrimaryKey values too)
        /// </summary>
        All = 3 // PkOnly & NonPkOnly
    }

    public partial class Question
    {
        public static DbModelComparisonTypesEnum DefaultComparisonType { get; set; }

        public Guid Id { get; set; }
        public string Title { get; set; }
    }
    
    [TestFixture]
    public class JsonEnumTests
    {
        [Test]
        public void Can_serialize_dto_with_static_enum()
        {
            Question.DefaultComparisonType = DbModelComparisonTypesEnum.All;

            var dto = new Question
            {
                Id = Guid.NewGuid(),
                Title = "Title",
            };

            var json = dto.ToJson();
            var q = json.FromJson<Question>();

            Assert.That(q.Id, Is.EqualTo(dto.Id));
            Assert.That(q.Title, Is.EqualTo(dto.Title));
        }
    }
}