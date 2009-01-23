namespace ServiceStack.ServiceInterface
{
	public enum MessagingRestriction
	{
		AsyncOneWay = 1,
		SyncReply   = 2,
		Public      = 4,
		Secure      = 8,
		Soap11      = 16,
		Soap12      = 32,
		Xml         = 64,
		Json        = 128,
		HttpGet     = 256,
		HttpPost    = 512,
		Internal    = 1024,
		External    = 2048,
		RestXml = HttpGet & Xml,
		RestJson = HttpGet & Json,
		Rest = RestXml | RestJson,
	}
}