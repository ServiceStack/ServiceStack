using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.Issues
{
    public class CircularMap : Collection<KeyValuePair<string, CircularMap>> { }

    public class CircularDictionary : Dictionary<string, CircularDictionary> { }

    public class CircularReferenceIssues
    {
        CircularMap CreateCircularMap()
        {
            var to = new CircularMap();
            to.Add(new KeyValuePair<string, CircularMap>("X", to));
            return to;
        }

        CircularDictionary CreateCircularDictionary()
        {
            var to = new CircularDictionary();
            to["X"] = to;
            return to;
        }
        
        [Test]
        public void Does_detect_circular_references_of_CircularMap()
        {
            Assert.That(TypeSerializer.HasCircularReferences(CreateCircularMap()));
        }

        [Test]
        public void Does_not_report_CircularReferences_of_Built_In_Types()
        {
            Assert.That(TypeSerializer.HasCircularReferences(new DateTime()), Is.False);
            Assert.That(TypeSerializer.HasCircularReferences(new TimeSpan()), Is.False);
            Assert.That(TypeSerializer.HasCircularReferences(Guid.NewGuid()), Is.False);
        }

        [Test]
        public void CircularMap_does_stop_at_MaxLimit()
        {
            JsConfig.MaxDepth = 5;
            
            var o = CreateCircularMap();
            var json = o.ToJson();
            
            Assert.That(json.CountOccurrencesOf('X'), Is.EqualTo(5));
            
            JsConfig.Reset();
        }
        
        [Test]
        public void Does_detect_circular_references_of_CircularDictionary()
        {
            Assert.That(TypeSerializer.HasCircularReferences(CreateCircularDictionary()));
        }

        [Test]
        public void CircularDictionary_does_stop_at_MaxLimit()
        {
            JsConfig.MaxDepth = 5;
            
            var o = CreateCircularDictionary();
            var json = o.ToJson();
            json.Print();
            
            Assert.That(json.CountOccurrencesOf('X'), Is.EqualTo(5).Within(1));
            
            JsConfig.Reset();
        }
    }
}