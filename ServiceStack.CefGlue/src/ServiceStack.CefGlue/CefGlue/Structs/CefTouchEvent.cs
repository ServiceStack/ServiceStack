using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xilium.CefGlue.Interop;

namespace Xilium.CefGlue
{
    /// <summary>
    /// Structure representing touch event information.
    /// </summary>
    public class CefTouchEvent
    {
        /// <summary>
        /// Id of a touch point. Must be unique per touch, can be any number except -1.
        /// Note that a maximum of 16 concurrent touches will be tracked; touches
        /// beyond that will be ignored.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// X coordinate relative to the left side of the view.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Y coordinate relative to the top side of the view.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// X radius in pixels. Set to 0 if not applicable.
        /// </summary>
        public float RadiusX { get; set; }

        /// <summary>
        /// Y radius in pixels. Set to 0 if not applicable.
        /// </summary>
        public float RadiusY { get; set; }

        /// <summary>
        /// Rotation angle in radians. Set to 0 if not applicable.
        /// </summary>
        public float RotationAngle { get; set; }

        /// <summary>
        /// The normalized pressure of the pointer input in the range of [0,1].
        /// Set to 0 if not applicable.
        /// </summary>
        public float Pressure { get; set; }

        /// <summary>
        /// The state of the touch point. Touches begin with one CEF_TET_PRESSED event
        /// followed by zero or more CEF_TET_MOVED events and finally one
        /// CEF_TET_RELEASED or CEF_TET_CANCELLED event. Events not respecting this
        /// order will be ignored.
        /// </summary>
        public CefTouchEventType Type { get; set; }

        /// <summary>
        /// Bit flags describing any pressed modifier keys. See
        /// cef_event_flags_t for values.
        /// </summary>
        public CefEventFlags Modifiers { get; set; }

        /// <summary>
        /// The device type that caused the event.
        /// </summary>
        public CefPointerType PointerType { get; set; }

        internal void ToNative(out cef_touch_event_t value)
        {
            value = new cef_touch_event_t();
            value.id = Id;
            value.x = X;
            value.y = Y;
            value.radius_x = RadiusX;
            value.radius_y = RadiusY;
            value.rotation_angle = RotationAngle;
            value.pressure = Pressure;
            value.type = Type;
            value.modifiers = Modifiers;
            value.pointer_type = PointerType;
        }
    }
}
