#if (_WINDOWS_CE)

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Amib.Threading.Internal
{
    /// <summary>
    /// EventWaitHandle class
    /// In WindowsCE this class doesn't exist and I needed the WaitAll and WaitAny implementation.
    /// So I wrote this class to implement these two methods with some of their overloads.
    /// It uses the WaitForMultipleObjects API to do the WaitAll and WaitAny.
    /// Note that this class doesn't even inherit from WaitHandle!
    /// </summary>
    public class STPEventWaitHandle
    {
        #region Public Constants

        public const int WaitTimeout = Timeout.Infinite;

        #endregion

        #region Private External Constants

        private const Int32 WAIT_FAILED = -1;
        private const Int32 WAIT_TIMEOUT = 0x102;
        private const UInt32 INFINITE = 0xFFFFFFFF;

        #endregion

        #region WaitAll and WaitAny

        internal static bool WaitOne(WaitHandle waitHandle, int millisecondsTimeout, bool exitContext)
        {
            return waitHandle.WaitOne(millisecondsTimeout, exitContext);
        }

	    private static IntPtr[] PrepareNativeHandles(WaitHandle[] waitHandles)
	    {
	        IntPtr[] nativeHandles = new IntPtr[waitHandles.Length];
	        for (int i = 0; i < waitHandles.Length; i++)
	        {
                nativeHandles[i] = waitHandles[i].Handle;
	        }
	        return nativeHandles;
	    }

	    public static bool WaitAll(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
	    {
            uint timeout = millisecondsTimeout < 0 ? INFINITE : (uint)millisecondsTimeout;

            IntPtr[] nativeHandles = PrepareNativeHandles(waitHandles);

	        int result = WaitForMultipleObjects((uint)waitHandles.Length, nativeHandles, true, timeout);

            if (result == WAIT_TIMEOUT || result == WAIT_FAILED)
            {
                return false;
            }

	        return true;
	    }


	    public static int WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
        {
            uint timeout = millisecondsTimeout < 0 ? INFINITE : (uint)millisecondsTimeout;

            IntPtr[] nativeHandles = PrepareNativeHandles(waitHandles);

            int result = WaitForMultipleObjects((uint)waitHandles.Length, nativeHandles, false, timeout);

            if (result >= 0 && result < waitHandles.Length)
            {
                return result;
            }

            return -1;
        }

        public static int WaitAny(WaitHandle[] waitHandles)
        {
            return WaitAny(waitHandles, Timeout.Infinite, false);
        }

        public static int WaitAny(WaitHandle[] waitHandles, TimeSpan timeout, bool exitContext)
        {
            int millisecondsTimeout = (int)timeout.TotalMilliseconds;

            return WaitAny(waitHandles, millisecondsTimeout, false);
        }
 
        #endregion

        #region External methods

        [DllImport("coredll.dll", SetLastError = true)]
        public static extern int WaitForMultipleObjects(uint nCount, IntPtr[] lpHandles, bool fWaitAll, uint dwMilliseconds);

        #endregion
    }
}
#endif