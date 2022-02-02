namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class used to write data to a stream. The methods of this class may be called
    /// on any thread.
    /// </summary>
    public sealed unsafe partial class CefStreamWriter
    {
        /// <summary>
        /// Create a new CefStreamWriter object for a file.
        /// </summary>
        public static CefStreamWriter Create(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");

            fixed (char* fileName_str = fileName)
            {
                var n_fileName = new cef_string_t(fileName_str, fileName != null ? fileName.Length : 0);
                return CefStreamWriter.FromNative(
                    cef_stream_writer_t.create_for_file(&n_fileName)
                    );
            }
        }

        /// <summary>
        /// Create a new CefStreamWriter object for a custom handler.
        /// </summary>
        public static CefStreamWriter Create(CefWriteHandler handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");

            return CefStreamWriter.FromNative(
                cef_stream_writer_t.create_for_handler(handler.ToNative())
                );
        }

        /// <summary>
        /// Write raw binary data.
        /// </summary>
        public int Write(byte[] buffer, int offset, int length)
        {
            if (offset < 0 || length < 0 || buffer.Length - offset < length) throw new ArgumentOutOfRangeException();

            fixed (byte* ptr = &buffer[offset])
            {
                return (int)cef_stream_writer_t.write(_self, ptr, (UIntPtr)1, (UIntPtr)length);
            }
        }

        /// <summary>
        /// Seek to the specified offset position. |whence| may be any one of
        /// SEEK_CUR, SEEK_END or SEEK_SET. Returns zero on success and non-zero on
        /// failure.
        /// </summary>
        public bool Seek(long offset, SeekOrigin whence)
        {
            return cef_stream_writer_t.seek(_self, offset, (int)whence) == 0;
        }

        /// <summary>
        /// Return the current offset position.
        /// </summary>
        public long Tell()
        {
            return cef_stream_writer_t.tell(_self);
        }

        /// <summary>
        /// Flush the stream.
        /// </summary>
        public bool Flush()
        {
            return cef_stream_writer_t.flush(_self) == 0;
        }

        /// <summary>
        /// Returns true if this writer performs work like accessing the file system
        /// which may block. Used as a hint for determining the thread to access the
        /// writer from.
        /// </summary>
        public bool MayBlock()
        {
            return cef_stream_writer_t.may_block(_self) != 0;
        }
    }
}
