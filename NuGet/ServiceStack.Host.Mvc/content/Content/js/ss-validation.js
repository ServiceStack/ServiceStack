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
    
    $.fn.applyErrors = function(responseStatus, opt) {
        this.clearErrors();
        if (!responseStatus) return this;
        
        this.addClass("error");

        var o = _.defaults({}, opt, $.ss.validation);
        if (opt && opt.messages) {
            o.overrideMessages = true;
            _.extend(o.messages, $.ss.validation.messages);
        }

        var filter = _.bind(o.errorFilter, o), 
            errors = responseStatus.errors;

        console.log(errors, responseStatus);

        if (errors && errors.length) {
            var fieldMap = { };
            this.find("input").each(function() {
                var $el = $(this);
                var $prev = $el.prev(), $next = $el.next();

                if ($prev.hasClass("help-inline") || $prev.hasClass("help-block")) {
                    fieldMap[(this.id || $el.attr("name")).toLowerCase()] = $prev;
                } else if ($next.hasClass("help-inline") || $next.hasClass("help-block")) {
                    fieldMap[(this.id || $el.attr("name")).toLowerCase()] = $next;
                }
            });
            _.each(errors, function(error) {
                var $field = fieldMap[(error.fieldName||"").toLowerCase()];
                if (!$field) return;

                $field.parent().addClass("error");
                $field.html(filter(error.message, error.errorCode, "field"));
                $field.show();
            });
        } else {
            this.find(".error-summary").html(
                filter(responseStatus.message || splitCase(responseStatus.errorCode), responseStatus.errorCode, "summary")
            );
        }
        return this;
    };

    $.fn.clearErrors = function() {
        this.removeClass("error");
        this.find(".error>.help-inline, .error>.help-block").each(function () {
            $(this).html("");
        });
        return this.find(".error").each(function() {
            $(this).removeClass("error");
        });
    };

})(window.jQuery);
