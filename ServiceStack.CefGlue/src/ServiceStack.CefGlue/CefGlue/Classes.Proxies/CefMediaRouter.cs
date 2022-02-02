namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Supports discovery of and communication with media devices on the local
    /// network via the Cast and DIAL protocols. The methods of this class may be
    /// called on any browser process thread unless otherwise indicated.
    /// </summary>
    public sealed unsafe partial class CefMediaRouter
    {
        /// <summary>
        /// Returns the MediaRouter object associated with the global request context.
        /// Equivalent to calling
        /// CefRequestContext::GetGlobalContext()->GetMediaRouter().
        /// </summary>
        public static CefMediaRouter GetGlobalMediaRouter()
        {
            return CefMediaRouter.FromNative(
                cef_media_router_t.get_global()
                );
        }

        /// <summary>
        /// Add an observer for MediaRouter events. The observer will remain registered
        /// until the returned Registration object is destroyed.
        /// </summary>
        public CefRegistration AddObserver(CefMediaObserver observer)
        {
            var n_result = cef_media_router_t.add_observer(_self, observer.ToNative());
            return CefRegistration.FromNative(n_result);
        }

        /// <summary>
        /// Returns a MediaSource object for the specified media source URN. Supported
        /// URN schemes include "cast:" and "dial:", and will be already known by the
        /// client application (e.g. "cast:&lt;appId&gt;?clientId=&lt;clientId&gt;").
        /// </summary>
        public CefMediaSource GetSource(string urn)
        {
            fixed (char* urn_str = urn)
            {
                var n_urn = new cef_string_t(urn_str, urn.Length);
                var n_result = cef_media_router_t.get_source(_self, &n_urn);
                return CefMediaSource.FromNativeOrNull(n_result);
            }
        }

        /// <summary>
        /// Trigger an asynchronous call to CefMediaObserver::OnSinks on all
        /// registered observers.
        /// </summary>
        public void NotifyCurrentSinks()
        {
            cef_media_router_t.notify_current_sinks(_self);
        }

        /// <summary>
        /// Create a new route between |source| and |sink|. Source and sink must be
        /// valid, compatible (as reported by CefMediaSink::IsCompatibleWith), and a
        /// route between them must not already exist. |callback| will be executed
        /// on success or failure. If route creation succeeds it will also trigger an
        /// asynchronous call to CefMediaObserver::OnRoutes on all registered
        /// observers.
        /// </summary>
        public void CreateRoute(CefMediaSource source, CefMediaSink sink, CefMediaRouteCreateCallback callback)
        {
            cef_media_router_t.create_route(_self,
                source.ToNative(),
                sink.ToNative(),
                callback.ToNative());
        }

        /// <summary>
        /// Trigger an asynchronous call to CefMediaObserver::OnRoutes on all
        /// registered observers.
        /// </summary>
        public void NotifyCurrentRoutes()
        {
            cef_media_router_t.notify_current_routes(_self);
        }
    }
}
