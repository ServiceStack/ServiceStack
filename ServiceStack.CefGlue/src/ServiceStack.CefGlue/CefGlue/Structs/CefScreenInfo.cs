namespace Xilium.CefGlue
{
    using System;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Screen information used when window rendering is disabled. This structure is
    /// passed as a parameter to CefRenderHandler::GetScreenInfo and should be filled
    /// in by the client.
    /// </summary>
    public sealed unsafe class CefScreenInfo
    {
        private cef_screen_info_t* _self;

        internal CefScreenInfo(cef_screen_info_t* self)
        {
            _self = self;
        }

        internal void Dispose()
        {
            _self = null;
        }

        /// <summary>
        /// Device scale factor. Specifies the ratio between physical and logical
        /// pixels.
        /// </summary>
        public float DeviceScaleFactor
        {
            get { return _self->device_scale_factor; }
            set { _self->device_scale_factor = value; }
        }

        /// <summary>
        /// The screen depth in bits per pixel.
        /// </summary>
        public int Depth
        {
            get { return _self->depth; }
            set { _self->depth = value; }
        }

        /// <summary>
        /// The bits per color component. This assumes that the colors are balanced
        /// equally.
        /// </summary>
        public int DepthPerComponent
        {
            get { return _self->depth_per_component; }
            set { _self->depth_per_component = value; }
        }

        /// <summary>
        /// This can be true for black and white printers.
        /// </summary>
        public bool IsMonochrome
        {
            get { return _self->is_monochrome != 0; }
            set { _self->is_monochrome = value ? 1 : 0; }
        }

        /// <summary>
        /// This is set from the rcMonitor member of MONITORINFOEX, to whit:
        ///   "A RECT structure that specifies the display monitor rectangle,
        ///   expressed in virtual-screen coordinates. Note that if the monitor
        ///   is not the primary display monitor, some of the rectangle's
        ///   coordinates may be negative values."
        ///
        /// The |rect| and |available_rect| properties are used to determine the
        /// available surface for rendering popup views.
        /// </summary>
        public CefRectangle Rectangle
        {
            get
            {
                var n_rect = _self->rect;
                return new CefRectangle(n_rect.x, n_rect.y, n_rect.width, n_rect.height);
            }
            set
            {
                _self->rect = new cef_rect_t(value.X, value.Y, value.Width, value.Height);
            }
        }

        /// <summary>
        /// This is set from the rcWork member of MONITORINFOEX, to whit:
        ///   "A RECT structure that specifies the work area rectangle of the
        ///   display monitor that can be used by applications, expressed in
        ///   virtual-screen coordinates. Windows uses this rectangle to
        ///   maximize an application on the monitor. The rest of the area in
        ///   rcMonitor contains system windows such as the task bar and side
        ///   bars. Note that if the monitor is not the primary display monitor,
        ///   some of the rectangle's coordinates may be negative values".
        ///
        /// The |rect| and |available_rect| properties are used to determine the
        /// available surface for rendering popup views.
        /// </summary>
        public CefRectangle AvailableRectangle
        {
            get
            {
                var n_rect = _self->available_rect;
                return new CefRectangle(n_rect.x, n_rect.y, n_rect.width, n_rect.height);
            }
            set
            {
                _self->available_rect = new cef_rect_t(value.X, value.Y, value.Width, value.Height);
            }
        }
    }
}
