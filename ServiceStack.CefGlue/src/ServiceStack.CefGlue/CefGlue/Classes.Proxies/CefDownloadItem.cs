namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class used to represent a download item.
    /// </summary>
    public sealed unsafe partial class CefDownloadItem
    {
        /// <summary>
        /// Returns true if this object is valid. Do not call any other methods if this
        /// function returns false.
        /// </summary>
        public bool IsValid
        {
            get { return cef_download_item_t.is_valid(_self) != 0; }
        }

        /// <summary>
        /// Returns true if the download is in progress.
        /// </summary>
        public bool IsInProgress
        {
            get { return cef_download_item_t.is_in_progress(_self) != 0; }
        }

        /// <summary>
        /// Returns true if the download is complete.
        /// </summary>
        public bool IsComplete
        {
            get { return cef_download_item_t.is_complete(_self) != 0; }
        }

        /// <summary>
        /// Returns true if the download has been canceled or interrupted.
        /// </summary>
        public bool IsCanceled
        {
            get { return cef_download_item_t.is_canceled(_self) != 0; }
        }

        /// <summary>
        /// Returns a simple speed estimate in bytes/s.
        /// </summary>
        public long CurrentSpeed
        {
            get { return cef_download_item_t.get_current_speed(_self); }
        }

        /// <summary>
        /// Returns the rough percent complete or -1 if the receive total size is
        /// unknown.
        /// </summary>
        public int PercentComplete
        {
            get { return cef_download_item_t.get_percent_complete(_self); }
        }

        /// <summary>
        /// Returns the total number of bytes.
        /// </summary>
        public long TotalBytes
        {
            get { return cef_download_item_t.get_total_bytes(_self); }
        }

        /// <summary>
        /// Returns the number of received bytes.
        /// </summary>
        public long ReceivedBytes
        {
            get { return cef_download_item_t.get_received_bytes(_self); }
        }

        /// <summary>
        /// Returns the time that the download started.
        /// </summary>
        public DateTime StartTime
        {
            get
            {
                var n_result = cef_download_item_t.get_start_time(_self);
                return cef_time_t.ToDateTime(&n_result);
            }
        }

        /// <summary>
        /// Returns the time that the download ended.
        /// </summary>
        public DateTime EndTime
        {
            get
            {
                var n_result = cef_download_item_t.get_end_time(_self);
                return cef_time_t.ToDateTime(&n_result);
            }
        }

        /// <summary>
        /// Returns the full path to the downloaded or downloading file.
        /// </summary>
        public string FullPath
        {
            get
            {
                var n_result = cef_download_item_t.get_full_path(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the unique identifier for this download.
        /// </summary>
        public uint Id
        {
            get { return cef_download_item_t.get_id(_self); }
        }

        /// <summary>
        /// Returns the URL.
        /// </summary>
        public string Url
        {
            get
            {
                var n_result = cef_download_item_t.get_url(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the original URL before any redirections.
        /// </summary>
        public string OriginalUrl
        {
            get
            {
                var n_result = cef_download_item_t.get_original_url(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the suggested file name.
        /// </summary>
        public string SuggestedFileName
        {
            get
            {
                var n_result = cef_download_item_t.get_suggested_file_name(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the content disposition.
        /// </summary>
        public string ContentDisposition
        {
            get
            {
                var n_result = cef_download_item_t.get_content_disposition(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the mime type.
        /// </summary>
        public string MimeType
        {
            get
            {
                var n_result = cef_download_item_t.get_mime_type(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }
    }
}
