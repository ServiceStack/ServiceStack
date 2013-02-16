#if !SILVERLIGHT && !MONOTOUCH && !XBOX
//
// ServiceStack: Useful extensions to simplify parsing xml with XLinq
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack.
//
// Licensed under the same terms of reddis and ServiceStack: new BSD license.
//

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace ServiceStack.ServiceModel.Extensions
{
    public static class XLinqExtensions
    {
        public static string GetString(this XElement el, string name)
        {
            return el == null ? null : GetElementValueOrDefault(el, name, x => x.Value);
        }

        public static bool GetBool(this XElement el, string name)
        {
            AssertElementHasValue(el, name);
            return (bool)GetElement(el, name);
        }

        public static bool GetBoolOrDefault(this XElement el, string name)
        {
            return GetElementValueOrDefault(el, name, x => (bool)x);
        }

        public static bool? GetNullableBool(this XElement el, string name)
        {
            var childEl = GetElement(el, name);
            return childEl == null || string.IsNullOrEmpty(childEl.Value) ? null : (bool?)childEl;
        }

        public static int GetInt(this XElement el, string name)
        {
            AssertElementHasValue(el, name);
            return (int)GetElement(el, name);
        }

        public static int GetIntOrDefault(this XElement el, string name)
        {
            return GetElementValueOrDefault(el, name, x => (int) x);
        }

        public static int? GetNullableInt(this XElement el, string name)
        {
            var childEl = GetElement(el, name);
            return childEl == null || string.IsNullOrEmpty(childEl.Value) ? null : (int?) childEl;
        }

        public static long GetLong(this XElement el, string name)
        {
            AssertElementHasValue(el, name);
            return (long)GetElement(el, name);
        }

        public static long GetLongOrDefault(this XElement el, string name)
        {
            return GetElementValueOrDefault(el, name, x => (long)x);
        }

        public static long? GetNullableLong(this XElement el, string name)
        {
            var childEl = GetElement(el, name);
            return childEl == null || string.IsNullOrEmpty(childEl.Value) ? null : (long?)childEl;
        }

        public static decimal GetDecimal(this XElement el, string name)
        {
            AssertElementHasValue(el, name);
            return (decimal)GetElement(el, name);
        }

        public static decimal GetDecimalOrDefault(this XElement el, string name)
        {
            return GetElementValueOrDefault(el, name, x => (decimal)x);
        }

        public static decimal? GetNullableDecimal(this XElement el, string name)
        {
            var childEl = GetElement(el, name);
            return childEl == null || string.IsNullOrEmpty(childEl.Value) ? null : (decimal?)childEl;
        }

        public static DateTime GetDateTime(this XElement el, string name)
        {
            AssertElementHasValue(el, name);
            return (DateTime)GetElement(el, name);
        }

        public static DateTime GetDateTimeOrDefault(this XElement el, string name)
        {
            return GetElementValueOrDefault(el, name, x => (DateTime)x);
        }

        public static DateTime? GetNullableDateTime(this XElement el, string name)
        {
            var childEl = GetElement(el, name);
            return childEl == null || string.IsNullOrEmpty(childEl.Value) ? null : (DateTime?)childEl;
        }

        public static TimeSpan GetTimeSpan(this XElement el, string name)
        {
            AssertElementHasValue(el, name);
            return (TimeSpan)GetElement(el, name);
        }

        public static TimeSpan GetTimeSpanOrDefault(this XElement el, string name)
        {
            return GetElementValueOrDefault(el, name, x => (TimeSpan)x);
        }

        public static TimeSpan? GetNullableTimeSpan(this XElement el, string name)
        {
            var childEl = GetElement(el, name);
            return childEl == null || string.IsNullOrEmpty(childEl.Value) ? null : (TimeSpan?)childEl;
        }

        public static Guid GetGuid(this XElement el, string name)
        {
            AssertElementHasValue(el, name);
            return (Guid)GetElement(el, name);
        }

        public static Guid GetGuidOrDefault(this XElement el, string name)
        {
            return GetElementValueOrDefault(el, name, x => (Guid)x);
        }

        public static Guid? GetNullableGuid(this XElement el, string name)
        {
            var childEl = GetElement(el, name);
            return childEl == null || string.IsNullOrEmpty(childEl.Value) ? null : (Guid?)childEl;
        }

        public static T GetElementValueOrDefault<T>(this XElement element, string name, Func<XElement, T> converter)
        {
            if (converter == null)
            {
                throw new ArgumentNullException("converter");
            }
            var el = GetElement(element, name);
            return el == null || string.IsNullOrEmpty(el.Value) ? default(T) : converter(el);
        }

        public static XElement GetElement(this XElement element, string name)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return element.AnyElement(name);
        }

        public static void AssertElementHasValue(this XElement element, string name)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            var childEl = element.AnyElement(name);
            if (childEl == null || string.IsNullOrEmpty(childEl.Value))
            {
                throw new ArgumentNullException(name, string.Format("{0} is required", name));
            }
        }

        public static List<string> GetValues(this IEnumerable<XElement> els)
        {
            var values = new List<string>();
            foreach (var el in els)
            {
                values.Add(el.Value);
            }
            return values;
        }

        public static XAttribute AnyAttribute(this XElement element, string name)
        {
            if (element == null) return null;
            foreach (var attribute in element.Attributes())
            {
                if (attribute.Name.LocalName == name)
                {
                    return attribute;
                }
            }
            return null;
        }

        public static IEnumerable<XElement> AllElements(this XElement element, string name)
        {
            var els = new List<XElement>();
            if (element == null) return els;
            foreach (var node in element.Nodes())
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                var childEl = (XElement)node;
                if (childEl.Name.LocalName == name)
                {
                    els.Add(childEl);
                }
            }
            return els;
        }

        public static XElement AnyElement(this XElement element, string name)
        {
            if (element == null) return null;
            foreach (var node in element.Nodes())
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                var childEl = (XElement)node;
                if (childEl.Name.LocalName == name)
                {
                    return childEl;
                }
            }
            return null;
        }

        public static XElement AnyElement(this IEnumerable<XElement> elements, string name)
        {
            foreach (var element in elements)
            {
                if (element.Name.LocalName == name)
                {
                    return element;
                }
            }
            return null;
        }

        public static IEnumerable<XElement> AllElements(this IEnumerable<XElement> elements, string name)
        {
            var els = new List<XElement>();
            foreach (var element in elements)
            {
                els.AddRange(AllElements(element, name));
            }
            return els;
        }

        public static XElement FirstElement(this XElement element)
        {
            if (element.FirstNode.NodeType == XmlNodeType.Element)
            {
                return (XElement) element.FirstNode;
            }
            return null;
        }

    }
}
#endif
