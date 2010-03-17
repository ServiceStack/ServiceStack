using System;
using System.Collections.Generic;
using System.IO;

namespace ServiceStack.Text.Jsv
{
	internal delegate void WriteListDelegate(TextWriter writer, object oList, Action<TextWriter, object> toStringFn);
	
	internal delegate void WriteGenericListDelegate<T>(TextWriter writer, IList<T> list, Action<TextWriter, object> toStringFn);

	internal delegate void WriteDelegate(TextWriter writer, object value);

	internal delegate Func<string, object> ParseFactoryDelegate();
}