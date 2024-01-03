using System;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class ServiceHostTestBase
{
	public void ShouldThrow<T>(Action action)
		where T : Exception
	{
		ShouldThrow<T>(action, "Should Throw");
	}

	public void ShouldThrow<T>(Action action, string errorMessageIfNotThrows)
		where T : Exception
	{
		try
		{
			action();
		}
		catch (T)
		{
			return;
		}
		Assert.Fail(errorMessageIfNotThrows);
	}

	public void ShouldNotThrow<T>(Action action)
		where T : Exception
	{
		ShouldNotThrow<T>(action, "Should not Throw");
	}
	
	public void ShouldNotThrow<T>(Action action, string errorMessageIfThrows)
		where T : Exception
	{
		try
		{
			action();
		}
		catch (T)
		{
			Assert.Fail(errorMessageIfThrows);
		}
	}

}