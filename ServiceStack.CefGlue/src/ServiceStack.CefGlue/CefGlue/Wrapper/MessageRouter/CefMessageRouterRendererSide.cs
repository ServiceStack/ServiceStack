namespace Xilium.CefGlue.Wrapper
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    using BrowserRequestInfoMap = CefBrowserInfoMap<System.Collections.Generic.KeyValuePair<int, int>, Xilium.CefGlue.Wrapper.CefMessageRouterRendererSide.RequestInfo>;

    /// <summary>
    /// Implements the renderer side of query routing. The methods of this class must
    /// be called on the render process main thread.
    /// </summary>
    public sealed class CefMessageRouterRendererSide
    {
        #region V8HandlerImpl

        private sealed class V8HandlerImpl : CefV8Handler
        {
            private readonly CefMessageRouterRendererSide _router;
            private readonly CefMessageRouterConfig _config;
            private int _contextId;

            public V8HandlerImpl(CefMessageRouterRendererSide router, CefMessageRouterConfig config)
            {
                _router = router;
                _config = config;
                _contextId = CefMessageRouter.ReservedId;
            }

            protected override bool Execute(string name, CefV8Value obj, CefV8Value[] arguments, out CefV8Value returnValue, out string exception)
            {
                if (name == _config.JSQueryFunction)
                {
                    if (arguments.Length != 1 || !arguments[0].IsObject)
                    {
                        returnValue = null;
                        exception = "Invalid arguments; expecting a single object";
                        return true;
                    }

                    var arg = arguments[0];

                    var requestVal = arg.GetValue(CefMessageRouter.MemberRequest);
                    if (requestVal == null || !requestVal.IsString)
                    {
                        returnValue = null;
                        exception = "Invalid arguments; object member '" +
                                    CefMessageRouter.MemberRequest + "' is required and must " +
                                    "have type string";
                        return true;
                    }

                    CefV8Value successVal = null;
                    if (arg.HasValue(CefMessageRouter.MemberOnSuccess))
                    {
                        successVal = arg.GetValue(CefMessageRouter.MemberOnSuccess);
                        if (!successVal.IsFunction)
                        {
                            returnValue = null;
                            exception = "Invalid arguments; object member '" +
                                        CefMessageRouter.MemberOnSuccess + "' must have type " +
                                        "function";
                            return true;
                        }
                    }

                    CefV8Value failureVal = null;
                    if (arg.HasValue(CefMessageRouter.MemberOnFailure))
                    {
                        failureVal = arg.GetValue(CefMessageRouter.MemberOnFailure);
                        if (!failureVal.IsFunction)
                        {
                            returnValue = null;
                            exception = "Invalid arguments; object member '" +
                                        CefMessageRouter.MemberOnFailure + "' must have type " +
                                        "function";
                            return true;
                        }
                    }

                    CefV8Value persistentVal = null;
                    if (arg.HasValue(CefMessageRouter.MemberPersistent))
                    {
                        persistentVal = arg.GetValue(CefMessageRouter.MemberPersistent);
                        if (!persistentVal.IsBool)
                        {
                            returnValue = null;
                            exception = "Invalid arguments; object member '" +
                                        CefMessageRouter.MemberPersistent + "' must have type " +
                                        "boolean";
                            return true;
                        }
                    }

                    var context = CefV8Context.GetCurrentContext();
                    var contextId = GetIDForContext(context);
                    var frameId = context.GetFrame().Identifier;
                    var persistent = (persistentVal != null && persistentVal.GetBoolValue());

                    var requestId = _router.SendQuery(context.GetBrowser(), frameId, contextId,
                        requestVal.GetStringValue(), persistent, successVal, failureVal);
                    returnValue = CefV8Value.CreateInt(requestId);
                    exception = null;
                    return true;
                }
                else if (name == _config.JSCancelFunction)
                {
                    if (arguments.Length != 1 || !arguments[0].IsInt)
                    {
                        returnValue = null;
                        exception = "Invalid arguments; expecting a single integer";
                        return true;
                    }

                    var result = false;
                    var requestId = arguments[0].GetIntValue();
                    if (requestId != CefMessageRouter.ReservedId)
                    {
                        CefV8Context context = CefV8Context.GetCurrentContext();
                        var contextId = GetIDForContext(context);
                        var frameId = context.GetFrame().Identifier;

                        result = _router.SendCancel(context.GetBrowser(), frameId, contextId, requestId);
                    }
                    returnValue = CefV8Value.CreateBool(result);
                    exception = null;
                    return true;
                }

                returnValue = null;
                exception = null;
                return false;
            }

            // Don't create the context ID until it's actually needed.
            private int GetIDForContext(CefV8Context context)
            {
                if (_contextId == CefMessageRouter.ReservedId)
                    _contextId = _router.CreateIDForContext(context);
                return _contextId;
            }
        };

        #endregion

        private readonly CefMessageRouterConfig _config;

        private readonly string _queryMessageName;
        private readonly string _cancelMessageName;

        private readonly CefMessageRouter.IdGeneratorInt32 _contextIdGenerator = new CefMessageRouter.IdGeneratorInt32();
        private readonly CefMessageRouter.IdGeneratorInt32 _requestIdGenerator = new CefMessageRouter.IdGeneratorInt32();

        // Map of (request ID, context ID) to RequestInfo for pending queries. An
        // entry is added when a request is initiated via the bound function and
        // removed when either the request completes, is canceled via the bound
        // function, or the associated context is released.
        private readonly BrowserRequestInfoMap _browserRequestInfoMap = new BrowserRequestInfoMap();

        // Map of context ID to CefV8Context for existing contexts. An entry is added
        // when a bound function is executed for the first time in the context and
        // removed when the context is released.
        private readonly Dictionary<int, CefV8Context> _contextMap = new Dictionary<int, CefV8Context>();

        /// <summary>
        /// Create a new router with the specified configuration.
        /// </summary>
        public CefMessageRouterRendererSide(CefMessageRouterConfig config)
        {
            if (!config.Validate()) throw new ArgumentException("Invalid configuration.");

            _config = config;
            _queryMessageName = config.JSQueryFunction + CefMessageRouter.MessageSuffix;
            _cancelMessageName = config.JSCancelFunction + CefMessageRouter.MessageSuffix;
        }

        /// <summary>
        /// Create a new router with the specified configuration.
        /// </summary>
        public static CefMessageRouterRendererSide Create(CefMessageRouterConfig config)
        {
            return new CefMessageRouterRendererSide(config);
        }

        /// <summary>
        /// Returns the number of queries currently pending for the specified |browser|
        /// and/or |context|. Either or both values may be empty.
        /// </summary>
        public int GetPendingCount(CefBrowser browser = null, CefV8Context context = null)
        {
            Helpers.RequireRendererThread();

            if (_browserRequestInfoMap.IsEmpty) return 0;

            if (context != null)
            {
                var contextId = GetIDForContext(context, false);
                if (contextId == CefMessageRouter.ReservedId)
                    return 0;  // Nothing associated with the specified context.

                int count = 0;
                BrowserRequestInfoMap.Visitor visitor = (int browserId, KeyValuePair<int, int> infoId, RequestInfo info, ref bool remove) =>
                {
                    if (infoId.Key == contextId) count++;
                    return true;
                };

                if (browser != null)
                {
                    // Count requests associated with the specified browser.
                    _browserRequestInfoMap.FindAll(browser.Identifier, visitor);
                }
                else
                {
                    // Count all requests for all browsers.
                    _browserRequestInfoMap.FindAll(visitor);
                }

                return count;
            }
            else if (browser != null)
            {
                return _browserRequestInfoMap.Count(browser.Identifier);
            }
            else
            {
                return _browserRequestInfoMap.Count();
            }

            return 0;
        }

        #region The below methods should be called from other CEF handlers. They must be called exactly as documented for the router to function correctly.

        /// <summary>
        /// Call from CefRenderProcessHandler::OnContextCreated. Registers the
        /// JavaScripts functions with the new context.
        /// </summary>
        public void OnContextCreated(CefBrowser browser, CefFrame frame, CefV8Context context)
        {
            Helpers.RequireRendererThread();

            // Do not keep any references to CefV8Value objects, otherwise
            // we should release all of them in OnContextReleased method (so we
            // can't rely on GC for this purpose).

            // Register function handlers with the 'window' object.
            using (var window = context.GetGlobal())
            {
                var handler = new V8HandlerImpl(this, _config);
                CefV8PropertyAttribute attributes = CefV8PropertyAttribute.ReadOnly | CefV8PropertyAttribute.DontEnum | CefV8PropertyAttribute.DontDelete;

                // Add the query function.
                using (var queryFunc = CefV8Value.CreateFunction(_config.JSQueryFunction, handler))
                {
                    window.SetValue(_config.JSQueryFunction, queryFunc, attributes);
                }

                // Add the cancel function.
                using (var cancelFunc = CefV8Value.CreateFunction(_config.JSCancelFunction, handler))
                {
                    window.SetValue(_config.JSCancelFunction, cancelFunc, attributes);
                }
            }
        }

        /// <summary>
        /// Call from CefRenderProcessHandler::OnContextReleased. Any pending queries
        /// associated with the released context will be canceled and
        /// Handler::OnQueryCanceled will be called in the browser process.
        /// </summary>
        public void OnContextReleased(CefBrowser browser, CefFrame frame, CefV8Context context)
        {
            Helpers.RequireRendererThread();

            // Get the context ID and remove the context from the map.
            var contextId = GetIDForContext(context, true);
            if (contextId != CefMessageRouter.ReservedId)
            {
                // Cancel all pending requests for the context.
                SendCancel(browser, frame.Identifier, contextId, CefMessageRouter.ReservedId);
            }
        }

        /// <summary>
        /// Call from CefRenderProcessHandler::OnProcessMessageReceived. Returns true
        /// if the message is handled by this router or false otherwise.
        /// </summary>
        public bool OnProcessMessageReceived(CefBrowser browser, CefProcessId sourceProcess, CefProcessMessage message)
        {
            Helpers.RequireRendererThread();

            var messageName = message.Name;
            if (messageName == _queryMessageName)
            {
                var args = message.Arguments;
                Debug.Assert(args.Count > 3);

                var contextId = args.GetInt(0);
                var requestId = args.GetInt(1);
                var isSuccess = args.GetBool(2);

                if (isSuccess)
                {
                    Debug.Assert(args.Count == 4);
                    string response = args.GetString(3);
                    Helpers.PostTaskUncertainty(CefThreadId.Renderer,
                        Helpers.Apply(this.ExecuteSuccessCallback, browser.Identifier, contextId, requestId, response)
                        );
                }
                else
                {
                    Debug.Assert(args.Count == 5);
                    var errorCode = args.GetInt(3);
                    string errorMessage = args.GetString(4);
                    Helpers.PostTaskUncertainty(CefThreadId.Renderer,
                        Helpers.Apply(this.ExecuteFailureCallback, browser.Identifier, contextId, requestId, errorCode, errorMessage)
                        );
                }

                return true;
            }

            return false;
        }

        #endregion

        // Structure representing a pending request.
        internal class RequestInfo
        {
            // True if the request is persistent.
            public bool Persistent;

            // Success callback function. May be NULL.
            public CefV8Value SuccessCallback;

            // Failure callback function. May be NULL.
            public CefV8Value FailureCallback;

            internal void Dispose()
            {
            }
        };

        // Retrieve a RequestInfo object from the map based on the renderer-side
        // IDs. If |always_remove| is true then the RequestInfo object will always be
        // removed from the map. Othewise, the RequestInfo object will only be removed
        // if the query is non-persistent. If |removed| is true the caller is
        // responsible for deleting the returned QueryInfo object.
        private RequestInfo GetRequestInfo(int browserId, int requestId, int contextId, bool alwaysRemove, ref bool removed)
        {
            var removedTemp = false;
            BrowserRequestInfoMap.Visitor visitor = (int vBrowserId, KeyValuePair<int, int> vInfoId, RequestInfo vInfo, ref bool vRemove) =>
            {
                vRemove = removedTemp = (alwaysRemove || !vInfo.Persistent);
                return true;
            };

            var info = _browserRequestInfoMap.Find(browserId, new KeyValuePair<int, int>(requestId, contextId), visitor);
            if (info != null) removed = removedTemp;
            return info;
        }

        // Returns the new request ID.
        private int SendQuery(CefBrowser browser, long frameId, int contextId, string request, bool persistent, CefV8Value successCallback, CefV8Value failureCallback)
        {
            Helpers.RequireRendererThread();

            var requestId = _requestIdGenerator.GetNextId();

            var info = new RequestInfo
            {
                Persistent = persistent,
                SuccessCallback = successCallback,
                FailureCallback = failureCallback,
            };
            _browserRequestInfoMap.Add(browser.Identifier, new KeyValuePair<int, int>(contextId, requestId), info);

            var message = CefProcessMessage.Create(_queryMessageName);
            var args = message.Arguments;
            args.SetInt(0, Helpers.Int64GetLow(frameId));
            args.SetInt(1, Helpers.Int64GetHigh(frameId));
            args.SetInt(2, contextId);
            args.SetInt(3, requestId);
            args.SetString(4, request);
            args.SetBool(5, persistent);

            browser.SendProcessMessage(CefProcessId.Browser, message);

            args.Dispose();
            message.Dispose();

            return requestId;
        }

        // If |requestId| is kReservedId all requests associated with |contextId|
        // will be canceled, otherwise only the specified |requestId| will be
        // canceled. Returns true if any request was canceled.
        private bool SendCancel(CefBrowser browser, long frameId, int contextId, int requestId)
        {
            Helpers.RequireRendererThread();

            var browserId = browser.Identifier;

            int cancelCount = 0;
            if (requestId != CefMessageRouter.ReservedId)
            {
                // Cancel a single request.
                bool removed = false;
                var info = GetRequestInfo(browserId, contextId, requestId, true, ref removed);
                if (info != null)
                {
                    Debug.Assert(removed);
                    info.Dispose();
                    cancelCount = 1;
                }
            }
            else
            {
                // Cancel all requests with the specified context ID.
                BrowserRequestInfoMap.Visitor visitor = (int vBrowserId, KeyValuePair<int, int> vInfoId, RequestInfo vInfo, ref bool vRemove) =>
                {
                    if (vInfoId.Key == contextId)
                    {
                        vRemove = true;
                        vInfo.Dispose();
                        cancelCount++;
                    }
                    return true;
                };

                _browserRequestInfoMap.FindAll(browserId, visitor);
            }

            if (cancelCount > 0)
            {
                var message = CefProcessMessage.Create(_cancelMessageName);

                var args = message.Arguments;
                args.SetInt(0, contextId);
                args.SetInt(1, requestId);

                browser.SendProcessMessage(CefProcessId.Browser, message);
                return true;
            }

            return false;
        }


        // Execute the onSuccess JavaScript callback.
        private void ExecuteSuccessCallback(int browserId, int contextId, int requestId, string response)
        {
            Helpers.RequireRendererThread();

            bool removed = false;
            var info = GetRequestInfo(browserId, contextId, requestId, false, ref removed);
            if (info == null) return;

            var context = GetContextByID(contextId);
            if (context != null && info.SuccessCallback != null)
            {
                var args = new[] { CefV8Value.CreateString(response) };
                info.SuccessCallback.ExecuteFunctionWithContext(context, null, args);
            }

            if (removed) info.Dispose();
        }

        // Execute the onFailure JavaScript callback.
        void ExecuteFailureCallback(int browserId, int contextId, int requestId,
                                    int errorCode, string errorMessage)
        {
            Helpers.RequireRendererThread();

            bool removed = false;
            var info = GetRequestInfo(browserId, contextId, requestId, true, ref removed);
            if (info == null) return;

            var context = GetContextByID(contextId);
            if (context != null && info.FailureCallback != null)
            {
                var args = new[] {
                    CefV8Value.CreateInt(errorCode),
                    CefV8Value.CreateString(errorMessage)
                    };
                info.FailureCallback.ExecuteFunctionWithContext(context, null, args);
            }

            Debug.Assert(removed);
            info.Dispose();
        }

        private int CreateIDForContext(CefV8Context context)
        {
            Helpers.RequireRendererThread();

            // The context should not already have an associated ID.
            Debug.Assert(GetIDForContext(context, false) == CefMessageRouter.ReservedId);

            var contextId = _contextIdGenerator.GetNextId();
            _contextMap.Add(contextId, context);
            return contextId;
        }

        // Retrieves the existing ID value associated with the specified |context|.
        // If |remove| is true the context will also be removed from the map.
        private int GetIDForContext(CefV8Context context, bool remove)
        {
            Helpers.RequireRendererThread();

            int contextId = CefMessageRouter.ReservedId;
            int? removeContextId = null;
            foreach (var kv in _contextMap)
            {
                if (kv.Value.IsSame(context))
                {
                    contextId = kv.Key;
                    if (remove)
                    {
                        removeContextId = contextId;

                        // "Release" stored context.
                        kv.Value.Dispose();
                    }
                    break;
                }
            }

            if (removeContextId.HasValue) _contextMap.Remove(removeContextId.Value);

            return contextId;
        }

        private CefV8Context GetContextByID(int contextId)
        {
            Helpers.RequireRendererThread();

            CefV8Context context;
            if (_contextMap.TryGetValue(contextId, out context)) return context;
            else return null;
        }

    }
}
