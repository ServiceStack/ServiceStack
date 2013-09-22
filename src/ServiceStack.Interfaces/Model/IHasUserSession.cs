//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;

namespace ServiceStack.Model
{
	public interface IHasUserSession
	{
		Guid UserId { get; }

		Guid SessionId { get; }
	}
}