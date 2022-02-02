using System;
using Xilium.CefGlue.Interop;

namespace Xilium.CefGlue
{
    /// <summary>
    /// Structure representing the audio parameters for setting up the audio handler.
    /// </summary>
    public unsafe ref struct CefAudioParameters
    {
        private readonly cef_audio_parameters_t* _target;

        internal CefAudioParameters(cef_audio_parameters_t* target)
        {
            _target = target;
        }

        /// <summary>
        /// Layout of the audio channels.
        /// </summary>
        public CefChannelLayout ChannelLayout
        {
            readonly get { CheckSelf(); return _target->channel_layout; }
            set { CheckSelf(); _target->channel_layout = value; }
        }

        /// <summary>
        /// Sample rate.
        /// </summary>
        public int SampleRate
        {
            readonly get { CheckSelf(); return _target->sample_rate; }
            set { CheckSelf(); _target->sample_rate = value; }
        }

        /// <summary>
        /// Number of frames per buffer.
        /// </summary>
        public int FramesPerBuffer
        {
            readonly get { CheckSelf(); return _target->frames_per_buffer; }
            set { CheckSelf(); _target->frames_per_buffer = value; }
        }

        private readonly void CheckSelf()
        {
            if (_target == null) ThrowCheckSelfFailed();
        }

        private static void ThrowCheckSelfFailed()
        {
            throw new InvalidOperationException("CefAudioParameters is null.");
        }
    }
}
