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

namespace ServiceStack;

public class LispReplTcpServer : IPlugin, IPreInitPlugin, IAfterInitAppHost, IDisposable, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.LispTcpServer;
    private static ILog Log = LogManager.GetLogger(typeof(LispReplTcpServer));
        
    private int port;
    private IPAddress localIp;

    public LispReplTcpServer() : this(IPAddress.Loopback, 5005) {}
    public LispReplTcpServer(int port) : this(IPAddress.Loopback, port) {}
    public LispReplTcpServer(string localIp, int port) : this(IPAddress.Parse(localIp), port) {}

    private TcpListener listener;
        
    public LispReplTcpServer(IPAddress localIp, int port)
    {
        this.port = port;
        this.localIp = localIp ?? IPAddress.Loopback;
    }

    /// <summary>
    /// Load the Lisp TCP Repl within this ScriptContext (falls back to SharpPagesFeature) 
    /// </summary>
    public ScriptContext ScriptContext { get; set; }

    /// <summary>
    /// Whether to Require Config.AdminAuthSecret to Access REPL
    /// </summary>
    public bool RequireAuthSecret { get; set; }
        
    /// <summary>
    /// Additional Script Methods you want to add to the ScriptContext when Lisp TCP Repl is running 
    /// </summary>
    public List<ScriptMethods> ScriptMethods { get; set; } = new List<ScriptMethods>();

    /// <summary>
    /// Additional Script Blocks you want to add to the ScriptContext when Lisp TCP Repl is running 
    /// </summary>
    public List<ScriptBlock> ScriptBlocks { get; set; } = new List<ScriptBlock>();

    /// <summary>
    /// Scan Types and auto-register any Script Methods, Blocks and Code Pages
    /// </summary>
    public List<Type> ScanTypes { get; set; } = new List<Type>();

    /// <summary>
    /// Scan Assemblies and auto-register any Script Methods, Blocks and Code Pages
    /// </summary>
    public List<Assembly> ScanAssemblies { get; set; } = new List<Assembly>();
        
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
            
        if (!ScriptMethods.IsEmpty())
            ScriptContext.ScriptMethods.AddRange(ScriptMethods);
        if (!ScriptBlocks.IsEmpty())
            ScriptContext.ScriptBlocks.AddRange(ScriptBlocks);
        if (!ScanTypes.IsEmpty())
            ScriptContext.ScanTypes.AddRange(ScanTypes);
        if (!ScanAssemblies.IsEmpty())
            ScriptContext.ScanAssemblies.AddRange(ScanAssemblies);
            
        if (!ScriptAssemblies.IsEmpty())
            ScriptContext.ScriptAssemblies.AddRange(ScriptAssemblies);
        if (!ScriptTypes.IsEmpty())
            ScriptContext.ScriptTypes.AddRange(ScriptTypes);
        if (!ScriptNamespaces.IsEmpty())
            ScriptContext.ScriptNamespaces.AddRange(ScriptNamespaces);
    }

    public void Register(IAppHost appHost)
    {
        if (RequireAuthSecret && string.IsNullOrEmpty(appHost.Config.AdminAuthSecret))
            throw new NotSupportedException($"LISP REPL requires AuthSecret but Config.AdminAuthSecret is not configured");
    }

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
#if !NETCORE                    
                //Ideally we shouldn't get here, but lets try our hardest to clean it up
                Log.Warn("Interrupting previous Background Thread: " + bgThread.Name);
                bgThread.Interrupt();
                if (!bgThread.Join(TimeSpan.FromSeconds(3)))
                {
                    Log.Warn(bgThread.Name + " just wont die, so we're now aborting it...");
#pragma warning disable CS0618, SYSLIB0014, SYSLIB0006
                    bgThread.Abort();
#pragma warning restore CS0618, SYSLIB0014, SYSLIB0006
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

                var CMD_CLEAR = "\u001B[2J";
                var sb = new StringBuilder();
                var networkStream = client.GetStream();
                var remoteUrl = $"tcp://{remoteIp}";
                void write(string msg) => MemoryProvider.Instance.Write(networkStream, msg.AsMemory());
                    
                using (var reader = new StreamReader(networkStream, Encoding.UTF8)) 
                {
                    string line;
                    if (RequireAuthSecret)
                    {
                        MemoryProvider.Instance.Write(networkStream, 
                            $"Authentication required:\n> ".AsMemory());

                        line = reader.ReadLine();
                        var authSuccess = !string.IsNullOrEmpty(line) && line == HostContext.Config.AdminAuthSecret; 
                        if (!authSuccess)
                        {
                            write($"Authentication failed.\n\n");
                            client.Close();
                            return;
                        }

                        write(CMD_CLEAR);
                    }
                        
                    MemoryProvider.Instance.Write(networkStream, 
                        $"\nWelcome to #Script Lisp! The Server time is: {DateTime.Now.ToShortTimeString()}, type ? for help.\n\n".AsMemory());

                    while (true)
                    {
                        if (listener == null)
                            return;
                        prompt:
                        write("> ");

                        sb.Clear();

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line == "quit" || line == "exit")
                            {
                                write($"Goodbye.\n\n");
                                client.Close();
                                return;
                            }
                            if (line == "verbose")
                            {
                                var toggle = interp.GetSymbolValue("verbose") is bool v && v
                                    ? null
                                    : Lisp.TRUE;

                                interp.SetSymbolValue("verbose", toggle);
                                var mode = toggle != null ? "on" : "off";
                                write($"verbose mode {mode}\n\n");
                                goto prompt;
                            }
                            if (line == "mode")
                            {
                                var toggle = interp.GetSymbolValue("multi-line") is bool v && v
                                    ? null
                                    : Lisp.TRUE;

                                interp.SetSymbolValue("multi-line", toggle);
                                var mode = toggle != null ? "off" : "on";
                                write($"single-line mode {mode}\n\n");
                                goto prompt;
                            }
                            if (line == "clear")
                            {
                                write(CMD_CLEAR);
                                goto prompt;
                            }
                            if (line == "?")
                            {
                                var usage = @"
 ; verbose - toggle output to indent complex responses
 ; mode    - toggle between single and multi-line modes 
 ; clear   - clear screen
 ; quit    - exit session

Learn more about #Script Lisp at: https://sharpscript.net/lisp

";
                                write(usage);
                                goto prompt;
                            }
                                
                            sb.AppendLine(line);

                            var multiLine = interp.GetSymbolValue("multi-line");
                            if (multiLine == null || multiLine is bool b && !b)
                                break;
                                
                            if (line == "") // evaluate on empty new line
                                break;
                        }

                        var lisp = sb.ToString();
                        if (lisp.Trim().Length == 0)
                            continue;

                        string output = null;
                        try
                        {
                            var requestArgs = CreateBasicRequest(remoteUrl);
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

    private Dictionary<string, object> CreateBasicRequest(string remoteUrl)
    {
        var requestArgs = SharpPagesFeatureExtensions.CreateRequestArgs(new Dictionary<string, object> {
            [ScriptConstants.BaseUrl] = $"tcp://{localIp}:{port}/",
            [nameof(IRequest.UrlReferrer)] = remoteUrl,
        });
        return requestArgs;
    }
}