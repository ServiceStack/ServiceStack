//
// https://github.com/ServiceStack/ServiceStack.Text
// ServiceStack.Text: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2012 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections.Generic;
using System.IO;

namespace ServiceStack.Text.Common
{
    internal delegate void WriteListDelegate(TextWriter writer, object oList, WriteObjectDelegate toStringFn);

    internal delegate void WriteGenericListDelegate<T>(TextWriter writer, IList<T> list, WriteObjectDelegate toStringFn);

    internal delegate void WriteDelegate(TextWriter writer, object value);

    internal delegate ParseStringSpanDelegate ParseFactoryDelegate();

    public delegate object DeserializeStringSpanDelegate(Type type, ReadOnlySpan<char> source);

    public delegate void WriteObjectDelegate(TextWriter writer, object obj);

    public delegate object ParseStringDelegate(string stringValue);

    public delegate object ParseStringSpanDelegate(ReadOnlySpan<char> value);

    public delegate object ConvertObjectDelegate(object fromObject);

    public delegate object ConvertInstanceDelegate(object obj, Type type);

    public delegate void DeserializationErrorDelegate(object instance, Type propertyType, string propertyName, string propertyValueStr, Exception ex);
}
