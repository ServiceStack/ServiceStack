namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to handle audio events
    /// All methods will be called on the UI thread
    /// </summary>
    public abstract unsafe partial class CefAudioHandler
    {
        private int get_audio_parameters(cef_audio_handler_t* self, cef_browser_t* browser, cef_audio_parameters_t* @params)
        {
            CheckSelf(self);

            var mBrowser = CefBrowser.FromNative(browser);

            var mResult = GetAudioParameters(mBrowser, new CefAudioParameters(@params));
            return mResult ? 1 : 0;
        }

        /// <summary>
        /// Called on the UI thread to allow configuration of audio stream parameters.
        /// Return true to proceed with audio stream capture, or false to cancel it.
        /// All members of |params| can optionally be configured here, but they are
        /// also pre-filled with some sensible defaults.
        /// </summary>
        protected abstract bool GetAudioParameters(CefBrowser browser, CefAudioParameters parameters);


        private void on_audio_stream_started(cef_audio_handler_t* self, cef_browser_t* browser, cef_audio_parameters_t* @params, int channels)
        {
            CheckSelf(self);

            var mBrowser = CefBrowser.FromNative(browser);
            OnAudioStreamStarted(mBrowser, new CefAudioParameters(@params), channels);
        }

        /// <summary>
        /// Called on a browser audio capture thread when the browser starts
        /// streaming audio. OnAudioSteamStopped will always be called after
        /// OnAudioStreamStarted; both methods may be called multiple times
        /// for the same browser. |params| contains the audio parameters like
        /// sample rate and channel layout. |channels| is the number of channels.
        /// </summary>
        protected abstract void OnAudioStreamStarted(CefBrowser browser, in CefAudioParameters parameters, int channels);


        private void on_audio_stream_packet(cef_audio_handler_t* self, cef_browser_t* browser, float** data, int frames, long pts)
        {
            CheckSelf(self);

            var mBrowser = CefBrowser.FromNative(browser);
            OnAudioStreamPacket(mBrowser, (IntPtr)data, frames, pts);
        }

        /// <summary>
        /// Called on the audio stream thread when a PCM packet is received for the
        /// stream. |data| is an array representing the raw PCM data as a floating
        /// point type, i.e. 4-byte value(s). |frames| is the number of frames in the
        /// PCM packet. |pts| is the presentation timestamp (in milliseconds since the
        /// Unix Epoch) and represents the time at which the decompressed packet should
        /// be presented to the user. Based on |frames| and the |channel_layout| value
        /// passed to OnAudioStreamStarted you can calculate the size of the |data|
        /// array in bytes.
        /// 
        /// |data| is |float**|, readonly!
        /// </summary>
        protected abstract void OnAudioStreamPacket(CefBrowser browser, IntPtr data, int frames, long pts);


        private void on_audio_stream_stopped(cef_audio_handler_t* self, cef_browser_t* browser)
        {
            CheckSelf(self);

            var mBrowser = CefBrowser.FromNative(browser);
            OnAudioStreamStopped(mBrowser);
        }

        /// <summary>
        /// Called on the UI thread when the stream has stopped. OnAudioSteamStopped
        /// will always be called after OnAudioStreamStarted; both methods may be
        /// called multiple times for the same stream.
        /// </summary>
        protected abstract void OnAudioStreamStopped(CefBrowser browser);


        private void on_audio_stream_error(cef_audio_handler_t* self, cef_browser_t* browser, cef_string_t* message)
        {
            CheckSelf(self);

            var mBrowser = CefBrowser.FromNative(browser);
            var mMessage = cef_string_t.ToString(message);
            OnAudioStreamError(mBrowser, mMessage);
        }

        /// <summary>
        /// Called on the UI or audio stream thread when an error occurred. During the
        /// stream creation phase this callback will be called on the UI thread while
        /// in the capturing phase it will be called on the audio stream thread. The
        /// stream will be stopped immediately.
        /// </summary>
        protected abstract void OnAudioStreamError(CefBrowser browser, string message);
    }
}
