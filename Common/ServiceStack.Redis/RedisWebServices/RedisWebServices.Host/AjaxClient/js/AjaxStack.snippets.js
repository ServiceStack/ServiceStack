/** @constructor */
function JsonServiceClient(baseUri, type)
{
	this.baseSyncReplyUri = Path.combine(baseUri, "Json/SyncReply");
	this.baseAsyncOneWayUri = Path.combine(baseUri, "Json/AsyncOneWay");
}
JsonServiceClient.prototype.send = function(webMethod, request, onSuccess, onError, ajaxOptions) {
	var startCallTime = new Date();
	var requestUrl = Path.combine(this.baseSyncReplyUri, webMethod);
	var id = JsonServiceClient.id++;

	var options = {
		type: "GET",
		url: requestUrl,
		data: request,
		dataType: "json",
		success: function(response)
		{
			var endCallTime = new Date();
			var callDuration = endCallTime.getTime() - startCallTime.getTime();
			if (!response)
			{
				if (onSuccess) onSuccess(null);
				return;
			}

			if (OperationResult.isResponseSuccessful(response.ResponseStatus, onError))
			{
				if (onSuccess) onSuccess(new ResultEvent(response));
				JsonServiceClient.onSuccess({ id: id, webMethod: webMethod, request: request,
					response: response, durationMs: callDuration
				});
			}
			else
			{
				JsonServiceClient.onError({ id: id, webMethod: webMethod, request: request,
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
			JsonServiceClient.onError({ id: id, webMethod: webMethod, request: request,
				error: xhr.responseText, durationMs: callDuration
			});
		}
	};

	O.merge(options, ajaxOptions);

	var ajax = $.ajax(options);
};

//Sends a HTTP 'GET' request on the QueryString
JsonServiceClient.prototype.getFromService = function(webMethod, request, onSuccess, onError) {
	this.send(webMethod, request, onSuccess, onError);
};

//Sends a HTTP 'POST' request as key value pair formData
JsonServiceClient.prototype.postFormDataToService = function(webMethod, request, onSuccess, onError) {
	this.send(webMethod, request, onSuccess, onError, { type: "POST" });
};

//Sends a HTTP 'POST' request as JSON @requires jQuery
JsonServiceClient.prototype.postToService = function(webMethod, request, onSuccess, onError) {
	var jsonRequest = $.compactJSON(request);
	this.send(webMethod, jsonRequest, onSuccess, onError, { type: "POST", processData: false, contentType: "application/json; charset=utf-8" });
};

JsonServiceClient.id = 0;
JsonServiceClient.onError = function() { };
JsonServiceClient.onSuccess = function() { };

/** @constructor */
function OperationResult()
{
	this.isSuccess = false;
	this.message = "";
	this.errorCode = "";
	this.stackTrace = "";
	this.fieldErrors = [];
	this.fieldErrorMap = {};
}
OperationResult.isResponseSuccessful = function(responseStatus, onError)
{
	//if there is no responseStatus then there is no way to work out if it was an error.
	if (!responseStatus) return true;
	
	var result = OperationResult.Parse(responseStatus);
	if (!result.isSuccess)
	{
		if (onError == null) return;
		var errorEvent = new ResponseErrorEvent(result);
		onError(errorEvent);
		return false;
	}
	return true;
};
OperationResult.Parse = function(responseStatus)
{
	var result = new OperationResult();
	result.isSuccess = is.Empty(responseStatus.ErrorCode);
	result.errorCode = responseStatus.ErrorCode;
	result.message = responseStatus.Message;
	result.errorMessage = responseStatus.ErrorMessage;
	result.stackTrace = responseStatus.StackTrace;

	A.each(responseStatus.FieldErrors, function(objError)
	{
		var error = new FieldError(objError.ErrorCode, objError.FieldName, objError.ErrorMessage);
		result.fieldErrors.push(error);

		if (!is.Empty(error.fieldName))
		{
			result.fieldErrorMap[error.fieldName] = error;
		}
	});

	return result;
};

OperationResult.prototype.toString = function()
{
	return this.errorCode + ": " + this.message;
};
OperationResult.prototype.getFieldErrors = function()
{
	return this.fieldErrors;
};
OperationResult.prototype.getFieldErrorMap = function()
{
	return this.fieldErrorMap;
};

/** @constructor */
function ResultEvent(result)
{
	this.result = result;
}
ResultEvent.prototype.getResult = function()
{
    return this.result;
};

/** @constructor */
function ResponseErrorEvent(result)
{
	this.result = result;
}

/** @constructor */
function FieldError(errorCode, fieldName, errorMessage)
{
	this.errorCode = errorCode;
	this.fieldName = fieldName;
	this.errorMessage = errorMessage || '';
}


/* Dependent snippets below from AjaxStack. TODO: replace with utils in Google Closure Library */
var is = {
	Null: function(a)
	{
		return a === null;
	},
	Undefined: function(a)
	{
		return a === undefined;
	},
	Empty: function(a)
	{
		return (a === null || a === undefined || a === "");
	},
	Function: function(a)
	{
		return (typeof (a) === 'function') ? a.constructor.toString().match(/Function/) !== null : false;
	},
	String: function(a)
	{
		if (a === null || a === undefined || a.type) return false;
		return (typeof (a) === 'string') ? true : (typeof (a) === 'object') ? a.constructor.toString().match(/string/i) !== null : false;
	},
	Array: function(a)
	{
		if (is.Empty(a) || a.type) return false;
		return (typeof (a) === 'object') ? a.constructor.toString().match(/array/i) !== null || a.length !== undefined : false;
	},
	Boolean: function(a)
	{
		if (is.Empty(a) || a.type) return false;
		return (typeof (a) === 'boolean') ? true : (typeof (a) === 'object') ? a.constructor.toString().match(/boolean/i) !== null : false;
	},
	Date: function(a)
	{
		if (is.Empty(a) || a.type) return false;
		return (typeof (a) === 'date') ? true : (typeof (a) === 'object') ? a.constructor.toString().match(/date/i) !== null : false;
	},
	Number: function(a)
	{
		if (is.Empty(a) || a.type) return false;
		return (typeof (a) === 'number') ? true : (typeof (a) === 'object') ? a.constructor.toString().match(/Number/) !== null : false;
	},
	ValueType: function(a)
	{
		if (is.Empty(a) || a.type) return false;
		return is.String(a) || is.Date(a) || is.Number(a) || is.Boolean(a);
	}
};

//String Utils
var S = {};
S.rtrim = function(str, chars)
{
	chars = chars || "\\s";
	return str.replace(new RegExp("[" + chars + "]+$", "g"), "");
};
S.toString = function()
{
	if (arguments.length == 0 || !arguments[0]) return null;

	var s = "";
	for (var i = 0; i < arguments.length; i++)
	{
		var arg = arguments[i];

		if (s) s += "/";

		if (is.String(arg)) s += arg;
		else if (is.ValueType(arg)) s += arg.toString();
		else if (is.Array(arg)) s += '[' + A.join(arg, ",") + ']';
		else
		{
			var o = "";
			for (var name in arg)
			{
				if (o) o += ",";
				o += name + ":" + S.safeString(arg[name]);
			}
			s += '{' + o + '}';
		}
	}
	return s;
};
S.safeString = function(str)
{
	if (!str) return str;
	if (S.containsAny(str, ['[', ']', '{', '}', ',']))
	{
		return '"' + str + '"';
	}
	return str;
};
S.containsAny = function(str, tests)
{
	if (!is.String(str)) return;
	for (var i = 0, len = tests.length; i < len; i++)
	{
		if (str.indexOf(tests[i]) != -1) return true;
	}
	return false;
};


//Array Utils
var A = {};
A.each = function(array, fn) {
	if (!array) return;
	for (var i = 0, len = array.length; i < len; i++)
		fn(array[i]);
};
A.convertAll = function(array, convertFn)
{
	var to = [];
	for (var i = 0, len = array.length; i < len; i++)
		to[i] = convertFn(array[i]);
	return to;
};
A.join = function(array, on)
{
	var s = "";
	on = on || ",";
	for (var i = 0, len = array.length; i < len; i++)
	{
		if (s) s += on;
		s += array[i];
	}
	return s;
};

//Object Utils
var O = {};
O.merge = function(dst, src) {
	for (var k in src) dst[k] = src[k];
	return dst;
};
O.keys = function(obj)
{
    var keys = [];
    for (var key in obj) keys.push(key);
    return keys;
};

//Path Utils
var Path = {};
Path.combine = function() {
    var paths = "";
    for (var i = 0, len = arguments.length; i < arguments.length; i++) {
        
        if (paths.length > 0)
            paths += "/";

        paths += S.rtrim(arguments[i], '/');
    }
    return paths;
};

var Urn = {};
Urn.toId = function(urn)
{
    return urn.replace(/:/g,'_');
};
Urn.getIdValue = function(urn)
{
    return urn.split(':')[2];
};
Urn.fromId = function(urn)
{
    return urn.replace(/_/g,':');
};


var Dto = {};
Dto.toArray = function(array)
{
    return is.Array(array)
            ? S.toString(array)
            : "[" + S.toString(array) + "]";
};
