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

    function splitCase(t) {
        return typeof t != 'string' ? t : t.replace( /([A-Z]|[0-9]+)/g , ' $1').replace( /_/g , ' ');
    };

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
        });
    };
    $.ss.__call = $.ss.__call || function (e) {
        var $el = $(e.target);
        var attr = $el.data(e.type) || $el.closest("[data-" + e.type + "]").data(e.type);
        if (!attr) return;
        var pos = attr.indexOf(':');
        if (pos >= 0) {
            var cmd = attr.substring(0, pos);
            var data = attr.substring(pos + 1);
            if (cmd == 'trigger') {
                $el.trigger(data, [e.target]);
            }
        } else {
            var fn = $.ss.handlers[attr];
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

})(window.jQuery);
