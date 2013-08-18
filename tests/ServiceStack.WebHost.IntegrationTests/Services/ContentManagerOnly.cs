using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.WebHost.IntegrationTests.Tests;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    public class ContentManagerOnly : IReturn<ContentManagerOnlyResponse>
	{
		public string Name { get; set; }
	}

	public class ContentManagerOnlyResponse : IHasResponseStatus
	{
		public string Result { get; set; }
		public ResponseStatus ResponseStatus { get; set; }
	}

	[RequiredRole(ManageRolesTests.ContentManager)]
	public class ContentManagerOnlyService : ServiceInterface.Service
	{
        public object Any(ContentManagerOnly request)
		{
			return new ContentManagerOnlyResponse { Result = "Haz Access" };
		}
	}

    public class ContentPermissionOnly : IReturn<ContentPermissionOnlyResponse>
	{
		public string Name { get; set; }
	}

	public class ContentPermissionOnlyResponse : IHasResponseStatus
	{
		public string Result { get; set; }
		public ResponseStatus ResponseStatus { get; set; }
	}

	[RequiredPermission(ManageRolesTests.ContentPermission)]
	public class ContentPermissionOnlyService : ServiceInterface.Service
	{
        public object Any(ContentPermissionOnly request)
		{
			return new ContentPermissionOnlyResponse { Result = "Haz Access" };
		}
	}
}