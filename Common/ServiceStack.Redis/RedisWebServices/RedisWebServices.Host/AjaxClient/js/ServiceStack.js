/** @constructor */

/*
if (!goog) {
    var goog = {};
    goog.provide = goog.require = goog.exportSymbol = function(){} 
}
*/

/*
goog.provide("JsonServiceClient");
goog.provide("is");
goog.provide("S");
goog.provide("A");
goog.provide("O");
goog.provide("Path");
goog.provide("Urn");
goog.provide("Dto");
*/

goog.require("goog.dom");
goog.require("goog.net.XhrIo");
goog.require("goog.structs.Map");
goog.require("goog.Uri.QueryData");
goog.require("goog.Uri.QueryData");
goog.require("goog.structs.Map");

function JsonServiceClient(baseUri) {
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
        success: function(response) {
            var endCallTime = new Date();
            var callDuration = endCallTime.getTime() - startCallTime.getTime();
            if (!response) {
                if (onSuccess) onSuccess(null);
                return;
            }

            var status = JsonServiceClient.parseResponseStatus_(response.ResponseStatus);
            if (status.isSuccess) {
                if (onSuccess) onSuccess(response);
                JsonServiceClient.onSuccess({ id: id, webMethod: webMethod, request: request,
                    response: response, durationMs: callDuration
                });
            }
            else {
                if (onError) onError(status);
                JsonServiceClient.onError({ id: id, webMethod: webMethod, request: request,
                    error: status, durationMs: callDuration
                });
            }
        },
        error: function(xhr, desc, exceptionobj) {
            var endCallTime = new Date();
            var callDuration = endCallTime.getTime() - startCallTime.getTime();

            try {
                var response = xhr.responseText;
                try { response = JsonServiceClient.parseJSON(response); } catch (e) { }
                if (onError) onError(response);
            }
            catch (e) { }
            JsonServiceClient.onError({ id: id, webMethod: webMethod, request: request,
                error: xhr.responseText, durationMs: callDuration
            });
        }
    };

    for (var k in ajaxOptions) options[k] = ajaxOptions[k];

    var ajax = JsonServiceClient.ajax(options);
};

//Sends a HTTP 'GET' request on the QueryString
JsonServiceClient.prototype.getFromService = function(webMethod, request, onSuccess, onError) {
    var options = document.all ? { cache: false} : null;
    this.send(webMethod, request, onSuccess, onError, options);
};

//Sends a HTTP 'POST' request as key value pair formData
JsonServiceClient.prototype.postFormDataToService = function(webMethod, request, onSuccess, onError) {
    this.send(webMethod, request, onSuccess, onError, { type: "POST" });
};

//Sends a HTTP 'POST' request as JSON @requires jQuery
JsonServiceClient.prototype.postToService = function(webMethod, request, onSuccess, onError) {
    var jsonRequest = JsonServiceClient.toJSON(request);
    this.send(webMethod, jsonRequest, onSuccess, onError, { type: "POST", processData: false, contentType: jsonContentType });
};

//Sends a HTTP 'PUT' request as JSON @requires jQuery
JsonServiceClient.prototype.putToService = function(webMethod, request, onSuccess, onError) {
    var jsonRequest = JsonServiceClient.toJSON(request);
    this.send(webMethod, jsonRequest, onSuccess, onError, { type: "PUT", processData: false, contentType: jsonContentType });
};

//Sends a HTTP 'DELETE' request as JSON @requires jQuery
JsonServiceClient.prototype.deleteFromService = function(webMethod, request, onSuccess, onError) {
    var jsonRequest = JsonServiceClient.toJSON(request);
    this.send(webMethod, jsonRequest, onSuccess, onError, { type: "DELETE", processData: false, contentType: jsonContentType });
};

JsonServiceClient.id = 0;
JsonServiceClient.onError = function() { };
JsonServiceClient.onSuccess = function() { };
JsonServiceClient.parseResponseStatus_ = function(status) {
    if (!status) return { isSuccess: true };

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

    if (status.FieldErrors) {
        for (var i = 0, len = status.FieldErrors.length; i < len; i++) {
            var err = status.FieldErrors[i];
            var error = { errorCode: err.ErrorCode, fieldName: err.FieldName, errorMessage: err.ErrorMessage || '' };
            result.fieldErrors.push(error);

            if (error.fieldName) {
                result.fieldErrorMap[error.fieldName] = error;
            }
        }
    }
    return result;
};
JsonServiceClient.toJsonDate = function(date) {
    var jsDate = is.Date(date) ? date : new Date(date);
}
//Adapter methods use jquery or google closure library if available
JsonServiceClient.parseJSON_ = null;
JsonServiceClient.parseJSON = function(json) {
    if (JsonServiceClient.parseJSON_ === null)
    {
        if (typeof(JSON) == 'object' && JSON.parse)
            JsonServiceClient.parseJSON_ = JSON.parse(json);
        if (window.$ !== undefined && $.parseJSON)
            JsonServiceClient.parseJSON_ = $.parseJSON(json);
        if (!is.Undefined(goog) && goog.json)
            JsonServiceClient.parseJSON_ = goog.json.parse(json);
        else
            throw "no json parser found";
    }
    return JsonServiceClient.parseJSON_(json);
}
JsonServiceClient.toJSON_ = null;
JsonServiceClient.toJSON = function(o) {
    if (JsonServiceClient.toJSON_ === null)
    {
        if (typeof(JSON) == 'object' && JSON.stringify)
            JsonServiceClient.toJSON_ = JSON.stringify;
        if (window.$ !== undefined && $.toJSON)
            JsonServiceClient.toJSON_ = $.toJSON;
        if (!is.Undefined(goog) && goog.json)
            JsonServiceClient.toJSON_ = goog.json.serialize;
        else
            throw "no json serializer found";
    }
    return JsonServiceClient.toJSON_(o);
}

var jsonContentType = "application/json; charset=utf-8",
    rquery = /\?/,
	rts = /(\?|&)_=.*?(&|$)/,
	rurl = /^(\w+:)?\/\/([^\/?#]+)/,
    ajaxSettings = {
        type: "GET",
        contentType: "application/x-www-form-urlencoded",
        dataType: "json",
        accepts: {
            xml: "application/xml, text/xml",
            html: "text/html",
            script: "text/javascript, application/javascript",
            json: "application/json, text/javascript",
            text: "text/plain",
            _default: "*/*"
        }
    };

JsonServiceClient.ajax = function(s) {
    if (window.$ !== undefined && $.ajax)
        return $.ajax(s);

    for (var k in ajaxSettings) if (!s[k]) s[k] = ajaxSettings[k];

    var xhr = new goog.net.XhrIo();
    goog.events.listen(xhr, "complete", function(){
        if (xhr.isSuccess())
        {
            if (!s.success) return;
            if (s.dataType == "json") {
                s.success(xhr.getResponseJson());
            } else if (s.dataType == "xml") {
                s.success(xhr.getResponseXml());
            } else {
                s.success(xhr.getResponseText());
            }
        }
        else
        {
            if (!s.error) return;
            s.error(xhr, xhr.getLastErrorCode(), xhr.getLastError());
        }
    });

    if (s.cache === false && type === "GET" )
    {
        var ts = (new Date).getTime();
        var ret = s.url.replace(rts, "$1_=" + ts + "$2");
        s.url = ret + ((ret === s.url) ? (rquery.test(s.url) ? "&" : "?") + "_=" + ts : "");
    }

    var headers = {'Content-Type':s.contentType};
    var data = goog.Uri.QueryData.createFromMap(new goog.structs.Map(s.data));

    if (s.type == "GET")
    {
        s.url += (rquery.test(s.url) ? "&" : "?") + data.toString();
        xhr.send(s.url, "GET", null, headers);
    }
    else
    {
        var strData = (s.contentType == jsonContentType)
            ? s.data
            : data.toString();

        xhr.send(s.url, s.type, strData, headers);
    }
}

/* Dependent snippets below from AjaxStack. TODO: replace with utils in Google Closure Library */
var is = {
    Null: function(a) {
        return a === null;
    },
    Undefined: function(a) {
        return a === undefined;
    },
    Empty: function(a) {
        return (a === null || a === undefined || a === "");
    },
    Function: function(a) {
        return (typeof (a) === 'function') ? a.constructor.toString().match(/Function/) !== null : false;
    },
    String: function(a) {
        if (a === null || a === undefined || a.type) return false;
        return (typeof (a) === 'string') ? true : (typeof (a) === 'object') ? a.constructor.toString().match(/string/i) !== null : false;
    },
    Array: function(a) {
        if (is.Empty(a) || a.type) return false;
        return (typeof (a) === 'object') ? a.constructor.toString().match(/array/i) !== null || a.length !== undefined : false;
    },
    Boolean: function(a) {
        if (is.Empty(a) || a.type) return false;
        return (typeof (a) === 'boolean') ? true : (typeof (a) === 'object') ? a.constructor.toString().match(/boolean/i) !== null : false;
    },
    Date: function(a) {
        if (is.Empty(a) || a.type) return false;
        return (typeof (a) === 'date') ? true : (typeof (a) === 'object') ? a.constructor.toString().match(/date/i) !== null : false;
    },
    Number: function(a) {
        if (is.Empty(a) || a.type) return false;
        return (typeof (a) === 'number') ? true : (typeof (a) === 'object') ? a.constructor.toString().match(/Number/) !== null : false;
    },
    ValueType: function(a) {
        if (is.Empty(a) || a.type) return false;
        return is.String(a) || is.Date(a) || is.Number(a) || is.Boolean(a);
    }
};

//String Utils
var S = {};
S.rtrim = function(str, chars) {
    chars = chars || "\\s";
    return str.replace(new RegExp("[" + chars + "]+$", "g"), "");
};
S.toString = function() {
    if (arguments.length == 0 || !arguments[0]) return null;

    var s = "";
    for (var i = 0; i < arguments.length; i++) {
        var arg = arguments[i];

        if (s) s += "/";

        if (is.String(arg)) s += arg;
        else if (is.ValueType(arg)) s += arg.toString();
        else if (is.Array(arg)) s += '[' + A.join(arg, ",") + ']';
        else {
            var o = "";
            for (var name in arg) {
                if (o) o += ",";
                o += name + ":" + S.safeString(arg[name]);
            }
            s += '{' + o + '}';
        }
    }
    return s;
};
S.safeString = function(str) {
    if (!str) return str;
    if (S.containsAny(str, ['[', ']', '{', '}', ','])) {
        return '"' + str + '"';
    }
    return str;
};
S.containsAny = function(str, tests) {
    if (!is.String(str)) return;
    for (var i = 0, len = tests.length; i < len; i++) {
        if (str.indexOf(tests[i]) != -1) return true;
    }
    return false;
};
S.startsWith = function(text, startsWith) {
    if (!text || !startsWith) return false;
    return text.lastIndexOf(startsWith, 0) == 0;
};
S.pad = function(text, padLen, padChar, rpad) {
    var padChar = padChar || (rpad ? " " : "0");
    text = text.toString();
    while (text.length < padLen) {
        text = rpad
          ? text + padChar
          : padChar + text;
    }
    return text;
}
S.padLeft = function(text, padLen, padChar) {
    return S.pad(text, padLen, padChar, false);
}
S.padRight = function(text, padLen, padChar) {
    return S.pad(text, padLen, padChar, true);
}
S.lpad = S.padLeft;
S.rpad = S.padRight;


//Array Utils
var A = {};
A.each = function(array, fn) {
    if (!array) return;
    for (var i = 0, len = array.length; i < len; i++)
        fn(array[i]);
};
A.convertAll = function(array, convertFn) {
    var to = [];
    for (var i = 0, len = array.length; i < len; i++)
        to[i] = convertFn(array[i]);
    return to;
};
A.join = function(array, on) {
    var s = "";
    on = on || ",";
    for (var i = 0, len = array.length; i < len; i++) {
        if (s) s += on;
        s += array[i];
    }
    return s;
};
A.toTable = function(array, tableFormatFns) {
    tableFormatFns = tableFormatFns || {};
    var cols = [], sb = [];
    for (var i = 0, len = array.length; i < len; i++) {
        var obj = array[i];
        if (!obj) continue;
        if (i == 0) {
            sb.push("<table><thead><tr>");
            for (var k in obj) {
                cols.push(k);
                sb.push("<th>" + k + "</th>");
            }
            sb.push("</tr></thead><tbody>");
        }
        sb.push("<tr>");
        for (var j = 0, colsLen = cols.length; j < colsLen; j++) {
            var k = cols[j];
            var data = tableFormatFns[k] ? tableFormatFns[k](obj[k]) : Dto.formatValue(obj[k]);

            sb.push("<td>" + data + "</td>");
        }
        sb.push("</tr>");
    }
    sb.push("</tbody></table>");
    return sb.join('');
}

//Object Utils
var O = {};
O.keys = function(obj) {
    var keys = [];
    for (var key in obj) keys.push(key);
    return keys;
};

//Path Utils
var Path = {};
Path.combine = function() {
    var paths = "";
    for (var i = 0, len = arguments.length; i < len; i++) {

        if (paths.length > 0)
            paths += "/";

        paths += S.rtrim(arguments[i], '/');
    }
    return paths;
};
Path.getFirstArg = function(path)
{
    if (!path) return null;
    return path.split('/')[0];
};
Path.getFirstValue = function(path)
{
    if (!path || path.indexOf('/') == -1) return null;
    return path.substr(path.indexOf('/') + 1);
};
Path.getArgs = function(path)
{
    if (!path) return null;
    return path.split('/');
};

var Urn = {};
Urn.toId = function(urn) {
    return urn.replace(/:/g, '_');
};
Urn.getIdValue = function(urn) {
    return urn.split(':')[2];
};
Urn.fromId = function(urn) {
    return urn.replace(/_/g, ':');
};


var Dto = {};
Dto.toArray = function(array) {
    return is.Array(array)
        ? S.toString(array)
        : "[" + S.toString(array) + "]";
};
Dto.toUtcDate = function(date) {
    return date.getUTCFullYear()
        + '-' + S.lpad(date.getUTCMonth() + 1, 2)
        + '-' + S.lpad(date.getUTCDate(), 2)
        + 'T' + S.lpad(date.getUTCHours(), 2)
        + ':' + S.lpad(date.getUTCMinutes(), 2)
        + ':' + S.lpad(date.getUTCSeconds(), 2)
        + 'Z';
};
Dto.isJsonDate = function(str)
{
    if (!is.String(str)) return false;
    return S.startsWith(str, Dto.WcfDatePrefix);
};
Dto.WcfDatePrefix = "\/Date(";
Dto.toJsonDate = function(date) {
    date = Dto.parseJsonDate(date);
    return Dto.WcfDatePrefix + date.getTime() + "+0000)\/";
};
Dto.parseJsonDate = function(date) {
    return is.Date(date)
        ? date
        : (S.startsWith(date, Dto.WcfDatePrefix)
            ? new Date(parseInt(date.substring(Dto.WcfDatePrefix.length, date.length - 2))) 
            : new Date(date));
};
Dto.formatDate = function(date) {
    //IE needs '/' seperators
    date = Dto.parseJsonDate(date);
    return date.getUTCFullYear()
        + '/' + S.lpad(date.getUTCMonth() + 1, 2)
        + '/' + S.lpad(date.getUTCDate(), 2);
};
Dto.formatValue = function(value)
{
    if (Dto.isJsonDate(value)) return Dto.formatDate(value);
    if (is.Empty(value)) return "";
    return value;
};

/*
goog.exportSymbol("JsonServiceClient", JsonServiceClient);
goog.exportSymbol("is", is);
goog.exportSymbol("S", S);
goog.exportSymbol("A", A);
goog.exportSymbol("O", O);
goog.exportSymbol("Path", Path);
goog.exportSymbol("Urn", Urn);
goog.exportSymbol("Dto", Dto);
*/