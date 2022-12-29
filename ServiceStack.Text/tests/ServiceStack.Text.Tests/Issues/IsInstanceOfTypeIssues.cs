using System;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.Issues
{    
    public class IsInstanceOfTypeIssues
    {
        [Test]
        public void Nullable_int_and_object_int_are_of_same_Type()
        {
            Assert.That(typeof(int?).IsInstanceOfType(1));
        }
    }
}