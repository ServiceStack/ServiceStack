using System.Collections.Generic;
using System.Linq;
using RedisWebServices.ServiceModel.Operations.Common;
using RedisWebServices.ServiceModel.Types;

namespace RedisWebServices.ServiceInterface.Common
{
	public class SearchKeysGroupService
		: RedisServiceBase<SearchKeysGroup>
	{
		protected override object Run(SearchKeysGroup request)
		{
			var keys = RedisExec(r => r.SearchKeys(request.Pattern));
			var keysMap = new Dictionary<string, int>();

			var baseKeyGroup = request.Pattern.TrimEnd('*');

			foreach (var key in keys)
			{
				var keyGroup = key;
				if (key.IndexOf(baseKeyGroup) != -1)
				{
					var nextPos = key.IndexOf(':', baseKeyGroup.Length);
					if (nextPos != -1)
					{
						keyGroup = key.Substring(0, nextPos);
					}
				}

				int keyGroupCount;
				if (!keysMap.TryGetValue(keyGroup, out keyGroupCount))
				{
					keyGroupCount = 0;
				}
				keysMap[keyGroup] = ++keyGroupCount;
			}

			var response = new SearchKeysGroupResponse();
			foreach (var keyGroup in keysMap.OrderBy(x => x.Key))
			{
				response.KeyGroups.Add(new ItemWithScore(keyGroup.Key, keyGroup.Value));
			}

			return response;
		}
	}
}