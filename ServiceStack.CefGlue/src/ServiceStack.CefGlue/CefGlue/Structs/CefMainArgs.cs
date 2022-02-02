namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;
    using Windows = Xilium.CefGlue.Platform.Windows;

    public sealed unsafe class CefMainArgs
    {
        private readonly string[] _args;
        private IntPtr _argcArgvBlock;

        public CefMainArgs(string[] args)
        {
            _args = args;
            _argcArgvBlock = IntPtr.Zero;
        }

        internal cef_main_args_t* ToNative()
        {
            switch (CefRuntime.Platform)
            {
                case CefRuntimePlatform.Windows:
                    return (cef_main_args_t*)ToNativeWindows();

                case CefRuntimePlatform.Linux:
                case CefRuntimePlatform.MacOS:
                    return (cef_main_args_t*)ToNativePosix();

                default:
                    throw ExceptionBuilder.UnsupportedPlatform();
            }
        }

        private cef_main_args_t_windows* ToNativeWindows()
        {
            var ptr = cef_main_args_t_windows.Alloc();
            ptr->instance = Windows.NativeMethods.GetModuleHandle(null);
            return ptr;
        }

        private cef_main_args_t_posix* ToNativePosix()
        {
            var ptr = cef_main_args_t_posix.Alloc();

            if (_args != null)
            {
                ptr->argv = MarshallArgcArgvBlock(_args);
                ptr->argc = _args.Length;
            }

            return ptr;
        }

        // TODO: check this, and reimplement CefMainArgs to do not leak memory
        private IntPtr MarshallArgcArgvBlock(string[] args)
        {
            Debug.Assert(args != null);

            var encoding = Encoding.UTF8;

            // calculate required block length
            var sizeOfArray = sizeof(IntPtr) * (args.Length + 1); // sizeof array of pointers to arguments
            var size = sizeOfArray; // size for entire block
            foreach (var arg in args)
            {
                size += 1 + encoding.GetByteCount(arg ?? "");
            }

            byte** argv = (byte**)Marshal.AllocHGlobal(size);
            byte* data = (byte*)argv + sizeOfArray;

            for (var i=0; i < args.Length; i++)
            {
                argv[i] = data;

                var bytes = encoding.GetBytes(args[i]);
                Marshal.Copy(bytes, 0, (IntPtr)data, bytes.Length);
                data += bytes.Length;
                data[0] = 0;
                data++;
            }

            argv[args.Length] = (byte*)0;

            return (IntPtr)argv;
        }

        private void FreeArgcArgvBlock(IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
        }

        internal static void Free(cef_main_args_t* ptr)
        {
            switch (CefRuntime.Platform)
            {
                case CefRuntimePlatform.Windows:
                    cef_main_args_t_windows.Free((cef_main_args_t_windows*)ptr);
                    return;

                case CefRuntimePlatform.Linux:
                case CefRuntimePlatform.MacOS:
                    cef_main_args_t_posix.Free((cef_main_args_t_posix*)ptr);
                    return;

                default:
                    throw ExceptionBuilder.UnsupportedPlatform();
            }
        }
    }
}
