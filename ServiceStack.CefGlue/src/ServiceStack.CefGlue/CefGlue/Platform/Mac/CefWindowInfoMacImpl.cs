namespace Xilium.CefGlue.Platform
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Xilium.CefGlue;
    using Xilium.CefGlue.Interop;
    using Xilium.CefGlue.Platform.Windows;

    internal unsafe sealed class CefWindowInfoMacImpl : CefWindowInfo
    {
        private cef_window_info_t_mac* _self;

        public CefWindowInfoMacImpl()
            : base(true)
        {
            _self = cef_window_info_t_mac.Alloc();
        }

        public CefWindowInfoMacImpl(cef_window_info_t* ptr)
            : base(false)
        {
            if (CefRuntime.Platform != CefRuntimePlatform.MacOS)
                throw new InvalidOperationException();

            _self = (cef_window_info_t_mac*)ptr;
        }

        internal override cef_window_info_t* GetNativePointer()
        {
            return (cef_window_info_t*)_self;
        }

        protected internal override void DisposeNativePointer()
        {
            cef_window_info_t_mac.Free(_self);
            _self = null;
        }

        public override IntPtr ParentHandle
        {
            get { ThrowIfDisposed(); return _self->parent_view; }
            set { ThrowIfDisposed(); _self->parent_view = value; }
        }

        public override IntPtr Handle
        {
            get { ThrowIfDisposed(); return _self->view; }
            set { ThrowIfDisposed(); _self->view = value; }
        }

        public override string Name
        {
            get { ThrowIfDisposed(); return cef_string_t.ToString(&_self->window_name); }
            set { ThrowIfDisposed(); cef_string_t.Copy(value, &_self->window_name); }
        }

        public override int X
        {
            get { ThrowIfDisposed(); return _self->x; }
            set { ThrowIfDisposed(); _self->x = value; }
        }

        public override int Y
        {
            get { ThrowIfDisposed(); return _self->y; }
            set { ThrowIfDisposed(); _self->y = value; }
        }

        public override int Width
        {
            get { ThrowIfDisposed(); return _self->width; }
            set { ThrowIfDisposed(); _self->width = value; }
        }

        public override int Height
        {
            get { ThrowIfDisposed(); return _self->height; }
            set { ThrowIfDisposed(); _self->height = value; }
        }

        public override WindowStyle Style
        {
            get { return default(WindowStyle); }
            set { }
        }

        public override WindowStyleEx StyleEx
        {
            get { return default(WindowStyleEx); }
            set { }
        }

        public override IntPtr MenuHandle
        {
            get { return default(IntPtr); }
            set { }
        }

        public override bool Hidden
        {
            get { ThrowIfDisposed(); return _self->hidden != 0; }
            set { ThrowIfDisposed(); _self->hidden = value ? 1 : 0; }
        }

        public override bool WindowlessRenderingEnabled
        {
            get { ThrowIfDisposed(); return _self->windowless_rendering_enabled != 0; }
            set { ThrowIfDisposed(); _self->windowless_rendering_enabled = value ? 1 : 0; }
        }

        public override bool SharedTextureEnabled
        {
            get { ThrowIfDisposed(); return _self->shared_texture_enabled != 0; }
            set { ThrowIfDisposed(); _self->shared_texture_enabled = value ? 1 : 0; }
        }

        public override bool ExternalBeginFrameEnabled
        {
            get { ThrowIfDisposed(); return _self->external_begin_frame_enabled != 0; }
            set { ThrowIfDisposed(); _self->external_begin_frame_enabled = value ? 1 : 0; }
        }
    }
}
