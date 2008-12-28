namespace ServiceStack.ServiceInterface
{
	public enum MessagingRestriction
	{
		AsyncOneWay,
		SyncReply,
		Public,
		Secure,
		Soap11,
		Soap12,
		Xml,
		Json,
		HttpGet,
		HttpPost,
		RestXml = HttpGet & Xml,
		RestJson = HttpGet & Json,
		Rest = RestXml | RestJson,
	}
}