/**
 * Created by IntelliJ IDEA.
 * User: mythz
 * Date: 16-Jun-2010
 * Time: 00:51:17
 * To change this template use File | Settings | File Templates.
 */

var JSV = {};
/**
 * parses JSV text into a JavaScript type 
 * @param str
 */
JSV.parse = function(str)
{
    if (!str) return str;
    if (str[0] == '{')
    {
        return JSV.parseObject_(str);
    }
    else if (str[0] == '[')
    {
        return JSV.parseArray_(str);
    }
    else
    {
        return JSV.parseString(str);
    }
}

JSV.ESCAPE_CHARS = ['"', ',', '{', '}', '[', ']'];

JSV.parseArray_ = function(str)
{
    var to = [], value = JSV.stripList_(str);
    if (!value) return to;

    if (value[0] == '{')
    {
        var ref = {i:0};
        do
        {
            var itemValue = JSV.eatMapValue_(value, ref);
            to.push(JSV.parse(itemValue));
        } while (++ref.i < value.length);
    }
    else
    {
        for (var ref={i:0}; ref.i < value.length; ref.i++)
        {
            var elementValue = JSV.eatElementValue_(value, ref);
            to.push(JSV.parse(elementValue));
        }
    }
    return to;
};

JSV.parseObject_ = function(str)
{
    if (str[0] != '{')
    {
        throw "Type definitions should start with a '{', got string starting with: "
            + str.substr(0, str.length < 50 ? str.length : 50);
    }

    var name, obj = {};

    if (str == '{}') return null;
    for (var ref={i:1}, strTypeLength = str.length; ref.i < strTypeLength; ref.i++)
    {
        name = JSV.eatMapKey_(str, ref);
        ref.i++;
        var value = JSV.eatMapValue_(str, ref);
        obj[name]= JSV.parse(value);
    }
    return obj;
}

JSV.eatElementValue_ = function(value, ref)
{
    return JSV.eatUntilCharFound_(value, ref, ',');
}

JSV.containsAny_ = function(str, tests)
{
    if (!is.String(str)) return;
    for (var i = 0, len = tests.length; i < len; i++)
    {
        if (str.indexOf(tests[i]) != -1) return true;
    }
    return false;
};

JSV.toCsvField = function(text)
{
    return !text || JSV.containsAny_(JSV.ESCAPE_CHARS)
        ? text
        : '"' + text.replace(/"/g, '""') + '"';
}

JSV.parseString = JSV.fromCsvField = function(text)
{
    return !text || text[0] != '"'
        ? text
        : text.substr(1, text.length - 2).replace(/""/g, '"');
}

JSV.stripList_ = function(value)
{
    if (!value) return null;
    return value[0] == '['
        ? value.substr(1, value.length - 2)
        : value;
};

/**
 * @param value {string}
 * @param ref {ref int}
 * @param findChar {char}
 */
JSV.eatUntilCharFound_ = function(value, ref, findChar)
{
    var tokenStartPos = ref.i;
    var valueLength = value.length;
    if (value[tokenStartPos] != '"')
    {
        ref.i = value.indexOf(findChar, tokenStartPos);
        if (ref.i == -1) ref.i = valueLength;
        return value.substr(tokenStartPos, ref.i - tokenStartPos);
    }

    while (++ref.i < valueLength)
    {
        if (value[ref.i] == '"')
        {
            if (ref.i + 1 >= valueLength)
            {
                return value.substr(tokenStartPos, ++ref.i - tokenStartPos);
            }
            if (value[ref.i + 1] == '"')
            {
                ref.i++;
            }
            else if (value[ref.i + 1] == findChar)
            {
                return value.substr(tokenStartPos, ++ref.i - tokenStartPos);
            }
        }
    }

    throw "Could not find ending quote";
}

/**
 *
 * @param value {string}
 * @param i {ref int}
 */
JSV.eatMapKey_ = function(value, ref)
{
    var tokenStartPos = ref.i;
    while (value[++ref.i] != ':' && ref.i < value.length) { }
    return value.substr(tokenStartPos, ref.i - tokenStartPos);
}

/**
 *
 * @param value {string}
 * @param ref {ref int}
 */
JSV.eatMapValue_ = function(value, ref)
{
    var tokenStartPos = ref.i;
    var valueLength = value.length;
    if (ref.i == valueLength) return null;

    var valueChar = value[ref.i];

    //If we are at the end, return.
    if (valueChar == ',' || valueChar == '}')
    {
        return null;
    }

    //Is List, i.e. [...]
    var withinQuotes = false;
    if (valueChar == '[')
    {
        var endsToEat = 1;
        while (++ref.i < valueLength && endsToEat > 0)
        {
            valueChar = value[ref.i];
            if (valueChar == '"')
                withinQuotes = !withinQuotes;
            if (withinQuotes)
                continue;
            if (valueChar == '[')
                endsToEat++;
            if (valueChar == ']')
                endsToEat--;
        }
        return value.substr(tokenStartPos, ref.i - tokenStartPos);
    }

    //Is Type/Map, i.e. {...}
    if (valueChar == '{')
    {
        var endsToEat = 1;
        while (++ref.i < valueLength && endsToEat > 0)
        {
            valueChar = value[ref.i];

            if (valueChar == '"')
                withinQuotes = !withinQuotes;
            if (withinQuotes)
                continue;
            if (valueChar == '{')
                endsToEat++;
            if (valueChar == '}')
                endsToEat--;
        }
        return value.substr(tokenStartPos, ref.i - tokenStartPos);
    }

    //Is Within Quotes, i.e. "..."
    if (valueChar == '"')
    {
        while (++ref.i < valueLength)
        {
            valueChar = value[ref.i];
            if (valueChar != '"') continue;
            var isLiteralQuote = ref.i + 1 < valueLength && value[ref.i + 1] == '"';
            ref.i++; //skip quote
            if (!isLiteralQuote)
                break;
        }
        return value.substr(tokenStartPos, ref.i - tokenStartPos);
    }

    //Is Value
    while (++ref.i < valueLength)
    {
        valueChar = value[ref.i];
        if (valueChar == ',' || valueChar == '}')
            break;
    }

    return value.substr(tokenStartPos, ref.i - tokenStartPos);
}

JSV.isEmpty_ = function(a)
{
    return (a === null || a === undefined || a === "");
}
JSV.isFunction_ = function(a)
{
    return (typeof (a) === 'function') ? a.constructor.toString().match(/Function/) !== null : false;
};
JSV.isString_ = function(a)
{
    if (a === null || a === undefined) return false;
    return (typeof (a) === 'string') ? true : (typeof (a) === 'object') ? a.constructor.toString().match(/string/i) !== null : false;
};
JSV.isDate_ = function(a)
{
    if (JSV.isEmpty_(a)) return false;
    return (typeof (a) === 'date') ? true : (typeof (a) === 'object') ? a.constructor.toString().match(/date/i) !== null : false;
};

JSV.isArray_ = function(a)
{
    if (a === null || a === undefined || a === "") return false;
    return (typeof (a) === 'object') ? a.constructor.toString().match(/array/i) !== null || a.length !== undefined : false;
};
JSV.toXsdDateTime = function(date)
{
    function pad(n) {
        var s = n.toString();
        return s.length < 2 ? '0'+s : s;
    };
    var yyyy = date.getUTCFullYear();
    var MM = pad(date.getUTCMonth()+1);
    var dd = pad(date.getUTCDate());
    var hh = pad(date.getUTCHours());
    var mm = pad(date.getUTCMinutes());
    var ss = pad(date.getUTCSeconds());
    var ms = pad(date.getUTCMilliseconds());

  return yyyy +'-' + MM + '-' + dd + 'T' + hh + ':' + mm + ':' + ss + '.' + ms + 'Z';
}
JSV.serialize = JSV.stringify = function(obj)
{
    if (obj === null || obj === undefined) return null;

    var typeOf = typeof(obj);
    if (obj === 'function') return null;

    if (typeOf === 'object')
    {
        var ctorStr = obj.constructor.toString().toLowerCase();
        if (ctorStr.indexOf('string') != -1)
            return JSV.escapeString(obj);
        if (ctorStr.indexOf('boolean') != -1)
            return obj ? "True" : "False";
        if (ctorStr.indexOf('number') != -1)
            return obj;
        if (ctorStr.indexOf('date') != -1)
            return JSV.toXsdDateTime(obj);
        if (ctorStr.indexOf('array') != -1)
            return JSV.serializeArray(obj);

        return JSV.serializeObject(obj);
    }
    else
    {
        switch(typeOf)
        {
            case 'string':
                return JSV.escapeString(obj);
                break;
            case 'boolean':
                return obj ? "True" : "False";
                break;
            case 'date':
                return JSV.toXsdDateTime(obj);
                break;
            case 'array':
                return JSV.serializeArray(obj);
                break;
            case 'number':
            default:
                return obj;
        }
    }
};
JSV.serializeObject = function(obj)
{
    var value, sb = new StringBuffer();
    for (var key in obj)
    {
        value = obj[key];
        if (!obj.hasOwnProperty(key) || JSV.isEmpty_(value) || JSV.isFunction_(value)) continue;

        if (sb.length > 0)
            sb.append(',');

        sb.append(JSV.escapeString(key));
        sb.append(':');
        sb.append(JSV.serialize(value));
    }
    return '{' + sb.toString() + '}';
};
JSV.serializeArray = function(array)
{
    var value, sb = new StringBuffer();
    for (var i=0, len=array.length; i<len; i++)
    {
        value = array[i];
        if (JSV.isEmpty_(value) || JSV.isFunction_(value)) continue;

        if (sb.getLength() > 0)
            sb.append(',');

        sb.append(JSV.serialize(value));
    }
    return '[' + sb.toString() + ']';
};
JSV.escapeString = function(str)
{
	if (str === undefined || str === null) return null;
    if (str === '') return '""';

    if (str.indexOf('"'))
    {
        str = str.replace(/"/g,'""');
    }
	if (JSV.containsAny_(str, JSV.ESCAPE_CHARS))
	{
		return '"' + str + '"';
	}
	return str;
};
JSV.containsAny_ = function(str, tests)
{
	if (!JSV.isString_(str)) return;
	for (var i = 0, len = tests.length; i < len; i++)
	{
		if (str.indexOf(tests[i]) != -1) return true;
	}
	return false;
};

/* Closure Library StringBuffer for efficient string concatenation */
var hasScriptEngine = 'ScriptEngine' in window;
var HAS_JSCRIPT = hasScriptEngine && window['ScriptEngine']() == 'JScript';

StringBuffer = function(opt_a1, var_args) {
  this.buffer_ = HAS_JSCRIPT ? [] : '';

  if (opt_a1 != null) {
    this.append.apply(this, arguments);
  }
};
StringBuffer.prototype.set = function(s) {
  this.clear();
  this.append(s);
};
if (HAS_JSCRIPT) {
  StringBuffer.prototype.bufferLength_ = 0;
  StringBuffer.prototype.append = function(a1, opt_a2, var_args) {
    // IE version.
    if (opt_a2 == null) { // second argument is undefined (null == undefined)
      // Array assignment is 2x faster than Array push.  Also, use a1
      // directly to avoid arguments instantiation, another 2x improvement.
      this.buffer_[this.bufferLength_++] = a1;
    } else {
      this.buffer_.push.apply(/** @type {Array} */ (this.buffer_), arguments);
      this.bufferLength_ = this.buffer_.length;
    }
    return this;
  };
} else {
  StringBuffer.prototype.append = function(a1, opt_a2, var_args) {
    // W3 version.
    this.buffer_ += a1;
    if (opt_a2 != null) { // second argument is undefined (null == undefined)
      for (var i = 1; i < arguments.length; i++) {
        this.buffer_ += arguments[i];
      }
    }
    return this;
  };
}
StringBuffer.prototype.clear = function() {
  if (HAS_JSCRIPT) {
     this.buffer_.length = 0;  // Reuse the array to avoid creating new object.
     this.bufferLength_ = 0;
   } else {
     this.buffer_ = '';
   }
};
StringBuffer.prototype.getLength = function() {
   return this.toString().length;
};
StringBuffer.prototype.toString = function() {
  if (HAS_JSCRIPT) {
    var str = this.buffer_.join('');
    this.clear();
    if (str) {
      this.append(str);
    }
    return str;
  } else {
    return /** @type {string} */ (this.buffer_);
  }
};


/**
 * Considering pulling this out
 * @param baseUri
 * @param type
 */
function JsvServiceClient(baseUri)
{
	this.baseSyncReplyUri = JsvServiceClient.combine_(baseUri, "jsv/reply");
	this.baseAsyncOneWayUri = JsvServiceClient.combine_(baseUri, "jsv/oneway");
}
JsvServiceClient.prototype.send = function(webMethod, request, onSuccess, onError, ajaxOptions) {
	var startCallTime = new Date();
	var requestUrl = JsvServiceClient.combine_(this.baseSyncReplyUri, webMethod);
	var id = JsvServiceClient.id++;

	var options = {
		type: "GET",
		url: requestUrl,
		data: request,
		dataType: "text",
		success: function(responseText)
		{
			var endCallTime = new Date();
			var callDuration = endCallTime.getTime() - startCallTime.getTime();

            var response = JSV.parse(responseText);
			if (!response)
			{
				if (onSuccess) onSuccess(null);
				return;
			}

            var status = JsvServiceClient.parseResponseStatus_(response.ResponseStatus);
			if (status.isSuccess)
			{
				if (onSuccess) onSuccess(response);
				JsvServiceClient.onSuccess({ id: id, webMethod: webMethod, request: request,
					response: response, durationMs: callDuration
				});
			}
			else
			{
                if (onError) onError(status);
				JsvServiceClient.onError({ id: id, webMethod: webMethod, request: request,
					error: status, durationMs: callDuration
                });
			}
		},
		error: function(xhr, desc, exObj)
		{
			var endCallTime = new Date();
			var callDuration = endCallTime.getTime() - startCallTime.getTime();

			try
			{
				if (onError) onError(xhr.responseText);
			}
			catch (e) {}
			JsvServiceClient.onError({ id: id, webMethod: webMethod, request: request,
				error: xhr.responseText, durationMs: callDuration
			});
		}
	};

    for (var k in ajaxOptions) options[k] = ajaxOptions[k];

	var ajax = $.ajax(options);
};

JsvServiceClient.combine_ = function() {
    var paths = "";
    for (var i = 0, len = arguments.length; i < len; i++) {
        if (paths.length > 0)
            paths += "/";
        paths += arguments[i].replace(/[/]+$/g, "");
    }
    return paths;
};

//Sends a HTTP 'GET' request on the QueryString
JsvServiceClient.prototype.getFromService = function(webMethod, request, onSuccess, onError) {
	this.send(webMethod, request, onSuccess, onError);
};

//Sends a HTTP 'POST' request as key value pair formData
JsvServiceClient.prototype.postFormDataToService = function(webMethod, request, onSuccess, onError) {
	this.send(webMethod, request, onSuccess, onError, { type: "POST" });
};

//Sends a HTTP 'POST' request as JSV @requires jQuery
JsvServiceClient.prototype.postToService = function(webMethod, request, onSuccess, onError) {
	var jsvRequest = JSV.serialize(request);
	this.send(webMethod, jsvRequest, onSuccess, onError, { type: "POST", processData: false, contentType: "application/jsv; charset=utf-8" });
};

JsvServiceClient.id = 0;
JsvServiceClient.onError = function() { };
JsvServiceClient.onSuccess = function() { };

JsvServiceClient.parseResponseStatus_ = function(status)
{
    if (!status) return {isSuccess:true};

	var result =
    {
        isSuccess: status.ErrorCode === undefined || status.ErrorCode === null,
        errorCode: status.ErrorCode,
        message: status.Message,
        errorMessage: status.ErrorMessage,
        stackTrace: status.StackTrace,
        fieldErrors: [],
        fieldErrorMap: {}
    };

    if (status.FieldErrors)
    {
        for (var i=0, len = status.FieldErrors.length; i<len; i++)
        {
            var err = status.FieldErrors[i];
            var error = {errorCode: err.ErrorCode, fieldName:err.FieldName, errorMessage:err.ErrorMessage||''};
            result.fieldErrors.push(error);

            if (error.fieldName)
            {
                result.fieldErrorMap[error.fieldName] = error;
            }
        }
    }
	return result;
};
