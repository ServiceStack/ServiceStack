using System.Runtime.Serialization;

namespace RedisWebServices.ServiceModel.Types
{
	[DataContract]
	public class KeyValuePair
	{
		public KeyValuePair()
		{
		}

		public KeyValuePair(string item, string value)
		{
			Key = item;
			Value = value;
		}

		[DataMember]
		public string Key { get; set; }

		[DataMember]
		public string Value { get; set; }

		public bool Equals(KeyValuePair other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(other.Key, Key) && Equals(other.Value, Value);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (KeyValuePair)) return false;
			return Equals((KeyValuePair) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((Key != null ? Key.GetHashCode() : 0)*397) ^ (Value != null ? Value.GetHashCode() : 0);
			}
		}
	}
}