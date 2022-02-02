namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Represents a sink to which media can be routed. Instances of this object are
    /// retrieved via CefMediaObserver::OnSinks. The methods of this class may
    /// be called on any browser process thread unless otherwise indicated.
    /// </summary>
    public sealed unsafe partial class CefMediaSink
    {
        /// <summary>
        /// Returns the ID for this sink.
        /// </summary>
        public string Id
        {
            get
            {
                var n_result = cef_media_sink_t.get_id(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the name of this sink.
        /// </summary>
        public string Name
        {
            get
            {
                var n_result = cef_media_sink_t.get_name(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the description of this sink.
        /// </summary>
        public string Description
        {
            get
            {
                var n_result = cef_media_sink_t.get_description(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the icon type for this sink.
        /// </summary>
        public CefMediaSinkIconType IconType =>
            cef_media_sink_t.get_icon_type(_self);

        /// <summary>
        /// Asynchronously retrieves device info.
        /// </summary>
        public void GetDeviceInfo(CefMediaSinkDeviceInfoCallback callback)
        {
            cef_media_sink_t.get_device_info(_self, callback.ToNative());
        }

        /// <summary>
        /// Returns true if this sink accepts content via Cast.
        /// </summary>
        public bool IsCastSink => cef_media_sink_t.is_cast_sink(_self) != 0;

        /// <summary>
        /// Returns true if this sink accepts content via DIAL.
        /// </summary>
        public bool IsDialSink => cef_media_sink_t.is_dial_sink(_self) != 0;

        /// <summary>
        /// Returns true if this sink is compatible with |source|.
        /// </summary>
        public bool IsCompatibleWith(CefMediaSource source)
        {
            return cef_media_sink_t.is_compatible_with(_self, source.ToNative()) != 0;
        }
    }
}
