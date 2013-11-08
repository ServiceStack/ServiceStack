namespace ServiceStack.WebHost.Endpoints.Tests
{
	public class Config
	{
	    public const string ServiceStackBaseUri = "http://localhost:20000";
        public const string AbsoluteBaseUri = ServiceStackBaseUri + "/";
        public const string ListeningOn = ServiceStackBaseUri + "/";

        public static void RegisterLicense()
        {
            Licensing.RegisterLicense("1001-e1JlZjoxMDAxLE5hbWU6VGVzdCBCdXNpbmVzcyxUeXBlOkJ1c2luZXNzLEhhc2g6T3dIRSt5T1FOV2NmYzZWWGpXd09QN3Y3L3Z2a09vWFUrN0FZUm5RZis1bXNTenpkejFSQnJUUzltQXZHNkV1SjVUTTMzR2JhZXZ5OENVQ08rRXZoSTlmeXo5SGt6bm5HekVmakh5U3VXM3JQc1ZmVHRqclJzTVZBOWYrNGMyUk1wUnlHOWVWRmVUR3lodXpvQ1JCODdlRXZDWTc4K0hLUkxpNHd3ZUNTakFrPSxFeHBpcnk6MjAxNC0wMS0wMX0=");
        }
	}
}