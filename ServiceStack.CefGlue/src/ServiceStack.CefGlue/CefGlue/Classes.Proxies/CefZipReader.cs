namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class that supports the reading of zip archives via the zlib unzip API.
    /// The methods of this class should only be called on the thread that creates
    /// the object.
    /// </summary>
    public sealed unsafe partial class CefZipReader
    {
        /// <summary>
        /// Create a new CefZipReader object. The returned object's methods can only
        /// be called from the thread that created the object.
        /// </summary>
        public static CefZipReader Create(CefStreamReader stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            return CefZipReader.FromNative(
                cef_zip_reader_t.create(stream.ToNative())
                );
        }

        /// <summary>
        /// Moves the cursor to the first file in the archive. Returns true if the
        /// cursor position was set successfully.
        /// </summary>
        public bool MoveToFirstFile()
        {
            return cef_zip_reader_t.move_to_first_file(_self) != 0;
        }

        /// <summary>
        /// Moves the cursor to the next file in the archive. Returns true if the
        /// cursor position was set successfully.
        /// </summary>
        public bool MoveToNextFile()
        {
            return cef_zip_reader_t.move_to_next_file(_self) != 0;
        }

        /// <summary>
        /// Moves the cursor to the specified file in the archive. If |caseSensitive|
        /// is true then the search will be case sensitive. Returns true if the cursor
        /// position was set successfully.
        /// </summary>
        public bool MoveToFile(string fileName, bool caseSensitive)
        {
            fixed (char* fileName_str = fileName)
            {
                var n_fileName = new cef_string_t(fileName_str, fileName != null ? fileName.Length : 0);

                return cef_zip_reader_t.move_to_file(_self, &n_fileName, caseSensitive ? 1 : 0) != 0;
            }
        }

        /// <summary>
        /// Closes the archive. This should be called directly to ensure that cleanup
        /// occurs on the correct thread.
        /// </summary>
        public bool Close()
        {
            return cef_zip_reader_t.close(_self) != 0;
        }

        /// <summary>
        /// The below methods act on the file at the current cursor position.
        /// Returns the name of the file.
        /// </summary>
        public string GetFileName()
        {
            var n_result = cef_zip_reader_t.get_file_name(_self);
            return cef_string_userfree.ToString(n_result);
        }

        /// <summary>
        /// Returns the uncompressed size of the file.
        /// </summary>
        public long GetFileSize()
        {
            return cef_zip_reader_t.get_file_size(_self);
        }

        /// <summary>
        /// Returns the last modified timestamp for the file.
        /// </summary>
        public DateTime GetFileLastModified()
        {
            var time = cef_zip_reader_t.get_file_last_modified(_self);
            return cef_time_t.ToDateTime(&time);
        }

        /// <summary>
        /// Opens the file for reading of uncompressed data. A read password may
        /// optionally be specified.
        /// </summary>
        public bool OpenFile(string password)
        {
            fixed (char* password_str = password)
            {
                var n_password = new cef_string_t(password_str, password != null ? password.Length : 0);
                return cef_zip_reader_t.open_file(_self, &n_password) != 0;
            }
        }

        /// <summary>
        /// Closes the file.
        /// </summary>
        public bool CloseFile()
        {
            return cef_zip_reader_t.close_file(_self) != 0;
        }

        /// <summary>
        /// Read uncompressed file contents into the specified buffer. Returns &lt; 0 if
        /// an error occurred, 0 if at the end of file, or the number of bytes read.
        /// </summary>
        public int ReadFile(byte[] buffer, int offset, int length)
        {
            if (offset < 0 || length < 0 || buffer.Length - offset < length) throw new ArgumentOutOfRangeException();

            fixed (byte* buffer_ptr = buffer)
            {
                return cef_zip_reader_t.read_file(_self, buffer_ptr + offset, (UIntPtr)length);
            }
        }

        /// <summary>
        /// Returns the current offset in the uncompressed file contents.
        /// </summary>
        public long Tell()
        {
            return cef_zip_reader_t.tell(_self);
        }

        /// <summary>
        /// Returns true if at end of the file contents.
        /// </summary>
        public bool Eof()
        {
            return cef_zip_reader_t.eof(_self) != 0;
        }
    }
}
