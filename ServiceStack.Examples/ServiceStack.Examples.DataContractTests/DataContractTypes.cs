using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Examples.DataContractTests
{

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
}
