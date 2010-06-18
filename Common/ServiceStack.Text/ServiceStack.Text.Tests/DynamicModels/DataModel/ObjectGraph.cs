using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace ServiceStack.Text.Tests.DynamicModels.DataModel
{
	[Serializable]
	public class ObjectGraph : ISerializable
	{
		private readonly CustomCollection internalCollection;

		public ObjectGraph()
		{
			internalCollection = new CustomCollection();
		}

		protected ObjectGraph(SerializationInfo info, StreamingContext context)
		{
			internalCollection = (CustomCollection)info.GetValue("col", typeof(CustomCollection));
			Data = (DataContainer)info.GetValue("data", typeof(DataContainer));
		}

		public CustomCollection MyCollection
		{
			get { return internalCollection; }
		}

		public Uri AddressUri
		{
			get { return internalCollection.AddressUri; }
			set { internalCollection.AddressUri = value; }
		}

		public Type SomeType
		{
			get { return internalCollection.SomeType; }
			set { internalCollection.SomeType = value; }
		}

		public int IntValue
		{
			get { return internalCollection.IntValue; }
			set { internalCollection.IntValue = value; }
		}

		public DataContainer Data { get; set; }

		#region ISerializable Members

		[SecurityPermission(SecurityAction.LinkDemand, SerializationFormatter = true)]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("col", internalCollection);
			info.AddValue("data", Data);
		}

		#endregion
	}
}