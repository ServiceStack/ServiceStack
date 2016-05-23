using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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

        public Expression<Func<T>> GetAssignmentExpression<T>(Expression<Func<T>> expr)
        {
            return expr;
        }

        [Test]
        public void Can_get_assigned_constants()
        {
            Assert.That(GetAssignmentExpression(() => new Poco { Name = "Foo" }).AssignedValues(),
                Is.EquivalentTo(new Dictionary<string, object> {
                    {"Name", "Foo"}
                }));
        }

        [Test]
        public void Can_get_assigned_expressions()
        {
            2.Times(i =>
            {
                Assert.That(GetAssignmentExpression(() => new Poco { Name = i % 2 == 0 ? "Foo" : "Bar" }).AssignedValues(),
                    Is.EquivalentTo(new Dictionary<string, object> {
                        { "Name", i % 2 == 0 ? "Foo" : "Bar" }
                    }));
            });
        }
    }
}