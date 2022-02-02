;(function (root, f) {
    if (typeof exports === 'object' && typeof module === 'object')
        module.exports = f(require("jquery"));
    if (typeof define === "function" && define.amd)
        define(["jquery"], f);
    else if (typeof exports === "object")
        f(require("jquery"));
    else
        f(root.jQuery);
})(this, function ($) {

    if (!$.ss) $.ss = {};
    $.ss.handlers = {};
    $.ss.onSubmitDisable = "[type=submit]";
    $.ss.validation = {
        overrideMessages: false,
        messages: {
            NotEmpty: "Required",
            NotNull: "Required",
            Email: "Invalid email",
            AlreadyExists: "Already exists"
        },
        errorFilter: function (errorMsg, errorCode, type) {
            return this.overrideMessages
                ? this.messages[errorCode] || errorMsg || splitCase(errorCode)
                : errorMsg || splitCase(errorCode);
        }
    };
    $.ss.clearAdjacentError = function () {
        $(this).removeClass("error");
        $(this).prev(".help-inline,.help-block").removeClass("error").html("");
        $(this).next(".help-inline,.help-block").removeClass("error").html("");
    };
    $.ss.todate = function (s) { return new Date(parseFloat(/Date\(([^)]+)\)/.exec(s)[1])); };
    $.ss.todfmt = function (s) { return $.ss.dfmt($.ss.todate(s)); };
    function pad(d) { return d < 10 ? '0' + d : d; };
    $.ss.dfmt = function (d) { return d.getFullYear() + '/' + pad(d.getMonth() + 1) + '/' + pad(d.getDate()); };
    $.ss.dfmthm = function (d) { return d.getFullYear() + '/' + pad(d.getMonth() + 1) + '/' + pad(d.getDate()) + ' ' + pad(d.getHours()) + ":" + pad(d.getMinutes()); };
    $.ss.tfmt12 = function (d) { return pad((d.getHours() + 24) % 12 || 12) + ":" + pad(d.getMinutes()) + ":" + pad(d.getSeconds()) + " " + (d.getHours() > 12 ? "PM" : "AM"); };
    $.ss.splitOnFirst = function (s, c) { if (!s) return [s]; var pos = s.indexOf(c); return pos >= 0 ? [s.substring(0, pos), s.substring(pos + 1)] : [s]; };
    $.ss.splitOnLast = function (s, c) { if (!s) return [s]; var pos = s.lastIndexOf(c); return pos >= 0 ? [s.substring(0, pos), s.substring(pos + 1)] : [s]; };
    $.ss.getSelection = function () {
        return window.getSelection
            ? window.getSelection().toString()
            : document.selection && document.selection.type !== "Control"
                ? document.selection.createRange().text : "";
    };
    $.ss.combinePaths = function() {
        var parts = [], i, l;
        for (i = 0, l = arguments.length; i < l; i++) {
            var arg = arguments[i];
            parts = arg.indexOf("://") === -1
                ? parts.concat(arg.split("/"))
                : parts.concat(arg.lastIndexOf("/") === arg.length-1 ? arg.substring(0, arg.length-1) : arg);
        }
        var paths = [];
        for (i = 0, l = parts.length; i < l; i++) {
            var part = parts[i];
            if (!part || part === ".") continue;
            if (part === "..") paths.pop();
            else paths.push(part);
        }
        if (parts[0] === "") paths.unshift("");
        return paths.join("/") || (paths.length ? "/" : ".");
    };
    $.ss.queryString = function (url) {
        if (!url || url.indexOf('?') === -1) return {};
        url = $.ss.splitOnFirst(url,'#')[0];
        var pairs = $.ss.splitOnFirst(url, '?')[1].split('&');
        var map = {};
        for (var i = 0; i < pairs.length; ++i) {
            var p = pairs[i].split('=');
            map[p[0]] = p.length > 1
                ? decodeURIComponent(p[1].replace(/\+/g, ' '))
                : null;
        }
        return map;
    };
    $.ss.toUrl = function (url, args) {
        for (var k in args) {
            url += url.indexOf('?') >= 0 ? '&' : '?';
            url += k + "=" + $.ss.encodeValue(args[k]);
        }
        return url;
    };
    $.ss.encodeValue = function (o) {
        if (o == null) return "";
        if ($.isArray(o)) {
            var s = "";
            for (var i = 0; i < o.length; i++) {
                if (s.length > 0)
                    s += ',';
                s += $.ss.encodeValue(o[i]);
            }
            return s;
        }
        return encodeURIComponent(o);
    };
    $.ss.bindAll = function (o) {
        for (var k in o) {
            if (typeof o[k] == 'function')
                o[k] = o[k].bind(o);
        }
        return o;
    };
    $.ss.createPath = function (route, args) {
        var argKeys = {};
        for (var k in args) {
            argKeys[k.toLowerCase()] = k;
        }
        var parts = route.split('/');
        var url = '';
        for (var i = 0; i < parts.length; i++) {
            var p = parts[i];
            if (p == null) p = '';
            if (p[0] === '{' && p[p.length - 1] === '}') {
                var key = argKeys[p.substring(1, p.length - 1).toLowerCase()];
                if (key) {
                    p = args[key];
                    delete args[key];
                }
            }
            if (url.length > 0) url += '/';
            url += p;
        }
        return route[0] === '/' ? '/' + url : url;
    };
    $.ss.createUrl = function(route, args) {
        var url = $.ss.createPath(route, args);
        return $.ss.toUrl(url, args);
    };
    function splitCase(t) {
        return typeof t != 'string' ? t : t.replace(/([A-Z]|[0-9]+)/g, ' $1').replace(/_/g, ' ');
    }
    $.ss.humanize = function (s) { return !s || s.indexOf(' ') >= 0 ? s : splitCase(s); };

    function toCamelCase(key) {
        return !key ? key : key.charAt(0).toLowerCase() + key.substring(1);
    }
    $.ss.toCamelCase = toCamelCase;
    $.ss.toPascalCase = function (s) { return !s ? s : s.charAt(0).toUpperCase() + s.substring(1); };
    $.ss.normalizeKey = function (key) {
        return typeof key == "string" ? key.toLowerCase().replace(/_/g, '') : key;
    };
    $.ss.normalize = function (dto, deep) {
        if ($.isArray(dto)) {
            if (!deep) return dto;
            var to = [];
            for (var i = 0; i < dto.length; i++) {
                to[i] = $.ss.normalize(dto[i], deep);
            }
            return to;
        }
        if (typeof dto != "object") return dto;
        var o = {};
        for (var k in dto) {
            o[$.ss.normalizeKey(k)] = deep ? $.ss.normalize(dto[k], deep) : dto[k];
        }
        return o;
    };
    function sanitize(status) {
        if (status["errors"])
            return status;
        var to = {};
        for (var k in status)
            to[toCamelCase(k)] = status[k];
        to.errors = [];
        $.each(status.Errors || [], function (i, o) {
            var err = {};
            for (var k in o)
                err[toCamelCase(k)] = o[k];
            to.errors.push(err);
        });
        return to;
    }
    $.ss.parseResponseStatus = function (json, defaultMsg) {
        try {
            var err = JSON.parse(json);
            return sanitize(err.ResponseStatus || err.responseStatus);
        } catch (e) {
            return {
                message: defaultMsg,
                __error: { error: e, json: json }
            };
        }
    };
    $.ss.postJSON = function (url, data, success, error) {
        return $.ajax({
            type: "POST", url: url, dataType: "json", contentType: "application/json",
            data: typeof data == "string" ? data : JSON.stringify(data),
            success: success, error: error
        });
    };
    $.ss.createElement = createElement;
    var keyAliases = {className:'class',htmlFor:'for'};
    function createElement(tagName, options, attrs) {
        var el = document.createElement(tagName);
        if (attrs) {
            for (var key in attrs) {
                if (!attrs.hasOwnProperty(key)) continue;
                el.setAttribute(keyAliases[key] || key, attrs[key]);
            }
        }
        if (options && options.insertAfter) {
            options.insertAfter.parentNode.insertBefore(el, options.insertAfter.nextSibling);
        }
        return el;
    }
    $.ss.showInvalidInputs = function() {
        var errorMsg = this.getAttribute('data-invalid');
        if (errorMsg) {
            var isCheck = this.type === "checkbox" || this.type === "radio" || hasClass(this, 'form-check');
            var elFormCheck = isCheck ? parent(this,'form-check') : null;
            var elInputGroup = parent(this,'input-group');
            if (!isCheck)
                addClass(this, 'is-invalid');
            else
                addClass(elFormCheck || this.parentElement, 'is-invalid form-control');

            var elNext = this.nextElementSibling;
            var elLast = elInputGroup ? elInputGroup.lastElementChild : 
                (elNext && (elNext.getAttribute('for') === this.id || elNext.tagName === "SMALL")
                    ? (isCheck ? elFormCheck || elNext.parentElement : elNext)
                    : this);
            var elError = elLast.nextElementSibling && hasClass(elLast.nextElementSibling, 'invalid-feedback')
                ? elLast.nextElementSibling
                : (elInputGroup && elInputGroup.querySelector('.invalid-feedback')) 
                  || $.ss.createElement("div", { insertAfter:elLast }, { className: 'invalid-feedback' });
            elError.innerHTML = errorMsg;
        }
    };
    function parent(el,cls) {
        while (el && !hasClass(el,cls))
            el = el.parentElement;
        return el;
    }
    function hasClass(el, cls) {
        return el && (" " + el.className + " ").replace(/[\n\t\r]/g, " ").indexOf(" " + cls + " ") > -1;
    }
    function addClass(el, cls) {
        if (!hasClass(el, cls)) el.className = (el.className + " " + cls).trim();
    }
    function errorResponseExcept(status, fieldNames) {
        if (typeof fieldNames == 'string')
            fieldNames = arguments.length == 1 ? [fieldNames] : Array.prototype.slice.call(arguments);
        if (fieldNames && !(status.errors == null || status.errors.length == 0)) {
            var lowerFieldsNames = fieldNames.map(function (x) { return (x || '').toLowerCase(); });
            for (var i = 0, _a = status.errors; i < _a.length; i++) {
                var field = _a[i];
                if (lowerFieldsNames.indexOf((field.fieldName || '').toLowerCase()) !== -1) {
                    return undefined;
                }
            }
            for (var _b = 0, _c = status.errors; _b < _c.length; _b++) {
                var field = _c[_b];
                if (lowerFieldsNames.indexOf((field.fieldName || '').toLowerCase()) === -1) {
                    return field.message || field.errorCode;
                }
            }
        }
        return status.message || status.errorCode || undefined;
    }
    $.ss.errorResponseExcept = errorResponseExcept;
    $.fn.bootstrap = function(){
        $(this).find('[data-invalid]').each($.ss.showInvalidInputs);
        return this;
    };

    $.fn.setFieldError = function (name, msg) {
        $(this).applyErrors({
            errors: [{
                fieldName: name,
                message: msg
            }]
        });
    };
    $.fn.serializeMap = function () {
        var o = {};
        $.each($(this).serializeArray(), function (i, e) {
            o[e.name] = e.value;
        });
        return o;
    };
    $.fn.applyErrors = function (status, opt) {
        this.clearErrors();
        if (!status) return this;
        status = sanitize(status);

        this.addClass("has-errors");

        var bs4 = opt && opt.type === "bootstrap-v4";
        var o = $.extend({}, $.ss.validation, opt);
        if (opt && opt.messages) {
            o.overrideMessages = true;
            $.extend(o.messages, $.ss.validation.messages);
        }

        var filter = $.proxy(o.errorFilter, o),
            errors = status.errors;

        if (errors && errors.length) {
            var fieldMap = {}, fieldLabelMap = {};
            this.find("input,textarea,select,button").each(function () {
                var $el = $(this);
                var $prev = $el.prev(), $next = $el.next();
                var isCheck = this.type === "radio" || this.type === "checkbox";
                var fieldId = $el.attr("name") || this.id;
                if (!fieldId) return;

                var key = (fieldId).toLowerCase();

                fieldMap[key] = $el;
                if (!bs4) {
                    if ($prev.hasClass("help-inline") || $prev.hasClass("help-block")) {
                        fieldLabelMap[key] = $prev;
                    } else if ($next.hasClass("help-inline") || $next.hasClass("help-block")) {
                        fieldLabelMap[key] = $next;
                    }
                }
            });
            this.find(".help-inline[data-for],.help-block[data-for]").each(function () {
                var $el = $(this);
                var key = $el.data("for").toLowerCase();
                fieldLabelMap[key] = $el;
            });
            $.each(errors, function (i, error) {
                var key = (error.fieldName || "").toLowerCase();
                var $field = fieldMap[key];
                if ($field) {
                    if (!bs4) {
                        $field.addClass("error");
                        $field.parent().addClass("has-error");
                    } else {
                        var type = $field.attr('type'), isCheck = type === "radio" || type === "checkbox";
                        if (!isCheck) $field.addClass("is-invalid");
                        $field.attr("data-invalid", filter(error.message, error.errorCode, "field"));
                    }
                }
                var $lblErr = fieldLabelMap[key];
                if (!$lblErr) return;

                $lblErr.addClass("error");
                $lblErr.html(filter(error.message, error.errorCode, "field"));
                $lblErr.show();
            });

            this.find("[data-validation-summary]").each(function () {
               var fields = this.getAttribute('data-validation-summary').split(','); 
               var summaryMsg = errorResponseExcept(status, fields);
               if (summaryMsg)
                   this.innerHTML = bsAlert(summaryMsg);
            });
        } else {
            var htmlSummary = filter(status.message || splitCase(status.errorCode), status.errorCode, "summary");
            if (!bs4) {
                this.find(".error-summary").html(htmlSummary).show();
            } else {
                this.find("[data-validation-summary]").html(htmlSummary[0] === "<" ? htmlSummary : bsAlert(htmlSummary));
            }
        }
        return this;
    };
    function bsAlert(msg) { return '<div class="alert alert-danger">' + msg + '</div>'; }
    
    $.fn.clearErrors = function () {
        this.removeClass("has-errors");
        this.find(".error-summary").html("").hide();
        this.find(".help-inline.error, .help-block.error").each(function () {
            $(this).html("");
        });
        this.find(".error").each(function () {
            $(this).removeClass("error");
        });
        this.find(".has-error").each(function () {
            $(this).removeClass("has-error");
        });
        this.find("[data-validation-summary]").html("");
        this.find('.form-check.is-invalid [data-invalid]').removeAttr('data-invalid');
        this.find('.form-check.is-invalid').removeClass('form-control');
        this.find('.is-invalid').removeClass('is-invalid').removeAttr('data-invalid');
        this.find('.is-valid').removeClass('is-valid');
    };
    $.fn.bindForm = function (orig) { //bootstrap v3
        return this.each(function () {
            var f = $(this);
            if (orig && orig.model)
                $.ss.populateForm(this,orig.model);
            f.submit(function (e) {
                e.preventDefault();
                return $(f).ajaxSubmit(orig);
            });
        });
    };
    $.fn.bootstrapForm = function (orig) { //bootstrap v4
        return this.each(function () {
            var f = $(this);
            if (orig.model) 
                $.ss.populateForm(this,orig.model);
            f.submit(function (e) {
                e.preventDefault();
                orig.type = "bootstrap-v4";
                return $(f).ajaxSubmit(orig);
            });
        });
    };
    $.fn.ajaxSubmit = function (orig) {
        orig = orig || {};
        var bs4 = orig.type === "bootstrap-v4";
        if (orig.validation) {
            $.extend($.ss.validation, orig.validation);
        }

        return this.each(function () {
            var f = $(this);
            f.clearErrors();
            try {
                if (orig.validate && orig.validate.call(f) === false)
                    return false;
            } catch (e) {
                return false;
            }
            f.addClass("loading");
            var $disable = $(orig.onSubmitDisable || $.ss.onSubmitDisable, f);
            $disable.attr("disabled", "disabled");
            var opt = $.extend({}, orig, {
                type: f.attr('method') || "POST",
                url: f.attr('action'),
                data: f.serialize(),
                accept: "application/json",
                error: function (jq, jqStatus, statusText) {
                    var err, errMsg = "The request failed with " + (statusText || jq.statusText);
                    try {
                        err = JSON.parse(jq.responseText);
                    } catch (e) {
                    }
                    if (!err) {
                        f.addClass("has-errors");
                        f.find(".error-summary").html(errMsg);
                        if (bs4) {
                            var elSummary = f.find('[data-validation-summary]');
                            elSummary.html('<div class="alert alert-danger">' + errMsg + '</div>');
                        }
                    } else {
                        f.applyErrors(err.ResponseStatus || err.responseStatus, {type:orig.type});
                    }
                    if (orig.error) {
                        orig.error.apply(this, arguments);
                    }
                    if (bs4) {
                        f.find('[data-invalid]').each($.ss.showInvalidInputs);
                    }
                },
                complete: function (jq) {
                    f.removeClass("loading");
                    $disable.removeAttr("disabled");
                    if (orig.complete) {
                        orig.complete.apply(this, arguments);
                    }
                    var loc = jq.getResponseHeader("X-Location");
                    if (loc) {
                        location.href = loc;
                    }
                    var evt = jq.getResponseHeader("X-Trigger");
                    if (evt) {
                        var pos = evt.indexOf(':');
                        var cmd = pos >= 0 ? evt.substring(0, pos) : evt;
                        var data = pos >= 0 ? evt.substring(pos + 1) : null;
                        f.trigger(cmd, data ? [data] : []);
                    }
                },
                dataType: "json"
            });
            $.ajax(opt);
            return false;
        });
    };
    $.fn.applyValues = function (map) {
        return this.each(function () {
            var $el = $(this);
            $.each(map, function (k, v) {
                $el.find("#" + k + ",[name=" + k + "]").val(v);
            });
            $el.find("[data-html]").each(function () {
                $(this).html(map[$(this).data("html")] || "");
            });
            $el.find("[data-val]").each(function () {
                $(this).val(map[$(this).data("val")] || "");
            });
            $el.find("[data-src]").each(function () {
                $(this).attr("src", map[$(this).data("src")] || "");
            });
            $el.find("[data-href]").each(function () {
                $(this).attr("href", map[$(this).data("href")] || "");
            });
        });
    };
    $.ss.__call = $.ss.__call || function (e) {
        var $el = $(e.target);
        var attr = $el.data(e.type) || $el.closest("[data-" + e.type + "]").data(e.type);
        if (!attr) return;

        var pos = attr.indexOf(':'), fn;
        if (pos >= 0) {
            var cmd = attr.substring(0, pos);
            var data = attr.substring(pos + 1);
            if (cmd === 'trigger') {
                $el.trigger(data, [e.target]);
            } else {
                fn = $.ss.handlers[cmd];
                if (fn) {
                    fn.apply(e.target, data.split(','));
                }
            }
        } else {
            fn = $.ss.handlers[attr];
            if (fn) {
                fn.apply(e.target, [].slice.call(arguments));
            }
        }
    };
    $.ss.listenOn = 'click dblclick change focus blur focusin focusout select keydown keypress keyup hover toggle input';
    $.fn.bindHandlers = function (handlers) {
        $.extend($.ss.handlers, handlers || {});
        return this.each(function () {
            var $el = $(this);
            $el.off($.ss.listenOn, $.ss.__call);
            $el.on($.ss.listenOn, $.ss.__call);
        });
    };

    $.ss.populateForm = function(form, model) {
        if (!model)
            return;
        var toggleCase = function (s) { return !s ? s :
            s[0] === s[0].toUpperCase() ? exports.toCamelCase(s) : s[0] === s[0].toLowerCase() ? exports.toPascalCase(s) : s; };
        for (var key in model) {
            var val = model[key];
            if (typeof val == 'undefined' || val === null)
                val = '';
            var el = form.elements.namedItem(key) || form.elements.namedItem(toggleCase(key));
            var input = el;
            if (!el)
                continue;
            var type = input.type || el[0].type;
            switch (type) {
                case 'radio':
                case 'checkbox':
                    var len = el.length;
                    for (var i = 0; i < len; i++) {
                        el[i].checked = (val.indexOf(el[i].value) > -1);
                    }
                    break;
                case 'select-multiple':
                    var values = $.isArray(val) ? val : [val];
                    var select = el;
                    for (var i = 0; i < select.options.length; i++) {
                        select.options[i].selected = (values.indexOf(select.options[i].value) > -1);
                    }
                    break;
                case 'select':
                case 'select-one':
                    input.value = val.toString() || val;
                    break;
                case 'date':
                    var d = exports.toDate(val);
                    if (d)
                        input.value = d.toISOString().split('T')[0];
                    break;
                default:
                    input.value = val;
                    break;
            }
        }
    };

    $.fn.setActiveLinks = function () {
        var url = window.location.href;
        return this.each(function () {
            $(this).filter(function () {
                return this.href === url;
            })
            .addClass('active')
            .closest("li").addClass('active');
        });
    };

    $.ss.eventSourceStop = false;
    $.ss.eventOptions = {};
    $.ss.eventReceivers = {};
    $.ss.eventChannels = [];
    $.ss.eventSourceUrl = null;
    $.ss.updateSubscriberUrl = null;
    $.ss.updateChannels = function(channels) {
        $.ss.eventChannels = channels;
        if (!$.ss.eventSource) return;
        var url = $.ss.eventSource.url;
        var qs = $.ss.queryString(url);
        qs['channels'] = channels;
        delete qs['channel'];
        var pos = url.indexOf('?');
        $.ss.eventSourceUrl = $.ss.toUrl(pos >= 0 ? url.substring(0, pos) : url, qs);
    };
    $.ss.updateSubscriberInfo = function (subscribe, unsubscribe) {
        var sub = typeof subscribe == "string" ? subscribe.split(',') : subscribe;
        var unsub = typeof unsubscribe == "string" ? unsubscribe.split(',') : unsubscribe;
        var channels = [];
        for (var i in $.ss.eventChannels) {
            var c = $.ss.eventChannels[i];
            if (unsub == null || $.inArray(c, unsub) === -1) {
                channels.push(c);
            }
        }
        if (sub) {
            for (var i in sub) {
                var c = sub[i];
                if ($.inArray(c, channels) === -1) {
                    channels.push(c);
                }
            }
        }
        $.ss.updateChannels(channels);
    };
    $.ss.subscribeToChannels = function (channels, cb, cbError) {
        return $.ss.updateSubscriber({ SubscribeChannels: channels.join(',') }, cb, cbError);
    };
    $.ss.unsubscribeFromChannels = function (channels, cb, cbError) {
        return $.ss.updateSubscriber({ UnsubscribeChannels: channels.join(',') }, cb, cbError);
    };
    $.ss.updateSubscriber = function (data, cb, cbError) {
        if (!$.ss.updateSubscriberUrl)
            throw new Error("updateSubscriberUrl was not populated");
        return $.ajax({
            type: "POST",
            url: $.ss.updateSubscriberUrl,
            data: data,
            dataType: "json",
            success: function (r) {
                $.ss.updateSubscriberInfo(data.SubscribeChannels, data.UnsubscribeChannels);
                r.channels = $.ss.eventChannels;
                if (cb != null)
                    cb(r);
            },
            error: function (e) {
                $.ss.reconnectServerEvents({ errorArgs: arguments });
                if (cbError != null)
                    cbError(e);
            }
        });
    };
    $.ss.reconnectServerEvents = function (opt) {
        if ($.ss.eventSourceStop) return;
        opt = opt || {};
        var hold = $.ss.eventSource;
        var es = new EventSource(opt.url || $.ss.eventSourceUrl || hold.url, { withCredentials:hold.withCredentials });
        es.onerror = opt.onerror || hold.onerror;
        es.onmessage = opt.onmessage || hold.onmessage;
        var fn = $.ss.handlers["onReconnect"];
        if (fn != null)
            fn.apply(es, opt.errorArgs);
        hold.close();
        return $.ss.eventSource = es;
    };
    $.ss.invokeReceiver = function (r, cmd, el, msg, e, name) {
        if (r) {
            if (typeof (r[cmd]) == "function") {
                r[cmd].call(el || r[cmd], msg, e);
            } else {
                r[cmd] = msg;
            }
        }
    };
    $.ss.onUnload = function () {
        if (!$.ss.unRegisterUrl) return;
        if (navigator.sendBeacon)
            navigator.sendBeacon($.ss.unRegisterUrl);
        else
            $.ajax({ type: 'POST', url: $.ss.unRegisterUrl, async: false });
    };
    $.fn.handleServerEvents = function (opt) {
        $.ss.eventSource = this[0];
        $.ss.eventOptions = opt = opt || {};
        if (opt.handlers) {
            $.extend($.ss.handlers, opt.handlers || {});
        }
        $(window).on("unload", $.ss.onUnload);
        function onMessage(e) {
            var parts = $.ss.splitOnFirst(e.data, ' ');
            var selector = parts[0];
            var selParts = $.ss.splitOnFirst(selector, '@');
            if (selParts.length > 1) {
                e.channel = selParts[0];
                selector = selParts[1];
            }
            var json = parts[1];
            var msg, fn;
            try {
                msg = json ? JSON.parse(json) : null;
            } catch (e) {
                e.json = json;
                fn = $.ss.handlers["onError"];
                if (fn)
                    fn("JSON.parse() error", e);
            }

            parts = $.ss.splitOnFirst(selector, '.');
            if (parts.length <= 1)
                throw "invalid selector format: " + selector;

            var op = parts[0],
                target = parts[1].replace(new RegExp("%20", 'g'), " ");

            if (opt.validate && opt.validate(op, target, msg, json) === false)
                return;

            var tokens = $.ss.splitOnFirst(target, '$'),
                cmd = tokens[0], cssSel = tokens[1],
                $els = cssSel && $(cssSel), el = $els && $els[0];

            $.extend(e, { cmd: cmd, op: op, selector: selector, "$target": target, cssSelector: cssSel, json: json }); //target is readonly field
            if (op === "cmd") {
                if (cmd === "onConnect") {
                    $.extend(opt, msg);
                    if (opt.heartbeatUrl) {
                        $.ss.CONNECT_ID = $.ss.CONNECT_ID ? $.ss.CONNECT_ID + 1 : 1;
                        (function(connectId) {
                            function sendHeartbeat() {
                                if ($.ss.eventSource == null || connectId !== $.ss.CONNECT_ID) // Only allow latest connections heartbeat callback through 
                                    return;
                                if ($.ss.eventSource.readyState === 2) //CLOSED
                                {
                                    var stopFn = $.ss.handlers["onStop"];
                                    if (stopFn != null)
                                        stopFn.apply($.ss.eventSource);
                                    $.ss.reconnectServerEvents({ errorArgs: { error:'CLOSED' } });
                                    return;
                                }
                                $.ajax({
                                    type: "POST",
                                    url: opt.heartbeatUrl,
                                    data: null,
                                    dataType: "text",
                                    success: function (r) {
                                        setTimeout(sendHeartbeat, parseInt(opt.heartbeatIntervalMs) || 10000)
                                    },
                                    error: function () {
                                        $.ss.reconnectServerEvents({ errorArgs: arguments });
                                    }
                                });
                            }
                            setTimeout(sendHeartbeat, parseInt(opt.heartbeatIntervalMs) || 10000);
                        })($.ss.CONNECT_ID);
                    }
                    $.ss.unRegisterUrl = opt.unRegisterUrl;
                    $.ss.updateSubscriberUrl = opt.updateSubscriberUrl;
                    $.ss.updateChannels((opt.channels || "").split(','));
                }
                fn = $.ss.handlers[cmd];
                if (fn) {
                    fn.call(el || document.body, msg, e);
                }
            }
            else if (op === "trigger") {
                $(el || document).trigger(cmd, [msg, e]);
            }
            else if (op === "css") {
                $($els || document.body).css(cmd, msg, e);
            }
            else {
                var r = opt.receivers && opt.receivers[op] || $.ss.eventReceivers[op];
                $.ss.invokeReceiver(r, cmd, el, msg, e, op);
            }

            fn = $.ss.handlers["onMessage"];
            if (fn) fn.call(el || document.body, msg, e);

            if (opt.success) opt.success(selector, msg, e); //deprecated
        }

        $.ss.eventSource.onmessage = onMessage;

        var hold = $.ss.eventSource.onerror;
        $.ss.eventSource.onerror = function () {
            var args = arguments;
            window.setTimeout(function () {
                $.ss.reconnectServerEvents({ errorArgs: args });
                if (hold)
                    hold.apply(args);
            }, 10000);
        };
    };
});
function reset() {
    $.ss.eventSourceStop = false;
    $.ss.eventOptions = {};
    $.ss.eventReceivers = {};
    $.ss.eventChannels = [];
    $.ss.eventSourceUrl = $.ss.updateSubscriberUrl = $.ss.eventOptions = $.ss.eventSource = null;
}
$.ss.disposeServerEvents = function (cb) {
  var unRegisterUrl = $.ss.eventOptions && $.ss.eventOptions.unRegisterUrl;
  if ($.ss.eventSource) $.ss.eventSource.close();
  reset();

  if (unRegisterUrl) {
      $.ajax({ type: 'POST', url: unRegisterUrl, complete: function() { if (cb) cb(); } });
  } else {
      if (cb) cb();
  }
};