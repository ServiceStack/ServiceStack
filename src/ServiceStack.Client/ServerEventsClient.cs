using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack
{
    public class ServerEventConnect : ServerEventJoin
    {
        public string Id { get; set; }
        public string UnRegisterUrl { get; set; }
        public string HeartbeatUrl { get; set; }
        public long HeartbeatIntervalMs { get; set; }
    }

    public class ServerEventJoin : ServerEventCommand
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string ProfileUrl { get; set; }
    }

    public class ServerEventLeave : ServerEventCommand {}

    public class ServerEventCommand : ServerEventMessage { }

    public class ServerEventMessage : IMeta
    {
        public long EventId { get; set; }
        public string Data { get; set; }
        public string Selector { get; set; }
        public string Json { get; set; }
        public string Op { get; set; }
        public string CssSelector { get; set; }

        public Dictionary<string, string> Meta { get; set; }
    }

    public class ServerEventsClient : IDisposable
    {
        private static ILog log = LogManager.GetLogger(typeof(ServerEventsClient));

        public static int BufferSize = 1024 * 64;
        static int DefaultHeartbeatMs = 10 * 1000;

        byte[] buffer;
        Encoding encoding = new UTF8Encoding();

        HttpWebRequest httpReq;
        CancellationTokenSource cancel;

        public ServerEventConnect ConnectionInfo { get; private set; }

        public string SubscriptionId
        {
            get { return ConnectionInfo != null ? ConnectionInfo.Id : null; }
        }

        public string EventStreamUri { get; set; }
        public IServiceClient ServiceClient { get; set; }

        public Action<ServerEventConnect> OnConnect;
        public Action<ServerEventMessage> OnCommand;
        public Action<ServerEventMessage> OnMessage;
        public Action<Exception> OnException;

        public ServerEventsClient(string baseUri, string channel=null)
        {
            this.EventStreamUri = baseUri.CombineWith("event-stream");
            if (channel != null)
                this.EventStreamUri = this.EventStreamUri.AddQueryParam("channel", channel);

            this.ServiceClient = new JsonServiceClient(baseUri);
        }

        public ServerEventsClient Start()
        {
            httpReq = (HttpWebRequest)WebRequest.Create(EventStreamUri);
            //httpReq.AllowReadStreamBuffering = false; //.NET v4.5

            var response = PclExport.Instance.GetResponse(httpReq);
            var stream = response.GetResponseStream();

            buffer = new byte[BufferSize];
            cancel = new CancellationTokenSource();
            connectTcs = new TaskCompletionSource<ServerEventConnect>();
            commandTcs = new TaskCompletionSource<ServerEventCommand>();
            messageTcs = new TaskCompletionSource<ServerEventMessage>();

            ProcessResponse(stream);

            return this;
        }

        private TaskCompletionSource<ServerEventConnect> connectTcs;
        public Task<ServerEventConnect> Connect()
        {
            if (httpReq == null)
                Start();

            return connectTcs.Task;
        }

        private TaskCompletionSource<ServerEventCommand> commandTcs;
        public Task<ServerEventCommand> WaitForNextCommand()
        {
            return commandTcs.Task;
        }

        private TaskCompletionSource<ServerEventMessage> messageTcs;
        public Task<ServerEventMessage> WaitForNextMessage()
        {
            if (messageTcs.Task.IsCompleted)
            {
                log.WarnFormat("WaitForNextMessage Already Completed: {0}", messageTcs.Task.Result.EventId);
            }

            return messageTcs.Task;
        }

        protected void OnConnectReceived()
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("OnConnectReceived: {0} on #{1}", ConnectionInfo.EventId, ConnectionInfo.DisplayName);

            var hold = connectTcs;
            connectTcs = new TaskCompletionSource<ServerEventConnect>();

            if (OnConnect != null)
                OnConnect(ConnectionInfo);

            hold.SetResult(ConnectionInfo);
        }

        protected void OnCommandReceived(ServerEventCommand e)
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("OnCommandReceived: {0} on #{1}", e.EventId, ConnectionInfo.DisplayName);

            var hold = commandTcs;
            commandTcs = new TaskCompletionSource<ServerEventCommand>();

            if (OnCommand != null)
                OnCommand(e);

            hold.SetResult(e);
        }

        protected void OnMessageReceived(ServerEventMessage e)
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("OnMessageReceived: {0} on #{1}", e.EventId, ConnectionInfo.DisplayName);

            var hold = messageTcs;
            messageTcs = new TaskCompletionSource<ServerEventMessage>();

            if (OnMessage != null)
                OnMessage(e);

            hold.SetResult(e);
        }

        protected void OnExceptionReceived(Exception ex)
        {
            ex = ex.UnwrapIfSingleException();

            if (OnException != null)
                OnException(ex);

            if (ConnectionInfo == null)
                connectTcs.SetException(ex);

            messageTcs.SetException(ex);
        }

        private string overflowText = "";
        public void ProcessResponse(Stream stream)
        {
            if (!stream.CanRead) return;

            var task = stream.ReadAsync(buffer, 0, 2048, cancel.Token);
            task.ContinueWith(t =>
            {
                if (cancel.IsCancellationRequested || t.IsCanceled)
                {
                    httpReq = null;
                    return;
                }

                if (t.IsFaulted)
                {
                    OnExceptionReceived(t.Exception);
                    httpReq = null;
                    return;
                }

                int len = task.Result;
                if (len > 0)
                {
                    var text = overflowText + encoding.GetString(buffer, 0, len);
                    int pos;
                    while ((pos = text.IndexOf('\n')) >= 0)
                    {
                        if (text == "\n")
                        {
                            ProcessEventMessage(currentMsg);
                            text = "";
                            break;
                        }

                        var line = text.Substring(0, pos);
                        ProcessLine(line);
                        if (text.Length > pos + 1)
                            text = text.Substring(pos + 1);
                    }

                    overflowText = text;

                    ProcessResponse(stream);
                }
            });
        }

        private ServerEventMessage currentMsg;
        void ProcessLine(string line)
        {
            if (line == null) return;

            if (currentMsg == null)
                currentMsg = new ServerEventMessage();

            var parts = line.SplitOnFirst(':');
            var label = parts[0];
            var data = parts[1];
            if (data.Length > 0 && data[0] == ' ')
                data = data.Substring(1);

            switch (label)
            {
                case "id":
                    currentMsg.EventId = long.Parse(data);
                    break;
                case "data":
                    currentMsg.Data = data;
                    break;
            }
        }

        void ProcessEventMessage(ServerEventMessage e)
        {
            var parts = e.Data.SplitOnFirst(' ');
            e.Selector = parts[0];
            e.Json = parts[1];

            //"Message Received: ".Fmt(e.Selector);

            if (!string.IsNullOrEmpty(e.Selector))
            {
                parts = e.Selector.SplitOnFirst('.');
                e.Op = parts[0];
                var target = parts[1].Replace("%20", " ");

                var tokens = target.SplitOnFirst('$');
                var cmd = tokens[0];
                if (tokens.Length > 1)
                    e.CssSelector = tokens[1];

                if (e.Op == "cmd")
                {
                    switch (cmd)
                    {
                        case "onConnect":
                            ProcessOnConnectMessage(e);
                            return;
                        case "onJoin":
                            ProcessOnJoinMessage(e);
                            return;
                        case "onLeave":
                            ProcessOnLeaveMessage(e);
                            return;
                        default:
                            break;
                    }
                }
            }

            OnMessageReceived(e);
        }

        private void ProcessOnConnectMessage(ServerEventMessage e)
        {
            var msg = JsonObject.Parse(e.Json);
            ConnectionInfo = new ServerEventConnect {
                HeartbeatIntervalMs = DefaultHeartbeatMs,
            }.Populate(e, msg);

            ConnectionInfo.Id = msg.Get("id");
            ConnectionInfo.HeartbeatUrl = msg.Get("heartbeatUrl");
            ConnectionInfo.HeartbeatIntervalMs = msg.Get<long>("heartbeatIntervalMs");
            ConnectionInfo.UnRegisterUrl = msg.Get("unRegisterUrl");
            ConnectionInfo.UserId = msg.Get("userId");
            ConnectionInfo.DisplayName = msg.Get("displayName");
            ConnectionInfo.ProfileUrl = msg.Get("profileUrl");

            OnConnectReceived();
        }

        private void ProcessOnJoinMessage(ServerEventMessage e)
        {
            var msg = JsonObject.Parse(e.Json);
            var joinMsg = new ServerEventJoin().Populate(e, msg);
            joinMsg.UserId = msg.Get("userId");
            joinMsg.DisplayName = msg.Get("displayName");
            joinMsg.ProfileUrl = msg.Get("profileUrl");

            OnCommandReceived(joinMsg);
        }

        private void ProcessOnLeaveMessage(ServerEventMessage e)
        {
            var msg = JsonObject.Parse(e.Json);
            var leaveMsg = new ServerEventLeave().Populate(e, msg);

            OnCommandReceived(leaveMsg);
        }

        public void Dispose()
        {
            if (cancel != null)
                cancel.Cancel();

            if (ConnectionInfo != null && ConnectionInfo.UnRegisterUrl != null)
            {
                ConnectionInfo.UnRegisterUrl.GetStringFromUrlAsync();
            }
   
            cancel = null;
            httpReq = null;
        }
    }

    public static class ServerEventClientExtensions
    {
        public static T Populate<T>(this T dst, ServerEventMessage src, JsonObject msg) where T : ServerEventMessage
        {
            dst.EventId = src.EventId;
            dst.Data = src.Data;
            dst.Selector = src.Selector;
            dst.Json = src.Json;
            dst.Op = src.Op;

            if (dst.Meta == null)
                dst.Meta = new Dictionary<string, string>();

            foreach (var entry in msg)
            {
                dst.Meta[entry.Key] = entry.Value;
            }

            return dst;
        }
    }
}