namespace Xilium.CefGlue
{
    using System;

    /// <summary>
    /// Key event types.
    /// </summary>
    public enum CefKeyEventType : int
    {
        /// <summary>
        /// Notification that a key transitioned from "up" to "down".
        /// </summary>
        RawKeyDown = 0,

        /// <summary>
        /// Notification that a key was pressed. This does not necessarily correspond
        /// to a character depending on the key and language. Use KEYEVENT_CHAR for
        /// character input.
        /// </summary>
        KeyDown,

        /// <summary>
        /// Notification that a key was released.
        /// </summary>
        KeyUp,

        /// <summary>
        /// Notification that a character was typed. Use this for text input. Key
        /// down events may generate 0, 1, or more than one character event depending
        /// on the key, locale, and operating system.
        /// </summary>
        Char,
    }
}
