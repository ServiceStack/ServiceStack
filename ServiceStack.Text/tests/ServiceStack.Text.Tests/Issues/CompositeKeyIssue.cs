using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.Issues
{
    public class Container<TKey>
    {
        public IDictionary<TKey, string[]> Data { get; set; } = new Dictionary<TKey, string[]>();
    }

    public struct CompositeKey
    {
        public string Name { get; set; }
        public bool Value { get; set; }
        public CompositeKey(string name, bool value)
        {
            Name = name;
            Value = value;
        }

        public CompositeKey(string jsonKey)
        {
            Name = jsonKey.LeftPart(':');
            Value = jsonKey.RightPart(':').ConvertTo<bool>();
        }

        public bool Equals(CompositeKey other) => 
            Name == other.Name && Value == other.Value;

        public override bool Equals(object obj) => 
            obj is CompositeKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) 
                       ^ Value.GetHashCode();
            }
        }

        public override string ToString() => $"{Name}:{Value}";
    }

    
    public class CompositeKeyIssue
    {
        [Test]
        public void Can_serialize_CompositeKey()
        {
            var dto = new Container<CompositeKey>
            {
                Data = new Dictionary<CompositeKey, string[]>
                {
                    { new CompositeKey("abc", false), new[] { "1","2","3"} },
                    { new CompositeKey("bdf", true), new[] { "b","c","d"} },
                    { new CompositeKey("ceg", false), new[] { "4","5","6"} },
                }
            };

            var serialized = JsonSerializer.SerializeToString(dto);
            var fromJson = JsonSerializer.DeserializeFromString<Container<CompositeKey>>(serialized);
            
            Assert.That(fromJson.Data.Count, Is.EqualTo(dto.Data.Count));
            Assert.That(fromJson.Data, Is.EqualTo(dto.Data));
        }
    }
}