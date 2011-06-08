using System;

namespace ServiceStack.WebHost.EndPoints.Support.Markdown
{
	public interface IExpirable
	{
		DateTime? LastModified { get; }
	}
}