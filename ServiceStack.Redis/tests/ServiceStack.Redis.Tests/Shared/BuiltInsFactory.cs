using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Common.Tests.Models
{
    public class BuiltInsFactory
        : ModelFactoryBase<string>
    {
        readonly string[] StringValues = new[] {
            "one", "two", "three", "four",
            "five", "six", "seven"
        };

        public override void AssertIsEqual(string actual, string expected)
        {
            Assert.That(actual, Is.EqualTo(expected));
        }

        public override string CreateInstance(int i)
        {
            return i < StringValues.Length
                ? StringValues[i]
                : i.ToString();
        }
    }

    public class IntFactory
        : ModelFactoryBase<int>
    {
        public override void AssertIsEqual(int actual, int expected)
        {
            Assert.That(actual, Is.EqualTo(expected));
        }

        public override int CreateInstance(int i)
        {
            return i;
        }
    }

    public class DateTimeFactory
        : ModelFactoryBase<DateTime>
    {
        public override void AssertIsEqual(DateTime actual, DateTime expected)
        {
            Assert.That(actual, Is.EqualTo(expected));
        }

        public override DateTime CreateInstance(int i)
        {
            return new DateTime(i, DateTimeKind.Utc);
        }
    }
}