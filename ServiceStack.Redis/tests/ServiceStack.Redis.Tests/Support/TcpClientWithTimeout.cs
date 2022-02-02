#if !NETCORE
using System;
using System.Net.Sockets;
using System.Threading;

namespace ServiceStack.Redis.Tests.Support
{
    internal class TcpClientWithTimeout
    {
        protected string hostname;
        protected int port;
        protected TimeSpan timeout;
        protected TcpClient connection;
        protected bool connected;
        protected Exception exception;

        public TcpClientWithTimeout(string hostname, int port, TimeSpan timeout)
        {
            this.hostname = hostname;
            this.port = port;
            this.timeout = timeout;
        }
        public TcpClient Connect()
        {
            // kick off the thread that tries to connect
            connected = false;
            exception = null;

            var backgroundThread = new Thread(BeginConnect)
            {
                IsBackground = true
            };
            // wont prevent the process from terminating while it does the long timeout
            backgroundThread.Start();

            // wait for either the timeout or the thread to finish
            backgroundThread.Join(timeout);

            if (connected)
            {
                // it succeeded, so return the connection
                backgroundThread.Abort();
                return connection;
            }
            if (exception != null)
            {
                // it crashed, so return the exception to the caller
                backgroundThread.Abort();
                throw exception;
            }
            else
            {
                // if it gets here, it timed out, so abort the thread and throw an exception
                backgroundThread.Abort();
                var message = string.Format("TcpClient connection to {0}:{1} timed out",
                                            hostname, port);
                throw new TimeoutException(message);
            }
        }
        protected void BeginConnect()
        {
            try
            {
                connection = new TcpClient(hostname, port);
                // record that it succeeded, for the main thread to return to the caller
                connected = true;
            }
            catch (Exception ex)
            {
                // record the exception for the main thread to re-throw back to the calling code
                exception = ex;
            }
        }
    }

    internal class TcpClientExample
    {
        void Main()
        {
            // connect with a 5 second timeout on the connection
            var connection = new TcpClientWithTimeout(
                "www.google.com", 80, TimeSpan.FromSeconds(5)).Connect();

            var stream = connection.GetStream();

            // Send 10 bytes
            byte[] toSend = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0xa };
            stream.Write(toSend, 0, toSend.Length);

            // ReceiveMessages 10 bytes
            var readbuf = new byte[10]; // you must allocate space first
            stream.ReadTimeout = 10000; // 10 second timeout on the read
            stream.Read(readbuf, 0, 10); // read

            // Disconnect nicely
            stream.Close(); // workaround for a .net bug: http://support.microsoft.com/kb/821625
            connection.Close();
        }
    }
}
#endif