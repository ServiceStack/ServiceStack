using System.Runtime.Serialization;

namespace RedisWebServices.ServiceModel.Types
{
	[DataContract]
	public class ItemWithScore
	{
		public ItemWithScore()
		{
		}

		public ItemWithScore(string item, double score)
		{
			Item = item;
			Score = score;
		}

		[DataMember]
		public string Item { get; set; }

		[DataMember]
		public double Score { get; set; }

		public bool Equals(ItemWithScore other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(other.Item, Item) && other.Score == Score;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (ItemWithScore)) return false;
			return Equals((ItemWithScore) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((Item != null ? Item.GetHashCode() : 0)*397) ^ Score.GetHashCode();
			}
		}
	}
}