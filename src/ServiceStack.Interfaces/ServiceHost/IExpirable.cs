using System;

namespace ServiceStack.WebHost.Endpoints.Support.Markdown
{
	public interface IExpirable
	{
		DateTime? LastModified { get; }
	}
}