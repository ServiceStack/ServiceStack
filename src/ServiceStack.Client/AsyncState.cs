// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using ServiceStack.Text;

namespace ServiceStack
{
    public class AsyncState<TResponse> : IDisposable
    {
        private bool timedOut; // Pass the correct error back even on Async Calls

        public AsyncState(int bufferSize)
        {
            BufferRead = new byte[bufferSize];
            TextData = new StringBuilder();
            BytesData = MemoryStreamFactory.GetStream(bufferSize);
            WebRequest = null;
            ResponseStream = null;
        }

        public string HttpMethod;

        public string Url;

        public StringBuilder TextData;

        public MemoryStream BytesData;

        public byte[] BufferRead;

        public object Request;

        public HttpWebRequest WebRequest;

        public HttpWebResponse WebResponse;

        public Stream ResponseStream;

        public int Completed;

        public int RequestCount;

        public ITimer Timer;

        public Action<TResponse> OnSuccess;

        public Action<TResponse, Exception> OnError;

        public SynchronizationContext UseSynchronizationContext;

        public bool HandleCallbackOnUIThread;

        public long ResponseBytesRead;

        public long ResponseContentLength;

        public void HandleSuccess(TResponse response)
        {
            StopTimer();

            if (this.OnSuccess == null)
                return;

            if (UseSynchronizationContext != null)
                UseSynchronizationContext.Post(asyncState => this.OnSuccess(response), this);
            else if (this.HandleCallbackOnUIThread)
                PclExportClient.Instance.RunOnUiThread(() => this.OnSuccess(response));
            else
                this.OnSuccess(response);
        }

        public void HandleError(TResponse response, Exception ex)
        {
            StopTimer();

            if (this.OnError == null)
                return;

            var toReturn = ex;
            if (timedOut)
            {
                toReturn = ex.CreateTimeoutException("The request timed out");
            }

            if (UseSynchronizationContext != null)
                UseSynchronizationContext.Post(asyncState => this.OnError(response, toReturn), this);
            else if (this.HandleCallbackOnUIThread)
                PclExportClient.Instance.RunOnUiThread(() => this.OnError(response, toReturn));
            else
                this.OnError(response, toReturn);
        }

        public void StartTimer(TimeSpan timeOut)
        {
            this.Timer = this.CreateTimer(timeOut);
        }

        public void StopTimer()
        {
            if (this.Timer != null)
            {
                this.Timer.Cancel();
                this.Timer = null;
            }
        }

#if NETFX_CORE
            public void TimedOut(ThreadPoolTimer timer)
            {
                if (Interlocked.Increment(ref Completed) == 1)
                {
                    if (this.WebRequest != null)
                    {
                        timedOut = true;
                        this.WebRequest.Abort();
                    }
                }

                StopTimer();

                this.Dispose();
            }
#else
        public void TimedOut(object state)
        {
            if (Interlocked.Increment(ref Completed) == 1)
            {
                if (this.WebRequest != null)
                {
                    timedOut = true;
                    this.WebRequest.Abort();
                }
            }

            StopTimer();

            this.Dispose();
        }
#endif

        public void Dispose()
        {
            if (this.BytesData != null)
            {
                this.BytesData.Dispose();
                this.BytesData = null;
            }
            if (this.Timer != null)
            {
                this.Timer.Dispose();
                this.Timer = null;
            }
        }
    }
}