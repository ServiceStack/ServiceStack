namespace ServiceStack.Redis
{
	public enum RedisKeyType
	{
		None, 
		String, 
		List, 
		Set,
		SortedSet,
		Hash
	}
}