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

			sb.Append(@"function RedisClient(baseUri) {
RedisClient.$baseConstructor.call(this);

    this.gateway = new JsonServiceClient(baseUri);
}
RedisClient.errorFn = function() {
};
RedisClient.getLexicalScore = function(value)
{
	if (!is.String(value)) return 0;
    
	var lexicalValue = 0;
	if (value.Length >= 1)
		lexicalValue += value[0] * Math.pow(256, 3);
	if (value.Length >= 2)
		lexicalValue += value[1] * Math.pow(256, 2);
	if (value.Length >= 3)
		lexicalValue += value[2] * Math.pow(256, 1);
	if (value.Length >= 4)
		lexicalValue += value[3];

	return lexicalValue;
};
RedisClient.convertKeyValuePairsToMap = function(kvps)
{
    var to = {};
    for (var i = 0; i < kvps.length; i++)
    {
        var kvp = kvps[i];
        to[kvp['Key']] = kvp['Value'];
    }
    return to;
};
RedisClient.convertMapToKeyValuePairs = function(map)
{
    var kvps = [];
    for (var k in map)
    {
        kvps.push({Key:k, Value:map[k]});
    }
    return kvps;
};
RedisClient.convertMapToKeyValuePairsDto = function(map)
{
    var kvps = RedisClient.convertMapToKeyValuePairs(map);
    var s = '';
    for (var i=0; i<kvps.length; i++)
    {
        var kvp = kvps[i];
        if (s) s+= ',';
        s+= '{Key:' + kvp.Key + ',Value:' + kvp.Value + '}';
    }
    return '[' + s + ']';
};

RedisClient.extend(AjaxStack.ASObject, { type: 'AjaxStack.RedisClient' },
{
");
			var isFirst = true;

			var allOperationTypes = EndpointHost.AllServiceOperations.AllOperations.Types;
			for (int opIndex = 0; opIndex < allOperationTypes.Count; opIndex++)
			{
				var operationType = allOperationTypes[opIndex];

				var operationName = operationType.Name;
			
				if (operationName.EndsWith(ResponseDtoSuffix)) continue;

				if (!isFirst)
					sb.AppendLine(",");
				else
					isFirst = false;

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
				sb.AppendFormat("\t\tthis.gateway.getFromService('{0}', {{ ", operationName);
				
				for (int argIndex = 0; argIndex < args.Count; argIndex++)
				{
					var arg = args[argIndex];
					sb.AppendFormat("{0}: {1} || null", arg, GetCamelCaseName(arg));

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
				sb.AppendLine("\t\t\tonErrorFn || RedisClient.errorFn);");
				sb.Append("\t}");
			}

			sb.AppendLine();
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