//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_channel_layout_t.
//
namespace Xilium.CefGlue
{
    /// <summary>
    /// Enumerates the various representations of the ordering of audio channels.
    /// </summary>
    public enum CefChannelLayout
    {
        None = 0,

        Unsupported = 1,

        /// <summary>
        /// Front C
        /// </summary>
        Mono = 2,

        /// <summary>
        /// Front L, Front R
        /// </summary>
        Stereo = 3,

        /// <summary>
        /// Front L, Front R, Back C
        /// </summary>
        Layout_2_1 = 4,

        /// <summary>
        /// Front L, Front R, Front C
        /// </summary>
        Surround = 5,

        /// <summary>
        /// Front L, Front R, Front C, Back C
        /// </summary>
        Layout_4_0 = 6,

        /// <summary>
        /// Front L, Front R, Side L, Side R
        /// </summary>
        Layout_2_2 = 7,

        /// <summary>
        /// Front L, Front R, Back L, Back R
        /// </summary>
        Quad = 8,

        /// <summary>
        /// Front L, Front R, Front C, Side L, Side R
        /// </summary>
        Layout_5_0 = 9,

        /// <summary>
        /// Front L, Front R, Front C, LFE, Side L, Side R
        /// </summary>
        Layout_5_1 = 10,

        /// <summary>
        /// Front L, Front R, Front C, Back L, Back R
        /// </summary>
        Layout_5_0_Back = 11,

        /// <summary>
        /// Front L, Front R, Front C, LFE, Back L, Back R
        /// </summary>
        Layout_5_1_Back = 12,

        /// <summary>
        /// Front L, Front R, Front C, Side L, Side R, Back L, Back R
        /// </summary>
        Layout_7_0 = 13,

        /// <summary>
        /// Front L, Front R, Front C, LFE, Side L, Side R, Back L, Back R
        /// </summary>
        Layout_7_1 = 14,

        /// <summary>
        /// Front L, Front R, Front C, LFE, Side L, Side R, Front LofC, Front RofC
        /// </summary>
        Layout_7_1_Wide = 15,

        /// <summary>
        /// Stereo L, Stereo R
        /// </summary>
        Layout_Stereo_Downmix = 16,

        /// <summary>
        /// Stereo L, Stereo R, LFE
        /// </summary>
        Layout_2Point1 = 17,

        /// <summary>
        /// Stereo L, Stereo R, Front C, LFE
        /// </summary>
        Layout_3_1 = 18,

        /// <summary>
        /// Stereo L, Stereo R, Front C, Rear C, LFE
        /// </summary>
        Layout_4_1 = 19,

        /// <summary>
        /// Stereo L, Stereo R, Front C, Side L, Side R, Back C
        /// </summary>
        Layout_6_0 = 20,

        /// <summary>
        /// Stereo L, Stereo R, Side L, Side R, Front LofC, Front RofC
        /// </summary>
        Layout_6_0_Front = 21,

        /// <summary>
        /// Stereo L, Stereo R, Front C, Rear L, Rear R, Rear C
        /// </summary>
        Layout_Hexagonal = 22,

        /// <summary>
        /// Stereo L, Stereo R, Front C, LFE, Side L, Side R, Rear Center
        /// </summary>
        Layout_6_1 = 23,

        /// <summary>
        /// Stereo L, Stereo R, Front C, LFE, Back L, Back R, Rear Center
        /// </summary>
        Layout_6_1_Back = 24,

        /// <summary>
        /// Stereo L, Stereo R, Side L, Side R, Front LofC, Front RofC, LFE
        /// </summary>
        Layout_6_1_Front = 25,

        /// <summary>
        /// Front L, Front R, Front C, Side L, Side R, Front LofC, Front RofC
        /// </summary>
        Layout_7_0_Front = 26,

        /// <summary>
        /// Front L, Front R, Front C, LFE, Back L, Back R, Front LofC, Front RofC
        /// </summary>
        Layout_7_1_Wide_Back = 27,

        /// <summary>
        /// Front L, Front R, Front C, Side L, Side R, Rear L, Back R, Back C.
        /// </summary>
        Layout_Octagonal = 28,

        /// <summary>
        /// Channels are not explicitly mapped to speakers.
        /// </summary>
        Discrete = 29,

        /// <summary>
        /// Front L, Front R, Front C. Front C contains the keyboard mic audio. This
        /// layout is only intended for input for WebRTC. The Front C channel
        /// is stripped away in the WebRTC audio input pipeline and never seen outside
        /// of that.
        /// </summary>
        StereoAndKeyboardMic = 30,

        /// <summary>
        /// Front L, Front R, Side L, Side R, LFE
        /// </summary>
        Layout_4_1_Quad_Side = 31,

        /// <summary>
        /// Actual channel layout is specified in the bitstream and the actual channel
        /// count is unknown at Chromium media pipeline level (useful for audio
        /// pass-through mode).
        /// </summary>
        Bitstream = 32,

        // Max value, must always equal the largest entry ever logged.
        //CEF_CHANNEL_LAYOUT_MAX = CEF_CHANNEL_LAYOUT_BITSTREAM
    }
}
