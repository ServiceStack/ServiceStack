using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.WebHost.IntegrationTests.Tests;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	public class ContentManagerOnly
	{
		public string Name { get; set; }
	}

	public class ContentManagerOnlyResponse : IHasResponseStatus
	{
		public string Result { get; set; }
		public ResponseStatus ResponseStatus { get; set; }
	}

	[RequiredRole(ManageRolesTests.ContentManager)]
	public class ContentManagerOnlyService : ServiceBase<ContentManagerOnly>
	{
		protected override object Run(ContentManagerOnly request)
		{
			return new ContentManagerOnlyResponse { Result = "Haz Access" };
		}
	}

	public class ContentPermissionOnly
	{
		public string Name { get; set; }
	}

	public class ContentPermissionOnlyResponse : IHasResponseStatus
	{
		public string Result { get; set; }
		public ResponseStatus ResponseStatus { get; set; }
	}

	[RequiredPermission(ManageRolesTests.ContentPermission)]
	public class ContentPermissionOnlyService : ServiceBase<ContentPermissionOnly>
	{
		protected override object Run(ContentPermissionOnly request)
		{
			return new ContentPermissionOnlyResponse { Result = "Haz Access" };
		}
	}
}