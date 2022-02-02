namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class used to make a URL request. URL requests are not associated with a
    /// browser instance so no CefClient callbacks will be executed. URL requests
    /// can be created on any valid CEF thread in either the browser or render
    /// process. Once created the methods of the URL request object must be accessed
    /// on the same thread that created it.
    /// </summary>
    public sealed unsafe partial class CefUrlRequest
    {
        /// <summary>
        /// Create a new URL request that is not associated with a specific browser or
        /// frame. Use CefFrame::CreateURLRequest instead if you want the request to
        /// have this association, in which case it may be handled differently (see
        /// documentation on that method).  A request created with this method may only
        /// originate from the browser process, and will behave as follows:
        /// - It may be intercepted by the client via CefResourceRequestHandler or
        /// CefSchemeHandlerFactory.
        /// - POST data may only contain only a single element of type PDE_TYPE_FILE
        /// or PDE_TYPE_BYTES.
        /// - If |request_context| is empty the global request context will be used.
        /// The |request| object will be marked as read-only after calling this method.
        /// </summary>
        public static CefUrlRequest Create(CefRequest request, CefUrlRequestClient client, CefRequestContext requestContext)
        {
            if (request == null) throw new ArgumentNullException("request");

            var n_request = request.ToNative();
            var n_client = client != null ? client.ToNative() : null;
            var n_requestContext = requestContext != null ? requestContext.ToNative() : null;

            return CefUrlRequest.FromNative(
                cef_urlrequest_t.create(n_request, n_client, n_requestContext)
                );
        }

        /// <summary>
        /// Returns the request object used to create this URL request. The returned
        /// object is read-only and should not be modified.
        /// </summary>
        public CefRequest GetRequest()
        {
            return CefRequest.FromNative(
                cef_urlrequest_t.get_request(_self)
                );
        }

        /// <summary>
        /// Returns the client.
        /// </summary>
        public CefUrlRequestClient GetClient()
        {
            return CefUrlRequestClient.FromNative(
                cef_urlrequest_t.get_client(_self)
                );
        }

        /// <summary>
        /// Returns the request status.
        /// </summary>
        public CefUrlRequestStatus RequestStatus
        {
            get { return cef_urlrequest_t.get_request_status(_self); }
        }

        /// <summary>
        /// Returns the request error if status is UR_CANCELED or UR_FAILED, or 0
        /// otherwise.
        /// </summary>
        public CefErrorCode RequestError
        {
            get { return cef_urlrequest_t.get_request_error(_self); }
        }

        /// <summary>
        /// Returns the response, or NULL if no response information is available.
        /// Response information will only be available after the upload has completed.
        /// The returned object is read-only and should not be modified.
        /// </summary>
        public CefResponse GetResponse()
        {
            return CefResponse.FromNativeOrNull(
                cef_urlrequest_t.get_response(_self)
                );
        }

        /// <summary>
        /// Returns true if the response body was served from the cache. This includes
        /// responses for which revalidation was required.
        /// </summary>
        public bool ResponseWasCached
        {
            get
            {
                return cef_urlrequest_t.response_was_cached(_self) != 0;
            }
        }

        /// <summary>
        /// Cancel the request.
        /// </summary>
        public void Cancel()
        {
            cef_urlrequest_t.cancel(_self);
        }
    }
}
