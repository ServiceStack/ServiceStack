using System;
using System.Collections.Generic;
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

	#region DataContract Types
	[DataContract(Namespace = Program.Namespace)]
	public class OneStringField
	{
		[DataMember]
		public string StringField { get; set; }
	}

	[DataContract(Namespace = Program.Namespace)]
	public class OneGuidField
	{
		[DataMember]
		public Guid GuidField { get; set; }
	}

	[DataContract(Namespace = Program.Namespace)]
	public class OneDateTimeField
	{
		[DataMember]
		public DateTime DateTimeField { get; set; }
	}

	[DataContract(Namespace = Program.Namespace)]
	public class OneDoubleField
	{
		[DataMember]
		public double DoubleField { get; set; }
	}

	[DataContract(Namespace = Program.Namespace)]
	public class OneDecimalField
	{
		[DataMember]
		public decimal DecimalField { get; set; }
	}

	[DataContract(Namespace = Program.Namespace)]
	public class ThreeStringFields
	{
		[DataMember]
		public string StringField1 { get; set; }

		[DataMember]
		public string StringField2 { get; set; }

		[DataMember]
		public string StringField3 { get; set; }
	}

	[DataContract(Namespace = Program.Namespace)]
	public class ThreeStringFieldsUnOrdered
	{
		[DataMember]
		public string StringField3 { get; set; }

		[DataMember]
		public string StringField1 { get; set; }

		[DataMember]
		public string StringField2 { get; set; }
	}

	[DataContract(Namespace = Program.Namespace)]
	public class ThreeStringFieldsOrdered
	{
		[DataMember(Order = 1)]
		public string StringField3 { get; set; }

		[DataMember(Order = 2)]
		public string StringField1 { get; set; }

		[DataMember(Order = 3)]
		public string StringField2 { get; set; }
	}

	[DataContract(Namespace = Program.Namespace)]
	public class DifferentValueTypesOrdered
	{
		[DataMember(Order = 1)]
		public string StringField { get; set; }

		[DataMember(Order = 2)]
		public Guid Guid { get; set; }

		[DataMember(Order = 3)]
		public DateTime DateTime { get; set; }

		[DataMember(Order = 4)]
		public double Double { get; set; }

		[DataMember(Order = 5)]
		public decimal Decimal { get; set; }
	}

	[CollectionDataContract(Namespace = Program.Namespace, ItemName = "string")]
	public class ArrayOfString : List<string>
	{
	}

	[DataContract(Namespace = Program.Namespace)]
	public class OneArrayOfStringField
	{
		public OneArrayOfStringField()
		{
			ArrayOfStrings = new ArrayOfString();
		}

		[DataMember(Order = 1)]
		public ArrayOfString ArrayOfStrings { get; set; }
	}

	[CollectionDataContract(Namespace = Program.Namespace, ItemName = "Property")]
	public class Properties : List<Property>
	{
		[DataMember(Order = 1)]
		public Property Property { get; set; }
	}

	[DataContract(Namespace = Program.Namespace)]
	public class Property
	{
		[DataMember(Order = 1)]
		public string Name { get; set; }

		[DataMember(Order = 2)]
		public string Value { get; set; }
	}

	[DataContract(Namespace = Program.Namespace)]
	public class OnePropertiesField
	{
		public OnePropertiesField()
		{
			Properties = new Properties();
		}

		[DataMember(Order = 1)]
		public Properties Properties { get; set; }
	}

	#endregion



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
