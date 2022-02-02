namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implemented by the client to observe MediaRouter events and registered via
    /// CefMediaRouter::AddObserver. The methods of this class will be called on the
    /// browser process UI thread.
    /// </summary>
    public abstract unsafe partial class CefMediaObserver
    {
        private void on_sinks(cef_media_observer_t* self, UIntPtr sinksCount, cef_media_sink_t** sinks)
        {
            CheckSelf(self);

            var mSinksCount = checked((int)sinksCount);
            var mSinks = new CefMediaSink[mSinksCount];
            for (var i = 0; i < mSinksCount; i++)
            {
                mSinks[i] = CefMediaSink.FromNative(sinks[i]);
            }

            OnSinks(mSinks);
        }

        /// <summary>
        /// The list of available media sinks has changed or
        /// CefMediaRouter::NotifyCurrentSinks was called.
        /// </summary>
        protected abstract void OnSinks(CefMediaSink[] sinks);


        private void on_routes(cef_media_observer_t* self, UIntPtr routesCount, cef_media_route_t** routes)
        {
            CheckSelf(self);

            var mRoutesCount = checked((int)routesCount);
            var mRoutes = new CefMediaRoute[mRoutesCount];
            for (var i = 0; i < mRoutesCount; i++)
            {
                mRoutes[i] = CefMediaRoute.FromNative(routes[i]);
            }

            OnRoutes(mRoutes);
        }

        /// <summary>
        /// The list of available media routes has changed or
        /// CefMediaRouter::NotifyCurrentRoutes was called.
        /// </summary>
        protected abstract void OnRoutes(CefMediaRoute[] routes);


        private void on_route_state_changed(cef_media_observer_t* self, cef_media_route_t* route, CefMediaRouteConnectionState state)
        {
            CheckSelf(self);

            var mRoute = CefMediaRoute.FromNative(route);
            OnRouteStateChanged(mRoute, state);
        }

        /// <summary>
        /// The connection state of |route| has changed.
        /// </summary>
        protected abstract void OnRouteStateChanged(CefMediaRoute route, CefMediaRouteConnectionState state);


        private void on_route_message_received(cef_media_observer_t* self, cef_media_route_t* route, void* message, UIntPtr message_size)
        {
            CheckSelf(self);

            var mRoute = CefMediaRoute.FromNative(route);
            var mMessageSize = checked((int)message_size);

            OnRouteMessageReceived(mRoute, (IntPtr)message, mMessageSize);
        }

        /// <summary>
        /// A message was recieved over |route|. |message| is only valid for
        /// the scope of this callback and should be copied if necessary.
        /// </summary>
        protected abstract void OnRouteMessageReceived(CefMediaRoute route, IntPtr message, int messageSize);
    }
}
