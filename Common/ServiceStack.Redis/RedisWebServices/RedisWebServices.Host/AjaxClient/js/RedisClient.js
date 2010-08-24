function RedisClient(baseUri) {
   var baseUri = baseUri || 'http://' + document.location.hostname + '/RedisWebServices.Host/Public/';
   this.gateway = new JsonServiceClient(baseUri);
}
RedisClient.errorFn = function() {
};
RedisClient.getLexicalScore = function(value)
{
	if (!is.String(value)) return 0;

	var lexicalValue = 0;
	if (value.Length >= 1)
		lexicalValue += value[0] * Math.pow(256, 3);
	if (value.Length >= 2)
		lexicalValue += value[1] * Math.pow(256, 2);
	if (value.Length >= 3)
		lexicalValue += value[2] * Math.pow(256, 1);
	if (value.Length >= 4)
		lexicalValue += value[3];

	return lexicalValue;
};
RedisClient.convertKeyValuePairsToMap = function(kvps)
{
    var to = {};
    for (var i = 0; i < kvps.length; i++)
    {
        var kvp = kvps[i];
        to[kvp['Key']] = kvp['Value'];
    }
    return to;
};
RedisClient.convertMapToKeyValuePairs = function(map)
{
    var kvps = [];
    for (var k in map)
    {
        kvps.push({Key:k, Value:map[k]});
    }
    return kvps;
};
RedisClient.toKeyValuePairsDto = function(kvps)
{
    var s = '';
    for (var i=0; i<kvps.length; i++)
    {
        var kvp = kvps[i];
        if (s) s+= ',';
        s+= '{Key:' + kvp.Key + ',Value:' + kvp.Value + '}';
    }
    return '[' + s + ']';
};
RedisClient.convertMapToKeyValuePairsDto = function(map)
{
    var kvps = RedisClient.convertMapToKeyValuePairs(map);
	return RedisClient.toKeyValuePairsDto(kvps);
};
RedisClient.convertItemWithScoresToMap = function(itwss)
{
    var to = {};
    for (var i = 0; i < itwss.length; i++)
    {
        var itws = itwss[i];
        to[itws['Item']] = itws['Score'];
    }
    return to;
};
RedisClient.convertToItemWithScoresDto = function(obj)
{
    var s = '';
    var isArray = obj.length !== undefined;
    if (isArray)
    {
        for (var i=0, len=obj.length; i < len; i++)
        {
            if (s) s+= ',';
            s+= '{Item:' + obj[i] + ',Score:0}';
        }
        return '[' + s + ']';
    }
    for (var item in obj)
    {
        if (s) s+= ',';
        s+= '{Item:' + item + ',Score:' + obj[item] + '}';
    }
    return '[' + s + ']';
};

RedisClient.prototype =
{
	removeItemFromSortedSet: function(id, item, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('RemoveItemFromSortedSet', { Id: id || null, Item: item || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Result);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getIntersectFromSets: function(setIds, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('GetIntersectFromSets', { SetIds: setIds || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Items);
			},
			onErrorFn || RedisClient.errorFn);
	},
	publishMessage: function(toChannel, message, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('PublishMessage', { ToChannel: toChannel || null, Message: message || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	getListCount: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetListCount', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Count);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getCodeGeneratedJavaScript: function(text, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetCodeGeneratedJavaScript', { Text: text || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.JavaScript);
			},
			onErrorFn || RedisClient.errorFn);
	},
	flushAll: function(onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('FlushAll', {  },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	storeDifferencesFromSet: function(id, fromSetId, setIds, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('StoreDifferencesFromSet', { Id: id || null, FromSetId: fromSetId || null, SetIds: setIds || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	getHashCount: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetHashCount', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Count);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getSubstring: function(key, fromIndex, toIndex, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetSubstring', { Key: key || null, FromIndex: fromIndex || null, ToIndex: toIndex || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Value);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getItemIndexInSortedSet: function(id, item, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetItemIndexInSortedSet', { Id: id || null, Item: item || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Index);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getRangeWithScoresFromSortedSet: function(id, fromRank, toRank, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetRangeWithScoresFromSortedSet', { Id: id || null, FromRank: fromRank || null, ToRank: toRank || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(RedisClient.convertItemWithScoresToMap(r.ItemsWithScores));
			},
			onErrorFn || RedisClient.errorFn);
	},
	popItemFromSet: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('PopItemFromSet', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Item);
			},
			onErrorFn || RedisClient.errorFn);
	},
	enqueueItemOnList: function(id, item, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('EnqueueItemOnList', { Id: id || null, Item: item || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	incrementValueInHash: function(id, key, incrementBy, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('IncrementValueInHash', { Id: id || null, Key: key || null, IncrementBy: incrementBy || '0' },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Value);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getRangeFromSortedSetByHighestScore: function(id, fromScore, toScore, skip, take, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetRangeFromSortedSetByHighestScore', { Id: id || null, FromScore: fromScore || null, ToScore: toScore || null, Skip: skip || '0', Take: take || '0' },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Items);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getRangeFromSortedSet: function(id, fromRank, toRank, sortDescending, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetRangeFromSortedSet', { Id: id || null, FromRank: fromRank || null, ToRank: toRank || null, SortDescending: sortDescending || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Items);
			},
			onErrorFn || RedisClient.errorFn);
	},
	addItemToSortedSet: function(id, item, score, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('AddItemToSortedSet', { Id: id || null, Item: item || null, Score: score || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Result);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getRangeFromList: function(id, startingFrom, endingAt, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetRangeFromList', { Id: id || null, StartingFrom: startingFrom || null, EndingAt: endingAt || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Items);
			},
			onErrorFn || RedisClient.errorFn);
	},
	blockingRemoveStartFromList: function(id, timeOut, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('BlockingRemoveStartFromList', { Id: id || null, TimeOut: timeOut || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Item);
			},
			onErrorFn || RedisClient.errorFn);
	},
	setEntryInHashIfNotExists: function(id, key, value, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('SetEntryInHashIfNotExists', { Id: id || null, Key: key || null, Value: value || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Result);
			},
			onErrorFn || RedisClient.errorFn);
	},
	searchKeys: function(pattern, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('SearchKeys', { Pattern: pattern || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Keys);
			},
			onErrorFn || RedisClient.errorFn);
	},
	setEntry: function(key, value, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('SetEntry', { Key: key || null, Value: value || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	shutdown: function(onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('Shutdown', {  },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	getRangeFromSortedSetByLowestScore: function(id, fromScore, toScore, skip, take, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetRangeFromSortedSetByLowestScore', { Id: id || null, FromScore: fromScore || null, ToScore: toScore || null, Skip: skip || '0', Take: take || '0' },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Items);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getRangeWithScoresFromSortedSetByHighestScore: function(id, fromScore, toScore, skip, take, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetRangeWithScoresFromSortedSetByHighestScore', { Id: id || null, FromScore: fromScore || null, ToScore: toScore || null, Skip: skip || '0', Take: take || '0' },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(RedisClient.convertItemWithScoresToMap(r.ItemsWithScores));
			},
			onErrorFn || RedisClient.errorFn);
	},
	storeIntersectFromSets: function(id, setIds, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('StoreIntersectFromSets', { Id: id || null, SetIds: setIds || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	moveBetweenSets: function(fromSetId, toSetId, item, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('MoveBetweenSets', { FromSetId: fromSetId || null, ToSetId: toSetId || null, Item: item || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	getUnionFromSets: function(setIds, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('GetUnionFromSets', { SetIds: setIds || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Items);
			},
			onErrorFn || RedisClient.errorFn);
	},
	removeEndFromList: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('RemoveEndFromList', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Item);
			},
			onErrorFn || RedisClient.errorFn);
	},
	popItemFromList: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('PopItemFromList', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Item);
			},
			onErrorFn || RedisClient.errorFn);
	},
	addItemToList: function(id, item, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('AddItemToList', { Id: id || null, Item: item || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	containsKey: function(key, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('ContainsKey', { Key: key || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Result);
			},
			onErrorFn || RedisClient.errorFn);
	},
	setContainsItem: function(id, item, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('SetContainsItem', { Id: id || null, Item: item || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Result);
			},
			onErrorFn || RedisClient.errorFn);
	},
	addRangeToList: function(id, items, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('AddRangeToList', { Id: id || null, Items: items || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	getValueFromHash: function(id, key, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetValueFromHash', { Id: id || null, Key: key || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Value);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getRangeWithScoresFromSortedSetByLowestScore: function(id, fromScore, toScore, skip, take, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetRangeWithScoresFromSortedSetByLowestScore', { Id: id || null, FromScore: fromScore || null, ToScore: toScore || null, Skip: skip || '0', Take: take || '0' },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(RedisClient.convertItemWithScoresToMap(r.ItemsWithScores));
			},
			onErrorFn || RedisClient.errorFn);
	},
	getAllItemsFromSortedSetDesc: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetAllItemsFromSortedSetDesc', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Items);
			},
			onErrorFn || RedisClient.errorFn);
	},
	addRangeToSortedSet: function(id, items, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('AddRangeToSortedSet', { Id: id || null, Items: RedisClient.convertToItemWithScoresDto(items || {}) },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	dequeueItemFromList: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('DequeueItemFromList', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Item);
			},
			onErrorFn || RedisClient.errorFn);
	},
	prependItemToList: function(id, item, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('PrependItemToList', { Id: id || null, Item: item || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	removeEntryFromHash: function(id, key, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('RemoveEntryFromHash', { Id: id || null, Key: key || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Result);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getHashKeys: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetHashKeys', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Keys);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getAllEntriesFromHash: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetAllEntriesFromHash', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(RedisClient.convertKeyValuePairsToMap(r.KeyValuePairs));
			},
			onErrorFn || RedisClient.errorFn);
	},
	getEntryType: function(key, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetEntryType', { Key: key || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.KeyType);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getAllKeys: function(onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetAllKeys', {  },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Keys);
			},
			onErrorFn || RedisClient.errorFn);
	},
	setEntryIfNotExists: function(key, value, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('SetEntryIfNotExists', { Key: key || null, Value: value || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Result);
			},
			onErrorFn || RedisClient.errorFn);
	},
	sortedSetContainsItem: function(id, item, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('SortedSetContainsItem', { Id: id || null, Item: item || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Result);
			},
			onErrorFn || RedisClient.errorFn);
	},
	popItemWithHighestScoreFromSortedSet: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('PopItemWithHighestScoreFromSortedSet', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Item);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getSetCount: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetSetCount', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Count);
			},
			onErrorFn || RedisClient.errorFn);
	},
	createSubscription: function(channels, patterns, timeOut, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('CreateSubscription', { Channels: channels || null, Patterns: patterns || null, TimeOut: timeOut || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Key);
			},
			onErrorFn || RedisClient.errorFn);
	},
	setItemInList: function(id, index, item, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('SetItemInList', { Id: id || null, Index: index || null, Item: item || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	rewriteAppendOnlyFileAsync: function(onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('RewriteAppendOnlyFileAsync', {  },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	save: function(inBackground, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('Save', { InBackground: inBackground || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	getAllItemsFromSortedSet: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetAllItemsFromSortedSet', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Items);
			},
			onErrorFn || RedisClient.errorFn);
	},
	popItemWithLowestScoreFromSortedSet: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('PopItemWithLowestScoreFromSortedSet', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Item);
			},
			onErrorFn || RedisClient.errorFn);
	},
	storeUnionFromSets: function(id, setIds, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('StoreUnionFromSets', { Id: id || null, SetIds: setIds || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	popAndPushItemBetweenLists: function(fromListId, toListId, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('PopAndPushItemBetweenLists', { FromListId: fromListId || null, ToListId: toListId || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Item);
			},
			onErrorFn || RedisClient.errorFn);
	},
	setRangeInHash: function(id, keyValuePairs, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('SetRangeInHash', { Id: id || null, KeyValuePairs: RedisClient.convertMapToKeyValuePairsDto(keyValuePairs || {}) },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	ping: function(onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('Ping', {  },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Result);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getValue: function(key, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetValue', { Key: key || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Value);
			},
			onErrorFn || RedisClient.errorFn);
	},
	expireEntryAt: function(key, expireAt, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('ExpireEntryAt', { Key: key || null, ExpireAt: expireAt || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Result);
			},
			onErrorFn || RedisClient.errorFn);
	},
	incrementItemInSortedSet: function(id, item, incrementBy, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('IncrementItemInSortedSet', { Id: id || null, Item: item || null, IncrementBy: incrementBy || '0' },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Score);
			},
			onErrorFn || RedisClient.errorFn);
	},
	removeItemFromList: function(id, item, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('RemoveItemFromList', { Id: id || null, Item: item || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.ItemsRemovedCount);
			},
			onErrorFn || RedisClient.errorFn);
	},
	setEntryInHash: function(id, key, value, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('SetEntryInHash', { Id: id || null, Key: key || null, Value: value || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Result);
			},
			onErrorFn || RedisClient.errorFn);
	},
	setEntryWithExpiry: function(key, value, expireIn, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('SetEntryWithExpiry', { Key: key || null, Value: value || null, ExpireIn: expireIn || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	getItemScoreInSortedSet: function(id, item, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetItemScoreInSortedSet', { Id: id || null, Item: item || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Score);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getAllItemsFromSet: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetAllItemsFromSet', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Items);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getRandomItemFromSet: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetRandomItemFromSet', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Item);
			},
			onErrorFn || RedisClient.errorFn);
	},
	addItemToSet: function(id, item, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('AddItemToSet', { Id: id || null, Item: item || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	removeAllFromList: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('RemoveAllFromList', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	hashContainsEntry: function(id, key, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('HashContainsEntry', { Id: id || null, Key: key || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Result);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getSortedEntryValues: function(key, startingFrom, endingAt, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetSortedEntryValues', { Key: key || null, StartingFrom: startingFrom || null, EndingAt: endingAt || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Values);
			},
			onErrorFn || RedisClient.errorFn);
	},
	slaveOf: function(host, port, noOne, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('SlaveOf', { Host: host || null, Port: port || null, NoOne: noOne || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	removeRangeFromSortedSetByScore: function(id, fromScore, toScore, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('RemoveRangeFromSortedSetByScore', { Id: id || null, FromScore: fromScore || null, ToScore: toScore || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.ItemsRemovedCount);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getRandomKey: function(onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetRandomKey', {  },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Key);
			},
			onErrorFn || RedisClient.errorFn);
	},
	decrementValue: function(key, decrementBy, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('DecrementValue', { Key: key || null, DecrementBy: decrementBy || '0' },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Value);
			},
			onErrorFn || RedisClient.errorFn);
	},
	appendToValue: function(key, value, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('AppendToValue', { Key: key || null, Value: value || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.ValueLength);
			},
			onErrorFn || RedisClient.errorFn);
	},
	pushItemToList: function(id, item, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('PushItemToList', { Id: id || null, Item: item || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	getHashValues: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetHashValues', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Values);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getAndSetEntry: function(key, value, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetAndSetEntry', { Key: key || null, Value: value || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.ExistingValue);
			},
			onErrorFn || RedisClient.errorFn);
	},
	expireEntryIn: function(key, expireIn, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('ExpireEntryIn', { Key: key || null, ExpireIn: expireIn || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Result);
			},
			onErrorFn || RedisClient.errorFn);
	},
	removeEntry: function(keys, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('RemoveEntry', { Keys: keys || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Result);
			},
			onErrorFn || RedisClient.errorFn);
	},
	flushDb: function(db, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('FlushDb', { Db: db || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	echo: function(text, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('Echo', { Text: text || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Text);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getSortedSetCount: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetSortedSetCount', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Count);
			},
			onErrorFn || RedisClient.errorFn);
	},
	removeStartFromList: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('RemoveStartFromList', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Item);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getItemFromList: function(id, index, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetItemFromList', { Id: id || null, Index: index || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Item);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getAllItemsFromList: function(id, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetAllItemsFromList', { Id: id || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Items);
			},
			onErrorFn || RedisClient.errorFn);
	},
	trimList: function(id, keepStartingFrom, keepEndingAt, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('TrimList', { Id: id || null, KeepStartingFrom: keepStartingFrom || null, KeepEndingAt: keepEndingAt || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	incrementValue: function(key, incrementBy, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('IncrementValue', { Key: key || null, IncrementBy: incrementBy || '0' },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Value);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getValues: function(keys, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('GetValues', { Keys: keys || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Values);
			},
			onErrorFn || RedisClient.errorFn);
	},
	blockingDequeueItemFromList: function(id, timeOut, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('BlockingDequeueItemFromList', { Id: id || null, TimeOut: timeOut || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Item);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getTimeToLive: function(key, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetTimeToLive', { Key: key || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.TimeRemaining);
			},
			onErrorFn || RedisClient.errorFn);
	},
	removeRangeFromSortedSet: function(id, fromRank, toRank, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('RemoveRangeFromSortedSet', { Id: id || null, FromRank: fromRank || null, ToRank: toRank || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.ItemsRemovedCount);
			},
			onErrorFn || RedisClient.errorFn);
	},
	removeItemFromSet: function(id, item, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('RemoveItemFromSet', { Id: id || null, Item: item || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	getDifferencesFromSet: function(id, setIds, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('GetDifferencesFromSet', { Id: id || null, SetIds: setIds || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Items);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getNorthwindData: function(onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetNorthwindData', {  },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Categories);
			},
			onErrorFn || RedisClient.errorFn);
	},
	storeUnionFromSortedSets: function(id, fromSetIds, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('StoreUnionFromSortedSets', { Id: id || null, FromSetIds: fromSetIds || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Count);
			},
			onErrorFn || RedisClient.errorFn);
	},
	storeIntersectFromSortedSets: function(id, fromSetIds, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('StoreIntersectFromSortedSets', { Id: id || null, FromSetIds: fromSetIds || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Count);
			},
			onErrorFn || RedisClient.errorFn);
	},
	addRangeToSet: function(id, items, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('AddRangeToSet', { Id: id || null, Items: items || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	getRangeFromSortedList: function(id, startingFrom, endingAt, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetRangeFromSortedList', { Id: id || null, StartingFrom: startingFrom || null, EndingAt: endingAt || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Items);
			},
			onErrorFn || RedisClient.errorFn);
	},
	blockingPopItemFromList: function(id, timeOut, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('BlockingPopItemFromList', { Id: id || null, TimeOut: timeOut || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Item);
			},
			onErrorFn || RedisClient.errorFn);
	},
	getValuesFromHash: function(id, keys, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('GetValuesFromHash', { Id: id || null, Keys: keys || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(r.Values);
			},
			onErrorFn || RedisClient.errorFn);
	},
	searchKeysGroup: function(pattern, onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('SearchKeysGroup', { Pattern: pattern || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(RedisClient.convertItemWithScoresToMap(r.KeyGroups));
			},
			onErrorFn || RedisClient.errorFn);
	},
	getEntryTypes: function(keys, onSuccessFn, onErrorFn)
	{
		this.gateway.postToService('GetEntryTypes', { Keys: keys || null },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(RedisClient.convertKeyValuePairsToMap(r.KeyTypes));
			},
			onErrorFn || RedisClient.errorFn);
	},
	populateRedisWithData: function(onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('PopulateRedisWithData', {  },
			function(r)
			{
				if (onSuccessFn) onSuccessFn();
			},
			onErrorFn || RedisClient.errorFn);
	},
	getServerInfo: function(onSuccessFn, onErrorFn)
	{
		this.gateway.getFromService('GetServerInfo', {  },
			function(r)
			{
				if (onSuccessFn) onSuccessFn(RedisClient.convertKeyValuePairsToMap(r.ServerInfo));
			},
			onErrorFn || RedisClient.errorFn);
	}
};