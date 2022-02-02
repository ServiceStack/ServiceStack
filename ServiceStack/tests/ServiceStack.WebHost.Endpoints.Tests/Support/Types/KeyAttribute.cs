using System;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Types
{
	/// <summary>
	/// An example of a custom / domain attribute that could be applied to properties of a POCO / Request DTO.
	/// If this type is added to the <typeparamref name="RouteInferenceStrategies"/><code>.AttributesToMatch</code> collection, 
	/// calling <typeparamref name="IServiceRoutes"/><code>.AddFromAssembly()</code> will allow
	/// <typeparamref name="RouteInferenceStrategies.FromAttributes"/> to infer a custom route if its included in the 
	/// <typeparamref name="RouteInferenceStrategies"/> available at runtime.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class KeyAttribute : AttributeBase
	{

	}
}
