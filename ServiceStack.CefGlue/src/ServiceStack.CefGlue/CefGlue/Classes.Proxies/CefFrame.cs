namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class used to represent a frame in the browser window. When used in the
    /// browser process the methods of this class may be called on any thread unless
    /// otherwise indicated in the comments. When used in the render process the
    /// methods of this class may only be called on the main thread.
    /// </summary>
    public sealed unsafe partial class CefFrame
    {
        /// <summary>
        /// True if this object is currently attached to a valid frame.
        /// </summary>
        public bool IsValid
        {
            get { return cef_frame_t.is_valid(_self) != 0; }
        }

        /// <summary>
        /// Execute undo in this frame.
        /// </summary>
        public void Undo()
        {
            cef_frame_t.undo(_self);
        }

        /// <summary>
        /// Execute redo in this frame.
        /// </summary>
        public void Redo()
        {
            cef_frame_t.redo(_self);
        }

        /// <summary>
        /// Execute cut in this frame.
        /// </summary>
        public void Cut()
        {
            cef_frame_t.cut(_self);
        }

        /// <summary>
        /// Execute copy in this frame.
        /// </summary>
        public void Copy()
        {
            cef_frame_t.copy(_self);
        }

        /// <summary>
        /// Execute paste in this frame.
        /// </summary>
        public void Paste()
        {
            cef_frame_t.paste(_self);
        }

        /// <summary>
        /// Execute delete in this frame.
        /// </summary>
        public void Delete()
        {
            cef_frame_t.del(_self);
        }

        /// <summary>
        /// Execute select all in this frame.
        /// </summary>
        public void SelectAll()
        {
            cef_frame_t.select_all(_self);
        }

        /// <summary>
        /// Save this frame's HTML source to a temporary file and open it in the
        /// default text viewing application. This method can only be called from the
        /// browser process.
        /// </summary>
        public void ViewSource()
        {
            cef_frame_t.view_source(_self);
        }

        /// <summary>
        /// Retrieve this frame's HTML source as a string sent to the specified
        /// visitor.
        /// </summary>
        public void GetSource(CefStringVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            cef_frame_t.get_source(_self, visitor.ToNative());
        }

        /// <summary>
        /// Retrieve this frame's display text as a string sent to the specified
        /// visitor.
        /// </summary>
        public void GetText(CefStringVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            cef_frame_t.get_text(_self, visitor.ToNative());
        }

        /// <summary>
        /// Load the request represented by the |request| object.
        /// WARNING: This method will fail with "bad IPC message" reason
        /// INVALID_INITIATOR_ORIGIN (213) unless you first navigate to the
        /// request origin using some other mechanism (LoadURL, link click, etc).
        /// </summary>
        public void LoadRequest(CefRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");

            cef_frame_t.load_request(_self, request.ToNative());
        }

        /// <summary>
        /// Load the specified |url|.
        /// </summary>
        public void LoadUrl(string url)
        {
            fixed (char* url_str = url)
            {
                var n_url = new cef_string_t(url_str, url != null ? url.Length : 0);
                cef_frame_t.load_url(_self, &n_url);
            }
        }

        /// <summary>
        /// Execute a string of JavaScript code in this frame. The |script_url|
        /// parameter is the URL where the script in question can be found, if any.
        /// The renderer may request this URL to show the developer the source of the
        /// error.  The |start_line| parameter is the base line number to use for error
        /// reporting.
        /// </summary>
        public void ExecuteJavaScript(string code, string url, int line)
        {
            fixed (char* code_str = code)
            fixed (char* url_str = url)
            {
                var n_code = new cef_string_t(code_str, code != null ? code.Length : 0);
                var n_url = new cef_string_t(url_str, url != null ? url.Length : 0);
                cef_frame_t.execute_java_script(_self, &n_code, &n_url, line);
            }
        }

        /// <summary>
        /// Returns true if this is the main (top-level) frame.
        /// </summary>
        public bool IsMain
        {
            get { return cef_frame_t.is_main(_self) != 0; }
        }

        /// <summary>
        /// Returns true if this is the focused frame.
        /// </summary>
        public bool IsFocused
        {
            get { return cef_frame_t.is_focused(_self) != 0; }
        }

        /// <summary>
        /// Returns the name for this frame. If the frame has an assigned name (for
        /// example, set via the iframe "name" attribute) then that value will be
        /// returned. Otherwise a unique name will be constructed based on the frame
        /// parent hierarchy. The main (top-level) frame will always have an empty name
        /// value.
        /// </summary>
        public string Name
        {
            get
            {
                var n_result = cef_frame_t.get_name(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the globally unique identifier for this frame or &lt; 0 if the
        /// underlying frame does not yet exist.
        /// </summary>
        public long Identifier
        {
            get { return cef_frame_t.get_identifier(_self); }
        }

        /// <summary>
        /// Returns the parent of this frame or NULL if this is the main (top-level)
        /// frame.
        /// </summary>
        public CefFrame Parent
        {
            get
            {
                return CefFrame.FromNativeOrNull(
                    cef_frame_t.get_parent(_self)
                    );
            }
        }

        /// <summary>
        /// Returns the URL currently loaded in this frame.
        /// </summary>
        public string Url
        {
            get
            {
                var n_result = cef_frame_t.get_url(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the browser that this frame belongs to.
        /// </summary>
        public CefBrowser Browser
        {
            get
            {
                return CefBrowser.FromNative(
                    cef_frame_t.get_browser(_self)
                    );
            }
        }

        /// <summary>
        /// Get the V8 context associated with the frame. This method can only be
        /// called from the render process.
        /// </summary>
        public CefV8Context V8Context
        {
            get
            {
                return CefV8Context.FromNative(
                    cef_frame_t.get_v8context(_self)
                    );
            }
        }

        /// <summary>
        /// Visit the DOM document. This method can only be called from the render
        /// process.
        /// </summary>
        public void VisitDom(CefDomVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            cef_frame_t.visit_dom(_self, visitor.ToNative());
        }

        /// <summary>
        /// Create a new URL request that will be treated as originating from this
        /// frame and the associated browser. This request may be intercepted by the
        /// client via CefResourceRequestHandler or CefSchemeHandlerFactory. Use
        /// CefURLRequest::Create instead if you do not want the request to have this
        /// association, in which case it may be handled differently (see documentation
        /// on that method). Requests may originate from both the browser process and
        /// the render process.
        /// For requests originating from the browser process:
        /// - POST data may only contain a single element of type PDE_TYPE_FILE or
        /// PDE_TYPE_BYTES.
        /// For requests originating from the render process:
        /// - POST data may only contain a single element of type PDE_TYPE_BYTES.
        /// - If the response contains Content-Disposition or Mime-Type header values
        /// that would not normally be rendered then the response may receive
        /// special handling inside the browser (for example, via the file download
        /// code path instead of the URL request code path).
        /// The |request| object will be marked as read-only after calling this method.
        /// </summary>
        public CefUrlRequest CreateUrlRequest(CefRequest request, CefUrlRequestClient client = null)
        {
            var n_request = request.ToNative();
            var n_client = client != null ? client.ToNative() : null;

            var n_result = cef_frame_t.create_urlrequest(_self, n_request, n_client);

            return CefUrlRequest.FromNativeOrNull(n_result);
        }

        /// <summary>
        /// Send a message to the specified |target_process|. Message delivery is not
        /// guaranteed in all cases (for example, if the browser is closing,
        /// navigating, or if the target process crashes). Send an ACK message back
        /// from the target process if confirmation is required.
        /// </summary>
        public void SendProcessMessage(CefProcessId targetProcess, CefProcessMessage message)
        {
            if (message == null) throw new ArgumentNullException("message");

            cef_frame_t.send_process_message(_self, targetProcess, message.ToNative());
        }
    }
}
