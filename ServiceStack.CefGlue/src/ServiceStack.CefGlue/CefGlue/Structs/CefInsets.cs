namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Xilium.CefGlue.Interop;

    public struct CefInsets
    {
        private int _top;
        private int _left;
        private int _bottom;
        private int _right;

        public CefInsets(int top, int left, int bottom, int right)
        {
            _top = top;
            _left = left;
            _bottom = bottom;
            _right = right;
        }

        public int Top
        {
            get { return _top; }
            set { _top = value; }
        }

        public int Left
        {
            get { return _left; }
            set { _left = value; }
        }

        public int Bottom
        {
            get { return _bottom; }
            set { _bottom = value; }
        }

        public int Right
        {
            get { return _right; }
            set { _right = value; }
        }
    }
}
