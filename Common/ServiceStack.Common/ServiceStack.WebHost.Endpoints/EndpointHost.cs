using System;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.WebHost.Endpoints.Metadata;

namespace ServiceStack.WebHost.Endpoints
{
	public class EndpointHost
	{
		public static ServiceOperations ServiceOperations { get; private set; }
		public static ServiceOperations AllServiceOperations { get; private set; }

		private static EndpointHostConfig config;

		public static EndpointHostConfig Config
		{
			internal get
			{
				return config;
			}
			set
			{
				var requiredFields = new Dictionary<string, object> {
             		{"ServiceName", value.ServiceName},
             		{"ServiceHost", value.ServiceHost},
             		{"ServiceController", value.ServiceController},
             	};

				var fieldsNotProvided = new List<string>();
				foreach (var requiredField in requiredFields)
				{
					if (requiredField.Value == null)
					{
						fieldsNotProvided.Add(requiredField.Key);
					}
				}

				if (fieldsNotProvided.Count > 0)
				{
					throw new ArgumentException("'{0}' are required fields", string.Join(", ", fieldsNotProvided.ToArray()));
				}

				ServiceOperations = new ServiceOperations(value.ServiceController.OperationTypes);
				AllServiceOperations = new ServiceOperations(value.ServiceController.AllOperationTypes);

				config = value;
			}
		}

		internal static object ExecuteService(object request)
		{
			AssertConfig();
			return Config.ServiceHost.ExecuteService(request);
		}

		internal static string ExecuteXmlService(string xmlRequest)
		{
			AssertConfig();
			return Config.ServiceHost.ExecuteXmlService(xmlRequest);
		}

		private static void AssertConfig()
		{
			if (Config == null) throw new ArgumentNullException("Config");
		}
	}
}