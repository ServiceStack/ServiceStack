//
// http://code.google.com/p/servicestack/wiki/TypeSerializer
// ServiceStack.Text: .NET C# POCO Type Text Serializer.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

using System;
using System.Collections.Generic;
using System.IO;

namespace ServiceStack.Text.Common
{
	internal delegate void WriteListDelegate(TextWriter writer, object oList, WriteObjectDelegate toStringFn);

	internal delegate void WriteGenericListDelegate<T>(TextWriter writer, IList<T> list, WriteObjectDelegate toStringFn);

	internal delegate void WriteDelegate(TextWriter writer, object value);

	internal delegate ParseStringDelegate ParseFactoryDelegate();

	internal delegate void WriteObjectDelegate(TextWriter writer, object obj);

	public delegate void SetPropertyDelegate(object instance, object propertyValue);

	public delegate object ParseStringDelegate(string stringValue);
}