using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace ServiceStack.Examples.DataContractTests
{

	class Program
	{
		public const string Namespace =  "http://schemas.servicestack.net/examples/test";

		private static bool showDetailLog = true;

		static void Main(string[] args)
		{
			showDetailLog = args.Length > 0 && args[0] == "-v";

			TestDataContract(new OneStringField { StringField = "StringValue" });
			TestDataContract(new OneGuidField { GuidField = Guid.NewGuid() });
			TestDataContract(new OneDateTimeField { DateTimeField = DateTime.Now });
			TestDataContract(new OneDoubleField { DoubleField = 99.9 });
			TestDataContract(new OneDecimalField { DecimalField = 1.99m });
			TestDataContract(new ThreeStringFields { StringField1 = "StringValue1", StringField2 = "StringValue2", StringField3 = "StringValue3" });
			TestDataContract(new ThreeStringFieldsUnOrdered { StringField1 = "StringValue1", StringField2 = "StringValue2", StringField3 = "StringValue3" });
			TestDataContract(new ThreeStringFieldsOrdered { StringField1 = "StringValue1", StringField2 = "StringValue2", StringField3 = "StringValue3" });
			TestDataContract(new DifferentValueTypesOrdered { StringField = "StringValue", Guid = Guid.NewGuid(), DateTime = DateTime.Now, Decimal = 1.99m, Double = 99.9 });

			TestDataContract(new OneArrayOfStringField { ArrayOfStrings = new ArrayOfString { "String1", "String2", "String3" } });
			TestDataContract(new Property { Name = "PropertyName", Value = "PropertyValue" });
			TestDataContract(new OnePropertiesField { Properties = { new Property { Name = "PropertyName", Value = "PropertyValue" }, new Property { Name = "PropertyName2", Value = "PropertyValue2" } } });

			Console.ReadKey();
		}

		static void TestDataContract(object dataContract)
		{
			var dtoName = dataContract.GetType().Name;
			try
			{
				Console.WriteLine();

				string xml = DataContractSerializer.Instance.Parse(dataContract);

				Console.WriteLine("Can serialize: " + dtoName);

				if (showDetailLog)
				{
					Console.WriteLine(xml);
				}

				var deserializedDto = DataContractDeserializer.Instance.Parse(xml, dataContract.GetType());

				Console.WriteLine("Can deserialize: " + dtoName);

			}
			catch (Exception ex)
			{
				Console.WriteLine("Failed to deserialize: " + dataContract.GetType().Name);
			}
		}
	}


	#region Deserializers
	public class DataContractSerializer
	{
		public static DataContractSerializer Instance = new DataContractSerializer();

		public string Parse<XmlDto>(XmlDto from, bool indentXml)
		{
			try
			{
				using (var ms = new MemoryStream())
				{
					using (var xw = new XmlTextWriter(ms, Encoding.UTF8))
					{
						if (indentXml)
						{
							xw.Formatting = Formatting.Indented;
						}

						var serializer = new System.Runtime.Serialization.DataContractSerializer(from.GetType());
						serializer.WriteObject(xw, from);
						xw.Flush();
						ms.Seek(0, SeekOrigin.Begin);
						using (var reader = new StreamReader(ms))
						{
							return reader.ReadToEnd();
						}
					}
				}
			}
			catch (Exception ex)
			{
				throw new SerializationException(string.Format("Error serializing object of type {0}", from.GetType().FullName), ex);
			}
		}

		public string Parse<XmlDto>(XmlDto from)
		{
			return Parse(from, false);
		}
	}

	public class DataContractDeserializer
	{
		/// <summary>
		/// Default MaxStringContentLength is 8k, and throws an exception when reached
		/// </summary>
		private readonly XmlDictionaryReaderQuotas quotas;

		public static DataContractDeserializer Instance 
			= new DataContractDeserializer(new XmlDictionaryReaderQuotas {
				MaxStringContentLength = 1024 * 1024,
			});

		public DataContractDeserializer(XmlDictionaryReaderQuotas quotas)
		{
			this.quotas = quotas;
		}

		public To Parse<To>(string xml)
		{
			var type = typeof(To);
			return (To)Parse(xml, type);
		}

		public object Parse(string xml, Type type)
		{
			try
			{
				var bytes = Encoding.UTF8.GetBytes(xml);

				using (var reader = XmlDictionaryReader.CreateTextReader(bytes, this.quotas))
				{
					var serializer = new System.Runtime.Serialization.DataContractSerializer(type);
					return serializer.ReadObject(reader);
				}
			}
			catch (Exception ex)
			{
				throw new SerializationException("DeserializeDataContract: Error converting type: " + ex.Message, ex);
			}
		}
	}
	#endregion

}
