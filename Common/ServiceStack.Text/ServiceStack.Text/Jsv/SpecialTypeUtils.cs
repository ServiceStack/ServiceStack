using System;
using System.Collections.Generic;
using System.IO;

namespace ServiceStack.Text.Jsv
{
	public static class SpecialTypeUtils
	{
		public static Dictionary<Type, Action<TextWriter, object>> SpecialTypes
			= new Dictionary<Type, Action<TextWriter, object>>
	  	{
	  		{ typeof(Uri), WriterUtils.WriteObjectString },
	  		{ typeof(Type), WriteType },
	  		{ typeof(Exception), WriterUtils.WriteException },
	  	};

		public static Action<TextWriter, object> GetWriteFn(Type type)
		{
			Action<TextWriter, object> writeFn = null;
			if (SpecialTypes.TryGetValue(type, out writeFn))
				return writeFn;

			if (type.IsInstanceOfType(typeof(Type)))
				return WriteType;

			if (type.IsInstanceOf(typeof(Exception)))
				return WriterUtils.WriteException;

			return null;
		}

		public static Func<string, object> GetParseMethod(Type type)
		{
			if (type == typeof(Uri))
				return x => new Uri(x.FromCsvField());

			//Warning: typeof(object).IsInstanceOfType(typeof(Type)) == True??
			if (type.IsInstanceOfType(typeof(Type)))
				return ParseType;

			if (type == typeof(Exception))
				return x => new Exception(x);

			if (type.IsInstanceOf(typeof(Exception)))
				return DeserializeTypeUtils.GetParseMethod(type);

			return null;
		}

		public static Type ParseType(string assemblyQualifiedName)
		{
			return Type.GetType(assemblyQualifiedName.FromCsvField());
		}

		public static void WriteType(TextWriter writer, object value)
		{
			writer.Write(((Type)value).AssemblyQualifiedName.ToCsvField());
		}
	}
}