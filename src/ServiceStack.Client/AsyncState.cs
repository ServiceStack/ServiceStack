// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace ServiceStack
{
    internal class AsyncState<TResponse> : IDisposable
    {
        private bool timedOut; // Pass the correct error back even on Async Calls

        public AsyncState(int bufferSize)
        {
            BufferRead = new byte[bufferSize];
            TextData = new StringBuilder();
            BytesData = new MemoryStream(bufferSize);
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

        public bool HandleCallbackOnUIThread;

        public long ResponseBytesRead;

        public long ResponseContentLength;

        public void HandleSuccess(TResponse response)
        {
            StopTimer();

            if (this.OnSuccess == null)
                return;

#if SL5 && !NETFX_CORE
                if (this.HandleCallbackOnUIThread)
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => this.OnSuccess(response));
                else
                    this.OnSuccess(response);
#else
            this.OnSuccess(response);
#endif
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

#if SL5 && !NETFX_CORE
                if (this.HandleCallbackOnUIThread)
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => this.OnError(response, toReturn));
                else
                    this.OnError(response, toReturn);
#else
            OnError(response, toReturn);
#endif
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