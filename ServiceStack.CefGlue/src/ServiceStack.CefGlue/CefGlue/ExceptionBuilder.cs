namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    internal static class ExceptionBuilder
    {
        public static CefVersionMismatchException RuntimeVersionBuildRevisionMismatch(int actual, int expected)
        {
            return new CefVersionMismatchException(string.Format(CultureInfo.InvariantCulture,
                "CEF runtime version mismatch: loaded revision {0}, but supported {1}.",
                actual,
                expected
                ));
        }

        public static CefVersionMismatchException RuntimeVersionApiHashMismatch(string actual, string expected, string expectedVersion)
        {
            return new CefVersionMismatchException(string.Format(CultureInfo.InvariantCulture,
                "CEF runtime version mismatch: loaded version API hash \"{0}\", but supported \"{1}\" ({2}).",
                actual,
                expected,
                expectedVersion
                ));
        }

        public static Exception CefRuntimeAlreadyInitialized()
        {
            return new InvalidOperationException("CEF runtime already initialized.");
        }

        public static Exception CefRuntimeNotInitialized()
        {
            return new InvalidOperationException("CEF runtime is not initialized.");
        }

        public static Exception CefRuntimeFailedToInitialize()
        {
            return new InvalidOperationException("Failed to initialize CEF runtime.");
        }

        public static Exception UnsupportedPlatform()
        {
            return new InvalidOperationException("Unsupported platform.");
        }

        public static Exception InvalidSelfReference()
        {
            return new InvalidOperationException("Invalid self reference.");
        }

        public static Exception FailedToCreateBrowser()
        {
            return new InvalidOperationException("Failed to create browser.");
        }

        public static Exception ObjectDisposed()
        {
            return new InvalidOperationException("Object disposed.");
        }

        public static Exception ObjectNotFound()
        {
            return new InvalidOperationException("Failed to map pointer to object.");
        }
    }
}
