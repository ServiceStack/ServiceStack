// Copyright (c) Microsoft Corporation.  All rights reserved.
// This code is licensed by Microsoft Corporation under the terms
// of the MICROSOFT REACTIVE EXTENSIONS FOR JAVASCRIPT AND .NET LIBRARIES License.
// See http://go.microsoft.com/fwlink/?LinkId=186234.

(function()
{
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
    var _undefined = undefined;
    var observable = root.Observable;
    var asyncSubject = root.AsyncSubject;
    var observableCreate = observable.Create;
 
    var cloneObject = function(obj)
    {
        var duplicate = {};
        for (var key in obj)
        {
            duplicate[key] = obj[key];
        }
        return duplicate;
    };

    var observableFromIEEvent = observable.FromIEEvent = function(htmlElement, eventName)
    {
        return observableCreate(function(observer)
        {
            var handler = function()
            {
                observer.OnNext(cloneObject(global.event));
            };
            htmlElement.attachEvent(eventName, handler);

            return function()
            {
                htmlElement.detachEvent(eventName, handler);
            };
        });
    };

    observable.FromHtmlEvent = function(htmlElement, eventName)
    {
        if (htmlElement.attachEvent !== _undefined)
        {
            return observableFromIEEvent(htmlElement, "on" + eventName);
        }
        else
        {
            return observableFromDOMEvent(htmlElement, eventName);
        }
    };

    var observableFromDOMEvent = observable.FromDOMEvent = function(htmlElement, eventName)
    {
        return observableCreate(function(observer)
        {
            var handler = function(evt)
            {
                observer.OnNext(cloneObject(evt));
            };
            htmlElement.addEventListener(eventName, handler, false);

            return function()
            {
                htmlElement.removeEventListener(eventName, handler, false);
            };
        });
    };

    observable.XmlHttpRequest = function(details)
    {
        if (typeof details == "string")
        {
            details = { Method: "GET", Url: details };
        }
        var result = new asyncSubject();
        try
        {
            var request = new XMLHttpRequest();
            if (details.Headers !== _undefined)
            {
                var h = details.Headers;
                for (var k in h)
                {
                    request.setRequestHeader(k, h[k]);
                }
            }
            request.open(details.Method, details.Url, true, details.User, details.Password);
            request.onreadystatechange = function()
            {
                if (request.readyState == 4)
                {
                    var status = request.status;
                    if ((status >= 200 && status < 300)
                    || status == 0 || status == "")
                    {
                        result.OnNext(request);
                        result.OnCompleted();
                    }
                    else
                    {
                        result.OnError(request);
                    }
                }
            }
            request.send(details.Body);
        }
        catch (e)
        {
            result.OnError(e);
        }
        var refCount = new root.RefCountDisposable(root.Disposable.Create(function()
        {
            if (request.readyState != 4)
            {
                request.abort();
                result.OnError(request);
            }
        }));

        return observable.CreateWithDisposable(function(subscriber)
        {
            return new root.CompositeDisposable(result.Subscribe(subscriber), refCount.GetDisposable());
        });
    }

})();