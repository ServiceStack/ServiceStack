namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class CefRuntimeException : Exception
    {
        public CefRuntimeException(string message)
            : base(message)
        {
        }
    }
}
