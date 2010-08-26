using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using RedisWebServices.ServiceModel.Operations.App;
using RedisWebServices.ServiceModel.Types;
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

			sb.Append(@"goog.provide(""RedisClient"");
goog.require(""JsonServiceClient"");

/**
 * The Redis ajax service client gateway
 * @param {string} the baseUri of ServiceStack web services.
 * @constructor
 */
RedisClient = function(baseUri) {
    var baseUri = document.location.href.indexOf('#') != -1
        ? document.location.href.substr(0, document.location.href.indexOf('#'))
        : document.location.href;
    baseUri = baseUri.replace('default.htm','').replace('AjaxClient', 'Public');
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
RedisClient.toKeyValuePairsDto = function(kvps)
{
    var s = '';
    for (var i=0; i<kvps.length; i++)
    {
        var kvp = kvps[i];
        if (s) s+= ',';
        s+= '{Key:' + kvp.Key + ',Value:' + kvp.Value + '}';
    }
    return '[' + s + ']';
};
RedisClient.convertMapToKeyValuePairsDto = function(map)
{
    var kvps = RedisClient.convertMapToKeyValuePairs(map);
	return RedisClient.toKeyValuePairsDto(kvps);
};
RedisClient.convertItemWithScoresToMap = function(itwss)
{
    var to = {};
    for (var i = 0; i < itwss.length; i++)
    {
        var itws = itwss[i];
        to[itws['Item']] = itws['Score'];
    }
    return to;
};
RedisClient.convertToItemWithScoresDto = function(obj)
{
    var s = '';
    var isArray = obj.length !== undefined;
    if (isArray)
    {
        for (var i=0, len=obj.length; i < len; i++)
        {
            if (s) s+= ',';
            s+= '{Item:' + obj[i] + ',Score:0}';
        }
        return '[' + s + ']';
    }
    for (var item in obj)
    {
        if (s) s+= ',';
        s+= '{Item:' + item + ',Score:' + obj[item] + '}';
    }
    return '[' + s + ']';
};

RedisClient.prototype =
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

				var args = operationType.GetPublicProperties();
				foreach (var property in args)
				{
					sb.AppendFormat("{0}, ", GetCamelCaseName(property.Name));
				}

				sb.AppendLine("onSuccessFn, onErrorFn)");
				sb.AppendLine("\t{");

				var hasCollectionArg = false;
				var sbArgs = new StringBuilder();
				for (int argIndex = 0; argIndex < args.Length; argIndex++)
				{
					var arg = args[argIndex];

					var isCollection = arg.PropertyType.FindInterfaces((x, y) => x == typeof(ICollection), null).Length > 0;
					if (isCollection)
						hasCollectionArg = true;

					sbArgs.Append(GetValidArgument(arg));

					if (argIndex != args.Length - 1)
						sbArgs.Append(", ");
				}

				if (hasCollectionArg)
					sb.AppendFormat("\t\tthis.gateway.postToService('{0}', {{ ", operationName);
				else
					sb.AppendFormat("\t\tthis.gateway.getFromService('{0}', {{ ", operationName);

				sb.Append(sbArgs.ToString());

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

						sb.AppendFormat("\t\t\t\tif (onSuccessFn) onSuccessFn({0});",
							GetValidReturnValue(resultPropertyType));
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
			sb.Append("};");
			sb.AppendLine("goog.exportSymbol(\"RedisClient\", RedisClient);");

			return new TextResult(sb, MimeTypes.JavaScript);
		}

		public string GetValidArgument(PropertyInfo propertyInfo)
		{
			//"{0}: {1} || null", arg, GetCamelCaseName(arg.Name)
			var arg = propertyInfo.Name;
			if (propertyInfo.PropertyType.IsNumericType())
			{
				//JSON serializer treats 0 as undefined so need to put it in quotes
				return string.Format("{0}: {1} || '0'", arg, GetCamelCaseName(arg));
			}
			if (propertyInfo.PropertyType.IsAssignableFrom(typeof(List<KeyValuePair>)))
			{
				return string.Format("{0}: RedisClient.convertMapToKeyValuePairsDto({1} || {{}})", arg, GetCamelCaseName(arg));
			}
			if (propertyInfo.PropertyType.IsAssignableFrom(typeof(List<ItemWithScore>)))
			{
				return string.Format("{0}: RedisClient.convertToItemWithScoresDto({1} || {{}})", arg, GetCamelCaseName(arg));
			}

			return string.Format("{0}: {1} || null", arg, GetCamelCaseName(arg));
		}

		public string GetValidReturnValue(PropertyInfo resultPropertyType)
		{
			var rawValue = "r." + resultPropertyType.Name;

			if (typeof(List<KeyValuePair>).IsAssignableFrom(resultPropertyType.PropertyType))
			{
				return string.Format("RedisClient.convertKeyValuePairsToMap({0})", rawValue);
			}
			if (typeof(List<ItemWithScore>).IsAssignableFrom(resultPropertyType.PropertyType))
			{
				return string.Format("RedisClient.convertItemWithScoresToMap({0})", rawValue);
			}
			return rawValue;
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