// Copyright (c) Microsoft Corporation.  All rights reserved.
// This code is licensed by Microsoft Corporation under the terms
// of the MICROSOFT REACTIVE EXTENSIONS FOR JAVASCRIPT AND .NET LIBRARIES License.
// See http://go.microsoft.com/fwlink/?LinkId=186234.

(function()
{
    var _jQuery = jQuery;
    var proto = _jQuery.fn;
    var global = this;
    var root;
    if (typeof ProvideCustomRxRootObject == "undefined")
    {
        root = global.Rx;
    }
    else
    {
        root = ProvideCustomRxRootObject();
    }
    var observable = root.Observable;
    var asyncSubject = root.AsyncSubject;
    var observableCreate = observable.Create;
    var disposableEmpty = root.Disposable.Empty;

    proto.toObservable = function(eventType, eventData)
    {
        var parent = this;
        return observableCreate(function(observer)
        {
            var handler = function(eventObject)
            {
                observer.OnNext(eventObject);
            };
            parent.bind(eventType, eventData, handler);
            return function()
            {
                parent.unbind(eventType, handler);
            };
        });
    };

    proto.toLiveObservable = function(eventType, eventData)
    {
        var parent = this;
        return observableCreate(function(observer)
        {
            var handler = function(eventObject)
            {
                observer.OnNext(eventObject);
            };
            parent.live(eventType, eventData, handler);
            return function()
            {
                parent.die(eventType, handler);
            };
        });
    };

    proto.hideAsObservable = function(duration)
    {
        var subject = new asyncSubject();
        this.hide(duration, function()
        {
            subject.OnNext(this);
            subject.OnCompleted();
        });
        return subject;
    }

    proto.showAsObservable = function(duration)
    {
        var subject = new asyncSubject();
        this.show(duration, function()
        {
            subject.OnNext(this);
            subject.OnCompleted();
        });
        return subject;
    }

    proto.animateAsObservable = function(properties, duration, easing)
    {
        var subject = new asyncSubject();
        this.animate(properties, duration, easing, function()
        {
            subject.OnNext(this);
            subject.OnCompleted();
        });
        return subject;
    }

    proto.fadeInAsObservable = function(duration)
    {
        var subject = new asyncSubject();
        this.fadeIn(duration, function()
        {
            subject.OnNext(this);
            subject.OnCompleted();
        });
        return subject;
    }

    proto.fadeToAsObservable = function(duration, opacity)
    {
        var subject = new asyncSubject();
        this.fadeTo(duration, opacity, function()
        {
            subject.OnNext(this);
            subject.OnCompleted();
        });
        return subject;
    }

    proto.fadeOutAsObservable = function(duration)
    {
        var subject = new asyncSubject();
        this.fadeOut(duration, function()
        {
            subject.OnNext(this);
            subject.OnCompleted();
        });
        return subject;
    }

    proto.slideDownAsObservable = function(duration)
    {
        var subject = new asyncSubject();
        this.slideDown(duration, function()
        {
            subject.OnNext(this);
            subject.OnCompleted();
        });
        return subject;
    }

    proto.slideUpAsObservable = function(duration)
    {
        var subject = new asyncSubject();
        this.slideUp(duration, function()
        {
            subject.OnNext(this);
            subject.OnCompleted();
        });
        return subject;
    }

    proto.slideToggleAsObservable = function(duration)
    {
        var subject = new asyncSubject();
        this.slideToggle(duration, function()
        {
            subject.OnNext(this);
            subject.OnCompleted();
        });
        return subject;
    }

    var ajaxAsObservable = _jQuery.ajaxAsObservable = function(settings)
    {
        var internalSettings = {};
        for (var k in settings)
        {
            internalSettings[k] = settings[k];
        }
        var subject = new asyncSubject();
        internalSettings.success = function(data, textStatus, xmlHttpRequest)
        {
            subject.OnNext({ data: data, textStatus: textStatus, xmlHttpRequest: xmlHttpRequest });
            subject.OnCompleted();
        };
        internalSettings.error = function(xmlHttpRequest, textStatus, errorThrown)
        {
            subject.OnError({ xmlHttpRequest: xmlHttpRequest, textStatus: textStatus, errorThrown: errorThrown });
        };
        _jQuery.ajax(internalSettings);
        return subject;
    };

    _jQuery.getJSONAsObservable = function(url, data)
    {
        return ajaxAsObservable({ url: url, dataType: 'json', data: data });
    };

    _jQuery.getScriptAsObservable = function(url, data)
    {
        return ajaxAsObservable({ url: url, dataType: 'script', data: data });
    };

    _jQuery.postAsObservable = function(url, data)
    {
        return ajaxAsObservable({ url: url, type: 'POST', data: data });
    };

    proto.loadAsObservable = function(url, params)
    {
        var subject = new asyncSubject();
        var callback = function(response, status, xmlHttpRequest)
        {
            if (status === "error") 
            {
                subject.OnError({ response : response, status : status, xmlHttpRequest: xmlHttpRequest });
            } 
            else 
            {
               subject.OnNext({ response : response, status : status, xmlHttpRequest: xmlHttpRequest });
               subject.OnCompleted();
            }
        };
        this.load(url, params, callback);
        return subject;        
    };       
    
    _jQuery.getScriptAsObservable = function(url)
    {
        return ajaxAsObservable({ url: url, dataType: 'script'});
    };    
    
    _jQuery.postAsObservable = function(url, data, type )
    {
        return ajaxAsObservable({ url : url, dataType : type, data : data, type : "POST" });
    };

})();