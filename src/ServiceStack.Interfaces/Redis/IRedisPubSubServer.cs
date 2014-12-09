using System;

namespace ServiceStack.Redis
{
    public interface IRedisPubSubServer : IDisposable
    {
        Action OnInit { get; set; }
        Action OnStart { get; set; }
        Action OnStop { get; set; }
        Action OnDispose { get; set; }
        Action<string, string> OnMessage { get; set; }
        Action<string> OnUnSubscribe { get; set; }
        Action<Exception> OnError { get; set; }
        Action<IRedisPubSubServer> OnFailover { get; set; }

        IRedisClientsManager ClientsManager { get; }
        string[] Channels { get; }

        TimeSpan? WaitBeforeNextRestart { get; set; }
        DateTime CurrentServerTime { get; }

        string GetStatus();
        string GetStatsDescription();

        IRedisPubSubServer Start();
        void Stop();
        void Restart();
    }
}