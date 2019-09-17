using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class LispReplTcpServer : IPlugin, IPreInitPlugin, IAfterInitAppHost, IDisposable
    {
        private static ILog Log = LogManager.GetLogger(typeof(LispReplTcpServer));
        
        private int port;
        private IPAddress localIp;

        public LispReplTcpServer() : this(IPAddress.Loopback, 5002) {}
        public LispReplTcpServer(int port) : this(IPAddress.Loopback, port) {}
        public LispReplTcpServer(string localIp, int port) : this(IPAddress.Parse(localIp), port) {}

        private TcpListener listener;
        
        public LispReplTcpServer(IPAddress localIp, int port)
        {
            this.port = port;
            this.localIp = localIp ?? IPAddress.Loopback;
        }
        
        public ScriptContext ScriptContext { get; set; }
        
        /// <summary>
        /// Allow scripting of Types from specified Assemblies
        /// </summary>
        public List<Assembly> ScriptAssemblies { get; set; } = new List<Assembly>();
        
        /// <summary>
        /// Allow scripting of the specified Types
        /// </summary>
        public List<Type> ScriptTypes { get; set; } = new List<Type>();
        
        /// <summary>
        /// Lookup Namespaces for resolving Types in Scripts
        /// </summary>
        public List<string> ScriptNamespaces { get; set; } = new List<string>();
        
        /// <summary>
        /// Allow scripting of all Types in loaded Assemblies 
        /// </summary>
        public bool? AllowScriptingOfAllTypes { get; set; }

        public void BeforePluginsLoaded(IAppHost appHost)
        {
            if (ScriptContext == null)
                ScriptContext = appHost.AssertPlugin<SharpPagesFeature>();
            
            if (!ScriptContext.ScriptLanguages.Contains(ScriptLisp.Language))
                ScriptContext.ScriptLanguages.Add(ScriptLisp.Language);

            if (AllowScriptingOfAllTypes != null)
                ScriptContext.AllowScriptingOfAllTypes = AllowScriptingOfAllTypes.Value;
            if (ScriptAssemblies != null)
                ScriptContext.ScriptAssemblies.AddRange(ScriptAssemblies);
            if (ScriptTypes != null)
                ScriptContext.ScriptTypes.AddRange(ScriptTypes);
            if (ScriptNamespaces != null)
                ScriptContext.ScriptNamespaces.AddRange(ScriptNamespaces);
        }

        public void Register(IAppHost appHost) {}

        public void AfterInit(IAppHost appHost) => Start();

        private Thread bgThread = null;

        object olock = new object();
        public void Start()
        {
            if (bgThread != null)
                Stop();
            
            lock (olock)
            {
                Log.Info($"Starting LISP REPL on {localIp}:{port} ...");
                listener = new TcpListener(localIp, port);
                listener.Start();

                bgThread = new Thread(StartListening) {
                    IsBackground = true, 
                    Name = nameof(LispReplTcpServer) + " #" + Interlocked.Increment(ref serverStarts)
                };
                bgThread.Start();
            }
        }

        private static int serverStarts;
        private static int connections;

        public void StartListening()
        {
            try
            {
                while (listener != null)
                {
                    var client = listener?.AcceptTcpClient();
                    if (listener == null)
                        return;
                    
                    if (Log.IsDebugEnabled)
                        Log.Debug("Waiting for connection");
                    
                    var id = Interlocked.Increment(ref connections);
                    var remoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
                    var remoteIp = remoteEndPoint.Address + ":" + remoteEndPoint.Port;
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug($"#{id} client connected from {remoteIp}!");
                    }

                    var thread = new Thread(() => HandleConnection(client, id, remoteIp)) {
                        IsBackground = true
                    };
                    thread.Start();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{nameof(LispReplTcpServer)} TcpListener error: {ex.Message}", ex);
            }
            finally
            {
                Stop();
            }
        }
        
        public void Stop()
        {
            lock (olock)
            {
                var hold = listener;
                listener = null;
                hold?.Stop();
                KillBgThreadIfExists();
            }
        }
        
        private void KillBgThreadIfExists()
        {
            if (bgThread != null && bgThread.IsAlive)
            {
                //give it a small chance to die gracefully
                if (!bgThread.Join(500))
                {
#if !NETSTANDARD2_0                    
                    //Ideally we shouldn't get here, but lets try our hardest to clean it up
                    Log.Warn("Interrupting previous Background Thread: " + bgThread.Name);
                    bgThread.Interrupt();
                    if (!bgThread.Join(TimeSpan.FromSeconds(3)))
                    {
                        Log.Warn(bgThread.Name + " just wont die, so we're now aborting it...");
                        bgThread.Abort();
                    }
#endif
                }
                bgThread = null;
            }
        }

        public void Dispose() => Stop();

        private void HandleConnection(TcpClient client, int id, string remoteIp)
        {
            try 
            { 
                using (client)
                {
                    var interp = Lisp.CreateInterpreter();

                    var sb = new StringBuilder();
                    var networkStream = client.GetStream();
                    var remoteUrl = $"tcp://{remoteIp}";
                    
                    using (var reader = new StreamReader(networkStream, Encoding.UTF8)) 
                    {
                        MemoryProvider.Instance.Write(networkStream, 
                            $"\nWelcome to #Script Lisp! The Server time is: {DateTime.Now.ToShortTimeString()}\n\n".AsMemory());
                        
                        while (true)
                        {
                            if (listener == null)
                                return;
                            
                            MemoryProvider.Instance.Write(networkStream, "> ".AsMemory());

                            sb.Clear();

                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                sb.AppendLine(line);
                                if (line == "") // evaluate on empty new line
                                    break;
                            }

                            var lisp = sb.ToString();
                            if (lisp.Trim().Length == 0)
                                continue;

                            string output = null;
                            try
                            {
                                var requestArgs = SharpPagesFeatureExtensions.CreateRequestArgs(new Dictionary<string, object> {
                                    [ScriptConstants.BaseUrl] = $"tcp://{localIp}:{port}/",
                                    [nameof(IRequest.UrlReferrer)] = remoteUrl,
                                });
                                output = interp.ReplEval(ScriptContext, networkStream, lisp, requestArgs);
                            }
                            catch (Exception e)
                            {
                                var str = (e.InnerException ?? e) + "\n";
                                MemoryProvider.Instance.Write(networkStream, str.AsMemory());
                            }

                            if (!string.IsNullOrEmpty(output))
                            {
                                MemoryProvider.Instance.Write(networkStream, output.AsMemory());
                                MemoryProvider.Instance.Write(networkStream, "\n\n".AsMemory());
                                networkStream.Flush();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Log.IsDebugEnabled)
                    Log.Debug($"{remoteIp} connection was disconnected. " + ex.Message);
            }
        }
    }
}