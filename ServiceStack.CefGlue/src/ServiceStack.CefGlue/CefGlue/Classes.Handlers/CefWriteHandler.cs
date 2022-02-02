namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;
    
    /// <summary>
    /// Interface the client can implement to provide a custom stream writer. The
    /// methods of this class may be called on any thread.
    /// </summary>
    public abstract unsafe partial class CefWriteHandler
    {
        private UIntPtr write(cef_write_handler_t* self, void* ptr, UIntPtr size, UIntPtr n)
        {
            CheckSelf(self);

            var length = (long)size * (long)n;
            using (var stream = new UnmanagedMemoryStream((byte*)ptr, length, length, FileAccess.Write))
            {
                return (UIntPtr)Write(stream, length);
            }
        }
        
        /// <summary>
        /// Write raw binary data.
        /// </summary>
        protected abstract long Write(Stream stream, long length);

        
        private int seek(cef_write_handler_t* self, long offset, int whence)
        {
            CheckSelf(self);

            return Seek(offset, (SeekOrigin)whence) ? 0 : -1;
        }
        
        /// <summary>
        /// Seek to the specified offset position. |whence| may be any one of
        /// SEEK_CUR, SEEK_END or SEEK_SET. Return zero on success and non-zero on
        /// failure.
        /// </summary>
        protected abstract bool Seek(long offset, SeekOrigin whence);
        

        private long tell(cef_write_handler_t* self)
        {
            CheckSelf(self);

            return Tell();
        }
        
        /// <summary>
        /// Return the current offset position.
        /// </summary>
        protected abstract long Tell();

        
        private int flush(cef_write_handler_t* self)
        {
            CheckSelf(self);

            return Flush() ? 0 : -1;
        }
        
        /// <summary>
        /// Flush the stream.
        /// </summary>
        protected abstract bool Flush();


        private int may_block(cef_write_handler_t* self)
        {
            CheckSelf(self);

            return MayBlock() ? 1 : 0;
        }

        /// <summary>
        /// Return true if this handler performs work like accessing the file system
        /// which may block. Used as a hint for determining the thread to access the
        /// handler from.
        /// </summary>
        protected abstract bool MayBlock();
    }
}
