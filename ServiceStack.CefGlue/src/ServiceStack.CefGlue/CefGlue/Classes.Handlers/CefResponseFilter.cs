namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to filter resource response content. The methods of
    /// this class will be called on the browser process IO thread.
    /// </summary>
    public abstract unsafe partial class CefResponseFilter
    {
        private int init_filter(cef_response_filter_t* self)
        {
            CheckSelf(self);

            return InitFilter() ? 1 : 0;
        }

        /// <summary>
        /// Initialize the response filter. Will only be called a single time. The
        /// filter will not be installed if this method returns false.
        /// </summary>
        protected abstract bool InitFilter();


        private CefResponseFilterStatus filter(cef_response_filter_t* self, void* data_in, UIntPtr data_in_size, UIntPtr* data_in_read, void* data_out, UIntPtr data_out_size, UIntPtr* data_out_written)
        {
            CheckSelf(self);

            // TODO: Use some buffers instead of UnmanagedMemoryStream.

            // TODO: Remove UnmanagedMemoryStream - normal usage is buffer operations.
            UnmanagedMemoryStream m_in_stream = null;
            UnmanagedMemoryStream m_out_stream = null;
            try
            {
                if (data_in != null)
                {
                    m_in_stream = new UnmanagedMemoryStream((byte*)data_in, (long)data_in_size, (long)data_in_size, FileAccess.Read);
                }

                m_out_stream = new UnmanagedMemoryStream((byte*)data_out, 0, (long)data_out_size, FileAccess.Write);

                {
                    long m_inRead;
                    long m_outWritten;
                    var result = Filter(m_in_stream, (long)data_in_size, out m_inRead, m_out_stream, (long)data_out_size, out m_outWritten);
                    *data_in_read = (UIntPtr)m_inRead;
                    *data_out_written = (UIntPtr)m_outWritten;
                    return result;
                }
            }
            finally
            {
                m_out_stream?.Dispose();
                m_in_stream?.Dispose();
            }
        }

        /// <summary>
        /// Called to filter a chunk of data. Expected usage is as follows:
        ///
        ///  A. Read input data from |data_in| and set |data_in_read| to the number of
        ///     bytes that were read up to a maximum of |data_in_size|. |data_in| will
        ///     be NULL if |data_in_size| is zero.
        ///  B. Write filtered output data to |data_out| and set |data_out_written| to
        ///     the number of bytes that were written up to a maximum of
        ///     |data_out_size|. If no output data was written then all data must be
        ///     read from |data_in| (user must set |data_in_read| = |data_in_size|).
        ///  C. Return RESPONSE_FILTER_DONE if all output data was written or
        ///     RESPONSE_FILTER_NEED_MORE_DATA if output data is still pending.
        ///
        /// This method will be called repeatedly until the input buffer has been
        /// fully read (user sets |data_in_read| = |data_in_size|) and there is no
        /// more input data to filter (the resource response is complete). This method
        /// may then be called an additional time with an empty input buffer if the
        /// user filled the output buffer (set |data_out_written| = |data_out_size|)
        /// and returned RESPONSE_FILTER_NEED_MORE_DATA to indicate that output data is
        /// still pending.
        ///
        /// Calls to this method will stop when one of the following conditions is met:
        ///
        ///  A. There is no more input data to filter (the resource response is
        ///     complete) and the user sets |data_out_written| = 0 or returns
        ///     RESPONSE_FILTER_DONE to indicate that all data has been written, or;
        ///  B. The user returns RESPONSE_FILTER_ERROR to indicate an error.
        ///
        /// Do not keep a reference to the buffers passed to this method.
        /// </summary>
        protected abstract CefResponseFilterStatus Filter(UnmanagedMemoryStream dataIn, long dataInSize, out long dataInRead, UnmanagedMemoryStream dataOut, long dataOutSize, out long dataOutWritten);
    }
}
