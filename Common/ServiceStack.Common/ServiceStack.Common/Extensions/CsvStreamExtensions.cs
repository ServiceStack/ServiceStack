using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ServiceStack.Common.Text;

namespace ServiceStack.Common.Extensions
{
	public static class CsvStreamExtensions
	{
		public const char DelimiterChar = ',';

		internal class RecordTypePropertyData<T>
		{
			public List<string> Headers { get; set; }

			public List<List<string>> Rows { get; set; }

			public RecordTypePropertyData(IEnumerable<T> records)
			{
				this.Headers = new List<string>();
				this.Rows = new List<List<string>>();

				if (records == null) return;

				var recordType = typeof(T);

				if (recordType.IsValueType || recordType == typeof(string))
				{
					WriteSingleRow(records, recordType);
					return;
				}

				var propertyGetters = new List<MethodInfo>();
				foreach (var propertyInfo in recordType.GetProperties())
				{
					if (!propertyInfo.CanRead || propertyInfo.GetGetMethod() == null) continue;
					if (!StringSerializer.CanCreateFromString(propertyInfo.PropertyType)) continue;
					//if (propertyInfo.PropertyType.FindInterfaces((x, criteria) => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>), null).Length > 0) continue;

					propertyGetters.Add(propertyInfo.GetGetMethod());
					this.Headers.Add(propertyInfo.Name);
				}

				foreach (var record in records)
				{
					var row = new List<string>();
					foreach (var propertyGetter in propertyGetters)
					{
						var value = propertyGetter.Invoke(record, new object[0]) ?? "";

						var strValue = value.GetType() == typeof(string)
							? (string) value
							: StringSerializer.SerializeToString(value);

						row.Add(strValue);
					}
					this.Rows.Add(row);
				}
			}

			private void WriteSingleRow(IEnumerable<T> records, Type recordType)
			{
				var row = new List<string>();
				foreach (var value in records)
				{
					var strValue = recordType == typeof(string)
						? value as string
						: StringSerializer.SerializeToString(value);

					row.Add(strValue);
				}
				this.Rows.Add(row);
			}
		}

		public static void WriteCsv<T>(this Stream outputStream, IEnumerable<T> records)
		{
			using (var textWriter = new StreamWriter(outputStream))
			{
				textWriter.WriteCsv(records);
			}
		}

		public static void WriteCsv<T>(this TextWriter writer, IEnumerable<T> records)
		{
			var dataSource = new RecordTypePropertyData<T>(records);

			if (dataSource.Headers.Count > 0)
			{
				var ranOnce = false;
				foreach (var header in dataSource.Headers)
				{
					ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
					writer.Write(header);
				}
				writer.WriteLine();
			}

			foreach (var row in dataSource.Rows)
			{
				var ranOnce = false;
				foreach (var field in row)
				{
					ToStringMethods.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

					writer.Write(field.ToCsvField());
				}

				writer.WriteLine();
			}
		}

	}

}