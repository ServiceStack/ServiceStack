using System;

namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// Used to decorate Request DTO's to associate a RESTful request path mapping with a service
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public class RestServiceAttribute
		: Attribute
	{
		public RestServiceAttribute(string path)
			: this(path, null, null)
		{
		}

		public RestServiceAttribute(string path, string verbs)
			: this(path, verbs, null)
		{
		}

		public RestServiceAttribute(string path, string verbs, string defaultContentType)
		{
			Path = path;
			Verbs = verbs;
			DefaultContentType = defaultContentType;
		}

		public string Path { get; set; }

		public string Verbs { get; set; }

		public string DefaultContentType { get; set; }
	}

}