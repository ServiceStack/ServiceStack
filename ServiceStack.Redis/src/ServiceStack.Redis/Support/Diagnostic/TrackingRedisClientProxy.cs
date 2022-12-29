#if !(NETSTANDARD2_0 || NETSTANDARD2_1 || NET6_0)
using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using ServiceStack.Logging;

namespace ServiceStack.Redis.Support.Diagnostic
{
    /// <summary>
    /// Dynamically proxies access to the IRedisClient providing events for before &amp; after each method invocation 
    /// </summary>
    public class TrackingRedisClientProxy : System.Runtime.Remoting.Proxies.RealProxy
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TrackingRedisClientProxy));

        private readonly IRedisClient redisClientInstance;
        private readonly Guid id;

        public TrackingRedisClientProxy(IRedisClient instance, Guid id)
            : base(typeof(IRedisClient))
        {
            this.redisClientInstance = instance;
            this.id = id;
        }

        public event EventHandler<InvokeEventArgs> BeforeInvoke;
        public event EventHandler<InvokeEventArgs> AfterInvoke;

        public override IMessage Invoke(IMessage msg)
        {
            // Thanks: http://stackoverflow.com/a/15734124/211978

            var methodCall = (IMethodCallMessage)msg;
            var method = (MethodInfo)methodCall.MethodBase;

            try
            {
                if (this.BeforeInvoke != null)
                {
                    this.BeforeInvoke(this, new InvokeEventArgs(method));
                }
                var result = method.Invoke(this.redisClientInstance, methodCall.InArgs);
                if (this.AfterInvoke != null)
                {
                    this.AfterInvoke(this, new InvokeEventArgs(method));
                }

                return new ReturnMessage(result, null, 0, methodCall.LogicalCallContext, methodCall);
            }
            catch (TargetInvocationException e)
            {
                Logger.Error("Reflection exception when invoking target method", e);
                return new ReturnMessage(e.InnerException, msg as IMethodCallMessage);
            }
        }
    }
}
#endif