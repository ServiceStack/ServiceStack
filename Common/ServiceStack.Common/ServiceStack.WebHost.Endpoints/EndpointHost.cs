using System;
using System.Collections.Generic;
using System.Reflection;

namespace ServiceStack.WebHost.Endpoints
{
	public class EndpointHost
	{
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
             		{"ServiceModelFinder", value.ServiceModelFinder},
             		{"OperationsNamespace", value.OperationsNamespace},
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

				config = value;
			}
		}

		public static string GetOperationTypeFullName(string operationTypeName)
		{
			AssertConfig();
			return Config.OperationsNamespace + "." + operationTypeName;
		}

		public static Type GetOperationType(string operationTypeName)
		{
			return ServiceModelAssembly.GetType(Config.OperationsNamespace + "." + operationTypeName);
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

		internal static Assembly ServiceModelAssembly
		{
			get
			{
				AssertConfig();
				return Config.ServiceModelFinder.GetType().Assembly;
			}
		}

		private static void AssertConfig()
		{
			if (Config == null) throw new ArgumentNullException("Config");
		}
	}
}