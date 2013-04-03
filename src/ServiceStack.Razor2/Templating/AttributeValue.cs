﻿//Originally from: https://github.com/NancyFx/Nancy/blob/master/src/Nancy.ViewEngines.Razor/AttributeValue.cs

namespace ServiceStack.Razor.Templating
{
    using System;

    /// <summary>
    /// Class to represent attribute values and, more imporantly, 
    /// decypher them from tuple madness slightly.
    /// </summary>
    public class AttributeValue
    {
        public Tuple<string, int> Prefix { get; private set; }

        public Tuple<object, int> Value { get; private set; }

        public bool IsLiteral { get; private set; }

        public AttributeValue(Tuple<string, int> prefix, Tuple<object, int> value, bool isLiteral)
        {
            this.Prefix = prefix;
            this.Value = value;
            this.IsLiteral = isLiteral;
        }

        public static implicit operator AttributeValue(Tuple<Tuple<string, int>, Tuple<object, int>, bool> value)
        {
            return new AttributeValue(value.Item1, value.Item2, value.Item3);
        }

        public static implicit operator AttributeValue(Tuple<Tuple<string, int>, Tuple<string, int>, bool> value)
        {
            return new AttributeValue(
                value.Item1, new Tuple<object, int>(value.Item2.Item1, value.Item2.Item2), value.Item3);
        }
    }
}