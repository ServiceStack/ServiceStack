//Uncomment below to use this script with jQuery instead of the google closure library
//if (!window.goog) {
//    var goog = {};
//    goog.provide = goog.require = goog.exportSymbol = function(){}
//}

goog.provide("JsonServiceClient");
goog.provide("is");
goog.provide("S");
goog.provide("A");
goog.provide("O");
goog.provide("Path");
goog.provide("Urn");
goog.provide("Dto");

goog.require("goog.dom");
goog.require("goog.net.XhrIo");
goog.require("goog.structs.Map");
goog.require("goog.Uri.QueryData");
goog.require("goog.Uri.QueryData");
goog.require("goog.structs.Map");

/**
 * Handles the Editor tab content
 * @param {string} the baseUri of ServiceStack web services.
 * @constructor
 */
JsonServiceClient = function(baseUri) {
    this.baseSyncReplyUri = Path.combine(baseUri, "json/reply");
    this.baseAsyncOneWayUri = Path.combine(baseUri, "json/oneway");
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
        if (window.goog !== undefined && goog.json)
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
        if (window.goog !== undefined && goog.json)
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

    if (s.cache === false && s.type === "GET" )
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
    if (!text || !startsWith || typeof text != 'string') return false;
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
    return S.startsWith(value, "{")
    	? _.jsonreport(value) 
    	: _.jsonreport.val(value);
};


if (!_) var _ = {};
_.jsonreport = (function ()
{
	var root = this, doc = document,
		$ = function (id) { return doc.getElementById(id); },
		$$ = function (sel) { return doc.getElementsByTagName(sel); },
		$each = function (fn) { for (var i = 0, len = this.length; i < len; i++) fn(i, this[i], this); },
		isIE = /msie/i.test(navigator.userAgent) && !/opera/i.test(navigator.userAgent);

	$.each = function (arr, fn) { $each.call(arr, fn); };

	var splitCase = function (t) { return typeof t != 'string' ? t : t.replace(/([A-Z]|[0-9]+)/g, ' $1').replace(/_/g, ' '); },
		uniqueKeys = function (m) { var h = {}; for (var i = 0, len = m.length; i < len; i++) for (var k in m[i]) if (show(k)) h[k] = k; return h; },
		keys = function (o) { var a = []; for (var k in o) if (show(k)) a.push(k); return a; }
	var tbls = [];

	function val(m)
	{
   		if (m == null) return '';
   		if (typeof m == 'number') return num(m);
   		if (typeof m == 'string') return str(m);
   		if (typeof m == 'boolean') return m ? 'true' : 'false';
   		return m.length ? arr(m) : obj(m);
	}
	function num(m) { return m; }
	function str(m)
	{
   		return m.substr(0, 6) == '/Date(' ? dfmt(date(m)) : m;
	}
	function date(s) { return new Date(parseFloat(/Date\(([^)]+)\)/.exec(s)[1])); }
	function pad(d) { return d < 10 ? '0' + d : d; }
	function dfmt(d) { return d.getFullYear() + '/' + pad(d.getMonth() + 1) + '/' + pad(d.getDate()); }
	function show(k) { return typeof k != 'string' || k.substr(0, 2) != '__'; }
	function obj(m)
	{
   		var sb = '<dl>';
   		for (var k in m) if (show(k)) sb += '<dt class="ib">' + splitCase(k) + '</dt><dd>' + val(m[k]) + '</dd>';
   		sb += '</dl>';
   		return sb;
	}
	function arr(m)
	{
   		if (typeof m[0] == 'string' || typeof m[0] == 'number') return m.join(', ');
   		var id = tbls.length, h = uniqueKeys(m);
   		var sb = '<table id="tbl-' + id + '"><caption></caption><thead><tr>';
   		tbls.push(m);
   		var i = 0;
   		for (var k in h) sb += '<th id="h-' + id + '-' + (i++) + '"><b></b>' + splitCase(k) + '</th>';
   		sb += '</tr></thead><tbody>' + makeRows(h, m) + '</tbody></table>';
   		return sb;
	}

	function makeRows(h, m)
	{
   		var sb = '';
   		for (var r = 0, len = m.length; r < len; r++)
   		{
   			sb += '<tr>';
   			var row = m[r];
   			for (var k in h) sb += '<td>' + val(row[k]) + '</td>';
   			sb += '</tr>';
   		}
   		return sb;
	}

	function setTableBody(tbody, html)
	{
   		if (!isIE) { tbody.innerHTML = html; return; }
   		var temp = tbody.ownerDocument.createElement('div');
   		temp.innerHTML = '<table>' + html + '</table>';
   		tbody.parentNode.replaceChild(temp.firstChild.firstChild, tbody);
	}

	function clearSel()
	{
   		if (doc.selection && doc.selection.empty) doc.selection.empty();
   		else if (root.getSelection)
   		{
   			var sel = root.getSelection();
   			if (sel && sel.removeAllRanges) sel.removeAllRanges();
   		}
	}

	function cmp(v1, v2)
	{
   		var f1, f2, f1 = parseFloat(v1), f2 = parseFloat(v2);
   		if (!isNaN(f1) && !isNaN(f2)) v1 = f1, v2 = f2;
   		if (typeof v1 == 'string' && v1.substr(0, 6) == '/Date(') v1 = date(v1), v2 = date(v2);
   		if (v1 == v2) return 0;
   		return v1 > v2 ? 1 : -1;
	}

	function enc(html)
	{
   		if (typeof html != 'string') return html;
   		return html.replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
	}

	function addEvent(obj, type, fn)
	{
   		if (obj.attachEvent)
   		{
   			obj['e' + type + fn] = fn;
   			obj[type + fn] = function () { obj['e' + type + fn](root.event); }
   			obj.attachEvent('on' + type, obj[type + fn]);
   		} else
   			obj.addEventListener(type, fn, false);
	}

	addEvent(doc, 'click', function (e)
	{
   		var e = e || root.event, el = e.target || e.srcElement, cls = el.className;
   		if (el.tagName == 'B') el = el.parentNode;
   		if (el.tagName != 'TH') return;
   		el.className = cls == 'asc' ? 'desc' : (cls == 'desc' ? null : 'asc');
   		$.each($$('TH'), function (i, th) { if (th == el) return; th.className = null; });
   		clearSel();
   		var ids = el.id.split('-'), tId = ids[1], cId = ids[2];
   		var tbl = tbls[tId].slice(0), h = uniqueKeys(tbl), col = keys(h)[cId], tbody = el.parentNode.parentNode.nextSibling;
   		if (!el.className) { setTableBody(tbody, makeRows(h, tbls[tId])); return; }
   		var d = el.className == 'asc' ? 1 : -1;
   		tbl.sort(function (a, b) { return cmp(a[col], b[col]) * d; });
   		setTableBody(tbody, makeRows(h, tbl));
	});

	var f = function (json)
	{
		var model = typeof json == 'string' ? JSON.parse(json) : json;
		return val(model);
	};
	f.val = val;
	return f;	
})();


goog.exportSymbol("JsonServiceClient", JsonServiceClient);
goog.exportSymbol("is", is);
goog.exportSymbol("S", S);
goog.exportSymbol("A", A);
goog.exportSymbol("O", O);
goog.exportSymbol("Path", Path);
goog.exportSymbol("Urn", Urn);
goog.exportSymbol("Dto", Dto);
goog.exportSymbol("jsonreport", _.jsonreport);
