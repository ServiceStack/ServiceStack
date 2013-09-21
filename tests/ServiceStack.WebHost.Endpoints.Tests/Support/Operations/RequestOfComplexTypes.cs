using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Common;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Operations
{
	[DataContract]
	public class RequestOfComplexTypes
	{
		[DataMember]
		public List<int> IntList { get; set; }

		[DataMember]
		public List<string> StringList { get; set; }

		[DataMember]
		public int[] IntArray { get; set; }

		[DataMember]
		public string[] StringArray { get; set; }

		[DataMember]
		public Dictionary<int, int> IntMap { get; set; }

		[DataMember]
		public Dictionary<string, string> StringMap { get; set; }

		[DataMember]
		public Dictionary<string, int> StringIntMap { get; set; }

		[DataMember]
		public RequestOfAllTypes RequestOfAllTypes { get; set; }

		public static RequestOfComplexTypes Create(int i)
		{
			return new RequestOfComplexTypes {
				IntArray = new[] { i, i + 1 },
				IntList = new List<int> { i, i + 1 },
				IntMap = new Dictionary<int, int> { { i, i + 1 }, { i + 2, i + 3 } },
				StringArray = new[] { "String" + i, "String" + i + 1 },
				StringList = new List<string> { "String" + i, "String" + (i + 1) },
				StringMap = new Dictionary<string, string> { { "String" + i, "String" + "String" + i + 1 }, { "String" + i + 2, "String" + i + 3 } },
				StringIntMap = new Dictionary<string, int> { { "String" + i, i }, { "String" + i + 1, i + 1 } },
				RequestOfAllTypes = RequestOfAllTypes.Create(i),
			};
		}

		public override bool Equals(object obj)
		{
			var other = obj as RequestOfComplexTypes;
			if (other == null) return false;

			return this.IntArray.EquivalentTo(other.IntArray)
			       && this.IntList.EquivalentTo(other.IntList)
			       && this.IntMap.EquivalentTo(other.IntMap)
			       && this.StringArray.EquivalentTo(other.StringArray)
			       && this.StringList.EquivalentTo(other.StringList)
			       && this.StringIntMap.EquivalentTo(other.StringIntMap)
			       && this.RequestOfAllTypes.Equals(other.RequestOfAllTypes);
		}

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
	}
}