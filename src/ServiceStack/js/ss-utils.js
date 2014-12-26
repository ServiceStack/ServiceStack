(function ($) {

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
    $.ss.clearAdjacentError = function() {
        $(this).removeClass("error");
        $(this).prev(".help-inline,.help-block").removeClass("error").html("");
        $(this).next(".help-inline,.help-block").removeClass("error").html("");
    };
    $.ss.todate = function(s) { return new Date(parseFloat(/Date\(([^)]+)\)/.exec(s)[1])); };
    $.ss.todfmt = function (s) { return $.ss.dfmt($.ss.todate(s)); };
    function pad(d) { return d < 10 ? '0' + d : d; };
    $.ss.dfmt = function (d) { return d.getFullYear() + '/' + pad(d.getMonth() + 1) + '/' + pad(d.getDate()); };
    $.ss.dfmthm = function (d) { return d.getFullYear() + '/' + pad(d.getMonth() + 1) + '/' + pad(d.getDate()) + ' ' + pad(d.getHours()) + ":" + pad(d.getMinutes()); };
    $.ss.tfmt12 = function (d) { return pad(d.getHours()) + ":" + pad(d.getMinutes()) + ":" + pad(d.getSeconds()) + " " + (d.getHours() > 12 ? "PM" : "AM"); };
    $.ss.splitOnFirst = function (s, c) { if (!s) return [s]; var pos = s.indexOf(c); return pos >= 0 ? [s.substring(0, pos), s.substring(pos + 1)] : [s]; };
    $.ss.splitOnLast = function (s, c) { if (!s) return [s]; var pos = s.lastIndexOf(c); return pos >= 0 ? [s.substring(0, pos), s.substring(pos + 1)] : [s]; };
    $.ss.getSelection = function () {
        return window.getSelection
            ? window.getSelection().toString()
            : document.selection && document.selection.type != "Control"
                ? document.selection.createRange().text : "";
    };
    $.ss.queryString = function(url) {
        if (!url) return {};
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
    $.ss.createUrl = function(route, args) {
        if (!args) args = {};
        var argKeys = {};
        for (var k in args) {
            argKeys[k.toLowerCase()] = k;
        }
        var parts = route.split('/');
        var url = '';
        for (var i = 0; i < parts.length; i++) {
            var p = parts[i];
            if (p == null) p = '';
            if (p[0] == '{' && p[p.length - 1] == '}') {
                var key = argKeys[p.substring(1, p.length - 1).toLowerCase()];
                if (key) {
                    p = args[key];
                    delete args[key];
                }
            }
            if (url.length > 0) url += '/';
            url += p;
        }
        return url;
    };

    function splitCase(t) {
        return typeof t != 'string' ? t : t.replace( /([A-Z]|[0-9]+)/g , ' $1').replace( /_/g , ' ');
    };
    $.ss.humanize = function(s) { return !s || s.indexOf(' ') >= 0 ? s : splitCase(s); };

    function toCamelCase(key) {
        return !key ? key : key.charAt(0).toLowerCase() + key.substring(1);
    }

    function sanitize(status) {
        if (status["errors"])
            return status;
        var to = {};
        for (var k in status)
            to[toCamelCase(k)] = status[k];
        to.errors = [];
        $.each(status.Errors || [], function(i, o) {
            var err = {};
            for (var k in o)
                err[toCamelCase(k)] = o[k];
            to.errors.push(err);
        });
        return to;
    }

    $.fn.setFieldError = function(name, msg) {
        $(this).applyErrors({
            errors: [{
                fieldName: name,
                message: msg
            }]
        });
    };

    $.fn.serializeMap = function() {
        var o = {};
        $.each($(this).serializeArray(), function(i, e) {
            o[e.name] = e.value;
        });
        return o;
    };

    $.fn.applyErrors = function (status, opt) {
        this.clearErrors();
        if (!status) return this;
        status = sanitize(status);

        this.addClass("has-errors");

        var o = $.extend({}, $.ss.validation, opt);
        if (opt && opt.messages) {
            o.overrideMessages = true;
            $.extend(o.messages, $.ss.validation.messages);
        }

        var filter = $.proxy(o.errorFilter, o), 
            errors = status.errors;

        if (errors && errors.length) {
            var fieldMap = { }, fieldLabelMap = {};
            this.find("input,textarea,select,button").each(function() {
                var $el = $(this);
                var $prev = $el.prev(), $next = $el.next();
                var fieldId = this.id || $el.attr("name");
                if (!fieldId) return;
                
                var key = (fieldId).toLowerCase();

                fieldMap[key] = $el;
                if ($prev.hasClass("help-inline") || $prev.hasClass("help-block")) {
                    fieldLabelMap[key] = $prev;
                } else if ($next.hasClass("help-inline") || $next.hasClass("help-block")) {
                    fieldLabelMap[key] = $next;
                }
            });
            this.find(".help-inline[data-for],.help-block[data-for]").each(function() {
                var $el = $(this);
                var key = $el.data("for").toLowerCase();
                fieldLabelMap[key] = $el;
            });
            $.each(errors, function (i, error) {
                var key = (error.fieldName || "").toLowerCase();
                var $field = fieldMap[key];
                if ($field) {
                    $field.addClass("error");
                    $field.parent().addClass("has-error");
                }
                var $lblErr = fieldLabelMap[key];
                if (!$lblErr) return;

                $lblErr.addClass("error");
                $lblErr.html(filter(error.message, error.errorCode, "field"));
                $lblErr.show();
            });
        } else {
            this.find(".error-summary")
                .html(filter(status.message || splitCase(status.errorCode), status.errorCode, "summary"))
                .show();
        }
        return this;
    };

    $.fn.clearErrors = function() {
        this.removeClass("has-errors");
        this.find(".error-summary").html("").hide();
        this.find(".help-inline.error, .help-block.error").each(function () {
            $(this).html("");
        });
        this.find(".error").each(function () {
            $(this).removeClass("error");
        });
        return this.find(".has-error").each(function () {
            $(this).removeClass("has-error");
        });
    };
    
    $.fn.bindForm = function (orig) {
        return this.each(function () {
            orig = orig || {};
            if (orig.validation) {
                $.extend($.ss.validation, orig.validation);
            }
            
            var f = $(this);
            f.submit(function (e) {
                e.preventDefault();
                f.clearErrors();
                try {
                    if (orig.validate && orig.validate.call(f) === false)
                        return false;
                } catch (e) { return false; }
                f.addClass("loading");
                var $disable = $(orig.onSubmitDisable || $.ss.onSubmitDisable, f);
                $disable.attr("disabled", "disabled");
                var opt = $.extend({}, orig, {
                    type: f.attr('method') || "POST",
                    url: f.attr('action'),
                    data: f.serialize(),
                    accept: "application/json",
                    error: function (jq, jqStatus, statusText) {
                        var err, errMsg = "The request failed with " + statusText;
                        try {
                            err = JSON.parse(jq.responseText);
                        } catch (e) { }
                        if (!err) {
                            f.addClass("has-errors");
                            f.find(".error-summary").html(errMsg);
                        } else {
                            f.applyErrors(err.ResponseStatus || err.responseStatus);
                        }
                        if (orig.error) {
                            orig.error.apply(this, arguments);
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
                            var pos = attr.indexOf(':');
                            var cmd = pos >= 0 ? evt.substring(0, pos) : evt;
                            var data = pos >= 0 ? evt.substring(pos + 1) : null;
                            f.trigger(cmd, data ? [data] : []);
                        }
                    },
                    dataType: "json",
                });
                $.ajax(opt);
                return false;
            });
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
            if (cmd == 'trigger') {
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
                fn.apply(e.target, [].splice(arguments));
            }
        }
    };
    $.ss.listenOn = 'click dblclick change focus blur focusin focusout select keydown keypress keyup hover toggle';
    $.fn.bindHandlers = function (handlers) {
        $.extend($.ss.handlers, handlers || {});
        return this.each(function () {
            var $el = $(this);
            $el.off($.ss.listenOn, $.ss.__call);
            $el.on($.ss.listenOn, $.ss.__call);
        });
    };
    
    $.fn.setActiveLinks = function () {
        var url = window.location.href;
        return this.each(function () {
            $(this).filter(function () {
                return this.href == url;
            })
            .addClass('active')
            .closest("li").addClass('active');
        });
    };

    $.ss.eventReceivers = {};
    $.ss.reconnectServerEvents = function(opt) {
        opt = opt || {};
        var hold = $.ss.eventSource;
        var es = new EventSource(opt.url || hold.url);
        es.onerror = opt.onerror || hold.onerror;
        es.onmessage = opt.onmessage || hold.onmessage;
        var fn = $.ss.handlers["onReconnect"];
        if (fn != null)
            fn.apply(es, opt.errorArgs);
        hold.close();
        return $.ss.eventSource = es;
    };
    $.fn.handleServerEvents = function (opt) {
        $.ss.eventSource = this[0];
        opt = opt || {};
        if (opt.handlers) {
            $.extend($.ss.handlers, opt.handlers);
        }
        function onMessage(e) {
            var parts = $.ss.splitOnFirst(e.data, ' ');
            var selector = parts[0];
            var selParts = $.ss.splitOnFirst(selector, '@');
            if (selParts.length > 1) {
                e.channel = selParts[0];
                selector = selParts[1];
            }
            var json = parts[1];
            var msg = json ? JSON.parse(json) : null;

            parts = $.ss.splitOnFirst(selector, '.');
            var op = parts[0],
                target = parts[1].replace(new RegExp("%20",'g')," ");

            if (opt.validate && opt.validate(op, target, msg, json) === false)
                return;

            var tokens = $.ss.splitOnFirst(target, '$'), 
                cmd = tokens[0], cssSel = tokens[1],
                $els = cssSel && $(cssSel), el = $els && $els[0];
            if (op == "cmd") {
                if (cmd == "onConnect") {
                    $.extend(opt, msg);
                    if (opt.heartbeatUrl) {
                        if (opt.heartbeat) {
                            window.clearInterval(opt.heartbeat);
                        }
                        opt.heartbeat = window.setInterval(function () {
                            $.ajax({
                                type: "POST",
                                url: opt.heartbeatUrl,
                                data: null,
                                success: function (r) { },
                                error: function () {
                                    $.ss.reconnectServerEvents({errorArgs:arguments});
                                }
                            });
                        }, parseInt(opt.heartbeatIntervalMs) || 10000);
                    }
                    if (opt.unRegisterUrl) {
                        $(window).unload(function () {
                            $.post(opt.unRegisterUrl, null, function(r) {});
                        });
                    }
                }
                var fn = $.ss.handlers[cmd];
                if (fn) {
                    fn.call(el || document.body, msg, e);
                }
            }
            else if (op == "trigger") {
                $(el || document).trigger(cmd, [msg, e]);
            }
            else if (op == "css") {
                $($els || document.body).css(cmd, msg, e);
            }
            else {
                var r = opt.receivers && opt.receivers[op] || $.ss.eventReceivers[op];
                if (r) {
                    if (typeof (r[cmd]) == "function") {
                        r[cmd].call(el || r[cmd], msg, e);
                    } else {
                        r[cmd] = msg;
                    }
                }
            }

            if (opt.success) {
                opt.success(selector, msg, e);
            }
        }
        $.ss.eventSource.onmessage = onMessage;
    };

})(window.jQuery);
