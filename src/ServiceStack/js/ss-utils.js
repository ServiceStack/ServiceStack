(function ($) {

    if (!$.ss) $.ss = {};
    if (!$.ss.validation)
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
            this.find("input").each(function() {
                var $el = $(this);
                var $prev = $el.prev(), $next = $el.next();
                var key = (this.id || $el.attr("name")).toLowerCase();

                fieldMap[key] = $el;
                if ($prev.hasClass("help-inline") || $prev.hasClass("help-block")) {
                    fieldLabelMap[key] = $prev;
                } else if ($next.hasClass("help-inline") || $next.hasClass("help-block")) {
                    fieldLabelMap[key] = $next;
                }
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
            this.find(".error-summary").html(
                filter(status.message || splitCase(status.errorCode), status.errorCode, "summary")
            ).show();
        }
        return this;
    };

    $.fn.clearErrors = function() {
        this.removeClass("has-errors");
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
    
    $.fn.submitForm = function (orig) {
        return this.each(function () {
            orig = orig || {};
            if (orig.validation) {
                $.extend($.ss.validation, orig.validation);
            }
            
            var f = $(this);
            f.submit(function () {
                f.clearErrors();
                f.addClass("loading");
                var opt = $.extend({}, orig, {
                    type: f.attr('method') || "POST",
                    url: f.attr('action'),
                    data: f.serialize(),
                    accept: "application/json",
                    error: function (jq, jqStatus, statusText) {
                        var err, errMsg = "The request failed with " + statusText;
                        try {
                            err = JSON.parse(jq.responseText);
                        } finally { }
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
                    complete: function () {
                        f.removeClass("loading");
                        if (orig.complete) {
                            orig.complete.apply(this, arguments);
                        }
                    },
                    dataType: "json",
                });
                $.ajax(opt);
                return false;
            });
        });
    };


})(window.jQuery);
