using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class EnumerableTests
        : TestBase
    {
        [Test]
        public void Can_serialize_array_list_of_mixed_types()
        {
            var list = (IEnumerable)new ArrayList {
                1.0,
                1.1,
                1,
                new object(),
                "boo",
                1,
                1.2
            };

            Serialize(list);
        }

        [Test]
        public void Can_serialize_array_list_of_mixed_types_with_null()
        {
            var list = (IEnumerable)new ArrayList {
                1.0,
                null,
                1,
                new object(),
                "boo",
                1,
                1.2
            };

            Serialize(list);
        }

        [Test]
        public void Can_serialize_array_list_of_mixed_types_with_null_on_first_position()
        {
            var list = (IEnumerable)new ArrayList {
                null,
                1,
                new object(),
                "boo",
                1,
                1.2
            };

            Serialize(list);
        }
    }
}
