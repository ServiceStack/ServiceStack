using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Common.Tests.Models
{
    public class ModelWithMapAndList<T>
    {
        public ModelWithMapAndList()
        {
            this.Map = new Dictionary<T, T>();
            this.List = new List<T>();
        }

        public ModelWithMapAndList(int id)
            : this()
        {
            Id = id;
            Name = "Name" + id;
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public Dictionary<T, T> Map { get; set; }

        public List<T> List { get; set; }

        public static ModelWithMapAndList<T> Create<U>(int id)
        {
            return new ModelWithMapAndList<T>(id);
        }

        public static void AssertIsEqual(ModelWithMapAndList<T> actual, ModelWithMapAndList<T> expected)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.Map, Is.EquivalentTo(expected.Map));
            Assert.That(actual.List, Is.EquivalentTo(expected.List));
        }
    }
}