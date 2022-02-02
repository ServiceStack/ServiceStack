using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Common.Tests.Models
{
    public class ModelWithComplexTypes
    {
        public ModelWithComplexTypes()
        {
            this.StringList = new List<string>();
            this.IntList = new List<int>();
            this.StringMap = new Dictionary<string, string>();
            this.IntMap = new Dictionary<int, int>();
        }

        public long Id { get; set; }

        public List<string> StringList { get; set; }

        public List<int> IntList { get; set; }

        public Dictionary<string, string> StringMap { get; set; }

        public Dictionary<int, int> IntMap { get; set; }

        public ModelWithComplexTypes Child { get; set; }

        public static ModelWithComplexTypes Create(int id)
        {
            var row = new ModelWithComplexTypes
            {
                Id = id,
                StringList = { "val" + id + 1, "val" + id + 2, "val" + id + 3 },
                IntList = { id + 1, id + 2, id + 3 },
                StringMap =
                    {
                        {"key" + id + 1, "val" + id + 1},
                        {"key" + id + 2, "val" + id + 2},
                        {"key" + id + 3, "val" + id + 3},
                    },
                IntMap =
                    {
                        {id + 1, id + 2},
                        {id + 3, id + 4},
                        {id + 5, id + 6},
                    },
                Child = new ModelWithComplexTypes { Id = id * 2 },
            };

            return row;
        }

        public static ModelWithComplexTypes CreateConstant(int i)
        {
            return Create(i);
        }

        public static void AssertIsEqual(ModelWithComplexTypes actual, ModelWithComplexTypes expected)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.StringList, Is.EquivalentTo(expected.StringList));
            Assert.That(actual.IntList, Is.EquivalentTo(expected.IntList));
            Assert.That(actual.StringMap, Is.EquivalentTo(expected.StringMap));
            Assert.That(actual.IntMap, Is.EquivalentTo(expected.IntMap));

            if (expected.Child == null)
            {
                Assert.That(actual.Child, Is.Null);
            }
            else
            {
                Assert.That(actual.Child, Is.Not.Null);
                AssertIsEqual(actual.Child, expected.Child);
            }
        }
    }
}