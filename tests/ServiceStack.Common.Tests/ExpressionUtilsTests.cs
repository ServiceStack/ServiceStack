using System;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Common.Tests
{
    public class ExpressionUtilsTests
    {
        [Test]
        public void Does_GetMemberName()
        {
            Assert.That(ExpressionUtils.GetMemberName((Poco x) => x.Name),
                Is.EqualTo("Name"));

            Assert.That(ExpressionUtils.GetMemberName((ModelWithFieldsOfNullableTypes x) => x.NId),
                Is.EqualTo("NId"));
        }
    }
}