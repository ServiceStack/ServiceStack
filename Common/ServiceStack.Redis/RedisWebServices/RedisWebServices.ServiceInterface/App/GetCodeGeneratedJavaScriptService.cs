using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using RedisWebServices.ServiceModel.Operations.App;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace RedisWebServices.ServiceInterface.App
{
	public class GetCodeGeneratedJavaScriptService
		: RedisServiceBase<GetCodeGeneratedJavaScript>
	{
		protected override object Run(GetCodeGeneratedJavaScript request)
		{
			var sb = new StringBuilder();

			sb.Append(@"function RedisGateway(baseUri) {
RedisGateway.$baseConstructor.call(this);

    this.gateway = new JsonServiceClient(baseUri);
}
RedisGateway.errorFn = function() {
};
RedisGateway.extend(AjaxStack.ASObject, { type: 'AjaxStack.RedisGateway' },
{");

			var allOperationTypes = EndpointHost.AllServiceOperations.AllOperations.Types;
			for (int opIndex = 0; opIndex < allOperationTypes.Count; opIndex++)
			{
				var operationType = allOperationTypes[opIndex];

				var operationName = operationType.Name;
			
				if (operationName.EndsWith(ResponseDtoSuffix)) continue;

				var camelCaseOperation = GetCamelCaseName(operationName);

				sb.AppendFormat("\t{0}: function(", camelCaseOperation);

				var args = new List<string>();
				foreach (var property in operationType.GetPublicProperties())
				{
					args.Add(property.Name);
					sb.AppendFormat("{0}, ", GetCamelCaseName(property.Name));
				}

				sb.AppendLine("onSuccessFn, onErrorFn)");
				sb.AppendLine("\t{");
				sb.AppendFormat("\t\tthis.gateway.getFromJsonService('{0}', {{ ", operationName);
				
				for (int argIndex = 0; argIndex < args.Count; argIndex++)
				{
					var arg = args[argIndex];
					sb.AppendFormat("{0}: {1}", arg, GetCamelCaseName(arg));

					if (argIndex != args.Count - 1)
						sb.Append(", ");
				}

				sb.AppendLine(" },");
				sb.AppendLine("\t\t\tfunction(r)");
				sb.AppendLine("\t\t\t{");
				
				var responseType = GetResponseType(operationType);
				if (responseType != null)
				{
					var resultPropertyType = responseType.GetPublicProperties()
						.Where(x => x.PropertyType != typeof(ResponseStatus)).FirstOrDefault();

					if (resultPropertyType != null)
					{
						sb.AppendFormat("\t\t\t\tif (onSuccessFn) onSuccessFn(r.getResult().{0});", 
							resultPropertyType.Name);
						sb.AppendLine();
					}
					else
					{
						sb.AppendLine("\t\t\t\tif (onSuccessFn) onSuccessFn();");
					}
				}

				sb.AppendLine("\t\t\t},");
				sb.AppendLine("\t\t\tonErrorFn || RedisGateway.errorFn);");
				sb.Append("\t}");

				if (opIndex != allOperationTypes.Count - 1)
					sb.AppendLine(",");
			}

			sb.Append("});");

			return new TextResult(sb, MimeTypes.JavaScript);
		}

		public Type GetResponseType(Type requestType)
		{
			var responseTypeName = requestType.FullName + ResponseDtoSuffix;
			var responseDtoType = typeof(GetCodeGeneratedJavaScript).Assembly.GetType(responseTypeName);

			return responseDtoType;
		}

		private static string GetCamelCaseName(string operationName)
		{
			var camelCaseName = operationName[0].ToString().ToLower() + operationName.Substring(1);
			return camelCaseName;
		}
	}
}