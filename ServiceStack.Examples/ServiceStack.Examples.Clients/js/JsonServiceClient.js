/*
  Helper 'ServiceClient' to talk to REST/POX JSON web services.
  Requires:
	- jQuery
	- jquery-json	
*/

function JsonServiceClient(baseUri, serviceUriComponent, type)
{
	this.baseUri = baseUri;
	this.serviceUriComponent = serviceUriComponent || "";
	this.baseServiceUri = this.baseUri + this.serviceUriComponent;

	this.sendJson = function(webMethod, request, onSuccess, onError, ajaxOptions)
	{
		var startCallTime = new Date();
		var requestUrl = this.baseServiceUri + "Public/Json/SyncReply/" + webMethod;
		var id = JsonServiceClient.id++;

		var $static = this.constructor;
		var $this = this;

		var options = {
			type: "GET",
			url: requestUrl,
			data: request,
			dataType: "json",
			success: function(response)
			{
				var endCallTime = new Date();
				var callDuration = endCallTime.getTime() - startCallTime.getTime();

				if (isJsonResponseSuccessful(response.ResponseStatus, onError))
				{

					if (onSuccess) onSuccess(new ResponseResultEvent(response));
					$static.onSuccess({ id: id, webMethod: webMethod, request: request,
						response: response, durationMs: callDuration
					});
				}
				else
				{
					$static.onError({ id: id, webMethod: webMethod, request: request,
						error: OperationResult.Parse(response.ResponseStatus),
						durationMs: callDuration
					});
				}
			},
			error: function(xhr, desc, exceptionobj)
			{
				var endCallTime = new Date();
				var callDuration = endCallTime.getTime() - startCallTime.getTime();

				try
				{
					if (onError) onError(xhr.responseText);
				}
				catch (e) { }
				$static.onError({ id: id, webMethod: webMethod, request: request,
					error: xhr.responseText, durationMs: callDuration
				});
			}
		};

		$.extend(options, ajaxOptions);

		var ajax = $.ajax(options);
	}

	//Sends a HTTP 'GET' request on the QueryString
	this.getFromJsonService = function(webMethod, request, onSuccess, onError)
	{
		this.sendJson(webMethod, request, onSuccess, onError);
	}

	//Sends a HTTP 'POST' request as key value pair formData
	this.postToJsonService = function(webMethod, request, onSuccess, onError)
	{
		this.sendJson(webMethod, request, onSuccess, onError, { type: "POST" });
	}

	//Sends a HTTP 'POST' request as JSON
	this.postJsonToService = function(webMethod, request, onSuccess, onError)
	{
		var jsonRequest = $.compactJSON(request);
		this.sendJson(webMethod, jsonRequest, onSuccess, onError,
			{ type: "POST", processData: false, contentType: "application/json; charset=utf-8" });
	}
}
window.JsonServiceClient = JsonServiceClient;
var JSN = JsonServiceClient;

JSN.UNDEFINED_NUMBER = 0;
JSN.id = 0;
JSN.onError = function(args) { };
JSN.onSuccess = function(args) { };

function isJsonResponseSuccessful(responseStatus, onError)
{
	//For simple services without a ResponseStatus, always return true.
	if (!responseStatus) return true;
	
	var result = OperationResult.Parse(responseStatus);
	if (!result.isSuccess)
	{
		if (onError == null)
		{
			/*
			new Logger("isJsonResponseSuccessful")
				.error("result.isSuccess == false: " + result.errorCode);
			*/
			return;
		}
		var errorEvent = new ResponseErrorEvent(result);
		onError(errorEvent);
		return false;
	}
	return true;
}

function OperationResult()
{
	this.isSuccess = false;
	this.message = "";
	this.errorCode = "";
	this.stackTrace = "";
	this.errors = [];

	this.toString = function()
	{
		return this.message 
			? this.errorCode + ": " + this.message
			: this.errorCode;
	}
}
OperationResult.Parse = function(responseStatus)
{
	var result = new OperationResult();
	result.isSuccess = !responseStatus.ErrorCode;
	result.errorCode = responseStatus.ErrorCode;
	result.message = responseStatus.Message;

	for (var objError in responseStatus.Errors)
	{
		var error = new ResponseError(objError.ErrorCode, objError.FieldName, objError.Message);
		result.errors.push(error);
	}

	return result;
}

function ResponseResultEvent(result)
{
	this.result = result;
}

function ResponseErrorEvent(responseStatus)
{
	$.extend(this, responseStatus);
}

function ResultEvent(result)
{
	this.result = result;
}

function ResponseError(result)
{
	this.result = result;
}

function ResponseError(errorCode, fieldName, message)
{
	this.errorCode = errorCode;
	this.fieldName = fieldName;
	this.message = message;
}



function Dto() { }

Dto.CHAR_A = "A".charCodeAt(0);
Dto.CHAR_Z = "Z".charCodeAt(0);
Dto.MULTI_FIELD_SEPERATOR = ";";

Dto.parseGuid = function(guid)
{
	return guid.toLowerCase().replace(/-/g, "");
};
Dto.parseBool = function(aBool)
{
	if (is.Boolean(aBool)) return aBool;
	if (is.String(aBool)) return aBool.toString().toLowerCase() == "true";
	return aBool;
};
Dto.toGetArray = function(array)
{
	return array ? array.join(",") : [];
};
Dto.toPostArray = function(array)
{
	return array || [];
};
Dto.toDate = function(date)
{
	return "\/Date(" + date.getTime() + "-0000)\/";
};
Dto.toNumber = function(obj)
{
	return isNaN(obj) ? 0 : obj;
};
Dto.toDto = function(obj)
{
	if (!obj || is.Function(obj)) return null;

	//If they have defined their own toDto method use that instead
	if (obj.toDto) return obj.toDto();

	if (is.Array(obj))
	{
		var arrayOfDto = [];
		$.each(obj, function(i, item)
		{
			arrayOfDto.push(Dto.toDto(item));
		});
		return arrayOfDto;
	}

	if (is.String(obj) || is.Boolean(obj)) return obj;

	if (is.Number(obj)) return Dto.toNumber(obj);

	if (is.Date(obj)) return Dto.toDate(obj);

	return Dto.cloneObjectWithPascalFields(obj);
};
Dto.cloneObjectWithPascalFields = function(obj)
{
	var clone = {};
	for (var fieldName in obj)
	{
		var firstChar = fieldName.charCodeAt(0);
		var startsWithCapital = firstChar >= Dto.CHAR_A && firstChar <= Dto.CHAR_Z;
		if (startsWithCapital)
		{
			clone[fieldName] = Dto.toDto(obj[fieldName]);
		}
	}
	return clone;
};

