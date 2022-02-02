namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Xilium.CefGlue.Interop;

    public sealed unsafe class CefPopupFeatures
    {
        private cef_popup_features_t* _self;

        internal CefPopupFeatures(cef_popup_features_t* ptr)
        {
            _self = ptr;
        }

        internal void Dispose()
        {
            _self = null;
        }

        public int? X
        {
            get
            {
                ThrowIfObjectDisposed();
                return _self->xSet != 0 ? (int?)_self->x : null;
            }
        }

        public int? Y
        {
            get
            {
                ThrowIfObjectDisposed();
                return _self->ySet != 0 ? (int?)_self->y : null;
            }
        }

        public int? Width
        {
            get
            {
                ThrowIfObjectDisposed();
                return _self->widthSet != 0 ? (int?)_self->width : null;
            }
        }

        public int? Height
        {
            get
            {
                ThrowIfObjectDisposed();
                return _self->heightSet != 0 ? (int?)_self->height : null;
            }
        }

        public bool MenuBarVisible
        {
            get
            {
                ThrowIfObjectDisposed();
                return _self->menuBarVisible != 0;
            }
        }

        public bool StatusBarVisible
        {
            get
            {
                ThrowIfObjectDisposed();
                return _self->statusBarVisible != 0;
            }
        }

        public bool ToolBarVisible
        {
            get
            {
                ThrowIfObjectDisposed();
                return _self->toolBarVisible != 0;
            }
        }

        public bool ScrollbarsVisible
        {
            get
            {
                ThrowIfObjectDisposed();
                return _self->scrollbarsVisible != 0;
            }
        }

        private void ThrowIfObjectDisposed()
        {
            if (_self == null) throw ExceptionBuilder.ObjectDisposed();
        }
    }
}
