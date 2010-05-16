using System.Collections.Generic;
using RedisWebServices.ServiceModel.Types;

namespace RedisWebServices.ServiceInterface
{
	public static class DtoExtensions
	{
		public static List<ItemWithScore> ToItemsWithScores(this IDictionary<string, double> itemsWithScores)
		{
			var to = new List<ItemWithScore>();
			foreach (var itemWithScore in itemsWithScores)
			{
				to.Add(new ItemWithScore(itemWithScore.Key, itemWithScore.Value));
			}
			return to;
		}
	}
}