using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
    public enum UserFileType
    {
        DefaultProfile,
        OriginalProfile,
        Profile75X75,
        Profile66X66,
        Profile63X63,
    }

    public class TestClassA
    {
        public IList<string> ToStringList { get; set; }
        public ArrayOfString FromStringList { get; set; }
        public IList<UserFileType> FromUserFileTypes { get; set; }
    }

    public class TestClassB
    {
        public ArrayOfString ToStringList { get; set; }
        public IList<string> FromStringList { get; set; }
        public ArrayOfString FromUserFileTypes { get; set; }
    }

    public class TestClassC
    {
        public IList<string> FromStringList { get; protected set; }
    }

    [TestFixture]
    public class ReflectionExtensionsTests
    {
        [Test]
        public void Can_translate_generic_lists()
        {
            var values = new[] { "A", "B", "C" };
            var testA = new TestClassA
            {
                FromStringList = new ArrayOfString(values),
                ToStringList = new List<string>(values),
                FromUserFileTypes = new List<UserFileType>
                {
                    UserFileType.DefaultProfile, UserFileType.OriginalProfile
                },
            };

            var fromTestA = testA.ConvertTo<TestClassB>();

            AssertAreEqual(testA, fromTestA);

            var userFileTypeValues = testA.FromUserFileTypes.Map(x => x.ToString());
            var testB = new TestClassB
            {
                FromStringList = new List<string>(values),
                ToStringList = new ArrayOfString(values),
                FromUserFileTypes = new ArrayOfString(userFileTypeValues),
            };

            var fromTestB = testB.ConvertTo<TestClassA>();
            AssertAreEqual(fromTestB, testB);
        }

        [Test]
        public void Can_translate_generic_list_does_ignore_protected_setters()
        {
            var values = new[] { "A", "B", "C" };
            var testA = new TestClassA
            {
                ToStringList = new List<string>(values),
            };

            var fromTestA = testA.ConvertTo<TestClassC>();
            Assert.NotNull(fromTestA);
            Assert.IsNull(fromTestA.FromStringList);
        }

        private static void AssertAreEqual(TestClassA testA, TestClassB testB)
        {
            Assert.That(testA, Is.Not.Null);
            Assert.That(testB, Is.Not.Null);

            Assert.That(testA.FromStringList, Is.Not.Null);
            Assert.That(testB.FromStringList, Is.Not.Null);
            Assert.That(testA.FromStringList,
                Is.EquivalentTo(new List<string>(testB.FromStringList)));

            Assert.That(testA.ToStringList, Is.Not.Null);
            Assert.That(testB.ToStringList, Is.Not.Null);
            Assert.That(testA.ToStringList, Is.EquivalentTo(testB.ToStringList));

            Assert.That(testA.FromUserFileTypes, Is.Not.Null);
            Assert.That(testB.FromUserFileTypes, Is.Not.Null);
            Assert.That(testA.FromUserFileTypes,
                Is.EquivalentTo(testB.FromUserFileTypes.ConvertAll(x => x.ToEnum<UserFileType>())));
        }
    }
}