/// <reference path="../jquery-1.7.js" />
/// <reference path="../underscore.js" />
/// <reference path="../backbone.js" />
/// <reference path="ss-validation.js" />
(function (root)
{
    $.ss.validation.overrideMessages = true;

	var app = root.App = root.App || {};
    var emptyFn = function() {};
    
	_.mixin({
	    cmdHandler: function (handlers)
	    {
	        $(document.body).click(function (e) {
	            var dataCmd = $(e.srcElement || e.target).data('cmd');
	            if (!dataCmd) return;

	            var cmd = dataCmd.split(':'),
					evt = cmd[0],
					args = cmd.length > 1 ? cmd[1].split(',') : [];

	            if (_.isFunction(handlers[evt]))
	                handlers[evt].apply(handlers, args);
	        });
	    },
	    setFieldError: function(field, msg) {
	        var $field = $(field), form = $field.parents("form");
	        $field.parent().add(form).addClass("error");
	        if (msg)
	            $field.next().html(msg);
	    },
		formData: function (form)
		{
			var ret = {};
			$(form).find("input,textarea").each(function() {
				if (this.type == "button" || this.type == "submit") return;
				var value = this.type == "checkbox"
			        ? this.checked.toString()
			        : $(this).val();
			    ret[this.name] = value;
			});
			return ret;
		},
		xhrMessage: function (xhr)
		{
			try
			{
				var respObj = JSON.parse(xhr.responseText);
				if (!respObj.responseStatus) return null;
				return respObj.responseStatus.message;
			}
			catch (e)
			{
				return null;
			}
		},
        get: function (url, data, success, error) {
            if (_.isFunction(data)) {
                success = data;
                error = success;
                data = undefined;
            }
            return _.ajax({
                type: 'GET',
                url: url,
                data: data,
                success: success,
                error: error
            });
        },
		post: function (opt) {
		    return _.ajax(opt);
		},
        ajax: function (opt)
		{
            var o = _.defaults(opt, {
               type: 'POST',
               loading: function() {
                   $(document.body).add(opt.form).addClass("loading");
               },
               finishedLoading: function() {
                   $(document.body).add(opt.form).removeClass("loading");
               },
               dataType: "json"
            });
			o.loading();
			$.ajax({
				type: o.type,
				url: o.url,
				data: o.data,
				success: function()
				{
					//console.log(arguments);
					o.finishedLoading();
				    $(o.form).clearErrors();
					if (o.success) o.success.apply(null, arguments);
				},
				error: function(xhr,err,status)
				{
					//console.log(arguments);
					o.finishedLoading();
				    try {
				        if (o.form) {
				            var r = JSON.parse(xhr.responseText);
				            $(o.form).applyErrors(r && r.responseStatus);
				        }
				    } catch(e){}
					(o.error || (app.error || emptyFn)).apply(null, arguments);
				},
				dataType: o.dataType
			});
		}
	});

})(window);
