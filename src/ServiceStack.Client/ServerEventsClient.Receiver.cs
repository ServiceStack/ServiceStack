using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack
{
    public delegate void ServerEventCallback(ServerEventsClient source, ServerEventMessage args);

    public class ServerEventReceiver : IReceiver
    {
        public static ILog Log = LogManager.GetLogger(typeof(ServerEventReceiver));

        public ServerEventsClient Client { get; set; }
        public ServerEventMessage Request { get; set; }

        public virtual void NoSuchMethod(string selector, object message)
        {
            if (Log.IsDebugEnabled)
                Log.Debug($"NoSuchMethod defined for {selector}");
        }
    }

    public class NewInstanceResolver : IResolver
    {
        public T TryResolve<T>()
        {
            return typeof(T).CreateInstance<T>();
        }
    }

    public class SingletonInstanceResolver : IResolver
    {
        readonly ConcurrentDictionary<Type, object> Cache = new ConcurrentDictionary<Type, object>();

        public T TryResolve<T>()
        {
            return (T) Cache.GetOrAdd(typeof (T), type => type.CreateInstance<T>());
        }
    }

    public partial class ServerEventsClient
    {
        public IResolver Resolver { get; set; }

        public Dictionary<string, ServerEventCallback> Handlers { get; set; }
        public Dictionary<string, ServerEventCallback> NamedReceivers { get; set; }
        public List<Type> ReceiverTypes { get; set; } 

        public ServerEventsClient RegisterReceiver<T>()
            where T : IReceiver
        {
            return RegisterNamedReceiver<T>("cmd");
        }

        public ServerEventsClient RegisterNamedReceiver<T>(string receiverName)
            where T : IReceiver
        {
            ReceiverExec<T>.Reset();

            NamedReceivers[receiverName] = CreateReceiverHandler<T>;

            ReceiverTypes.Add(typeof(T));

            return this;
        }

        private void CreateReceiverHandler<T>(ServerEventsClient client, ServerEventMessage msg)
        {
            var receiver = Resolver.TryResolve<T>() as IReceiver;
            if (receiver == null)
                throw new ArgumentNullException("receiver", "Resolver returned null receiver");

            var injectRecevier = receiver as ServerEventReceiver;
            if (injectRecevier != null)
            {
                injectRecevier.Client = client;
                injectRecevier.Request = msg;
            }

            var target = msg.Target.Replace("-", ""); //css bg-image

            ReceiverExecContext receiverCtx;
            ReceiverExec<T>.RequestTypeExecMap.TryGetValue(target, out receiverCtx);

            if (receiverCtx == null)
                ReceiverExec<T>.MethodNameExecMap.TryGetValue(target, out receiverCtx);

            if (receiverCtx == null)
            {
                receiver.NoSuchMethod(msg.Target, msg);
                return;
            }

            object requestDto;
            try
            {
                requestDto = string.IsNullOrEmpty(msg.Json)
                    ? receiverCtx.RequestType.CreateInstance()
                    : JsonSerializer.DeserializeFromString(msg.Json, receiverCtx.RequestType);
            }
            catch (Exception ex)
            {
                throw new SerializationException($"Could not deserialize into '{typeof(T).Name}' from '{msg.Json}'", ex);
            }


            receiverCtx.Exec(receiver, requestDto);
        }
    }

    internal delegate object ActionInvokerFn(object intance, object request);
    internal delegate void VoidActionInvokerFn(object intance, object request);

    internal class ReceiverExecContext
    {
        public const string AnyAction = "ANY";
        public string Id { get; set; }
        public Type RequestType { get; set; }
        public Type ServiceType { get; set; }
        public ActionInvokerFn Exec { get; set; }

        public static string Key(string method, string requestDtoName)
        {
            return method.ToUpper() + " " + requestDtoName;
        }

        public static string AnyKey(string requestDtoName)
        {
            return AnyAction + " " + requestDtoName;
        }
    }

    internal class ReceiverExec<T>
    {
        internal static Dictionary<string, ReceiverExecContext> RequestTypeExecMap;
        internal static Dictionary<string, ReceiverExecContext> MethodNameExecMap;

        public static void Reset()
        {
            RequestTypeExecMap = new Dictionary<string, ReceiverExecContext>(StringComparer.OrdinalIgnoreCase);
            MethodNameExecMap = new Dictionary<string, ReceiverExecContext>(StringComparer.OrdinalIgnoreCase);

            var methods = typeof(T).GetMethodInfos().Where(x => x.IsPublic && !x.IsStatic);
            foreach (var mi in methods)
            {
                var actionName = mi.Name;
                var args = mi.GetParameters();
                if (args.Length != 1) 
                    continue;
                if (mi.Name == "Equals")
                    continue;

                if (actionName.StartsWith("set_"))
                    actionName = actionName.Substring("set_".Length);

                var requestType = args[0].ParameterType;
                var execCtx = new ReceiverExecContext
                {
                    Id = ReceiverExecContext.Key(actionName, requestType.GetOperationName()),
                    ServiceType = typeof(T),
                    RequestType = requestType,
                };

                try
                {
                    execCtx.Exec = CreateExecFn(requestType, mi);
                }
                catch
                {
                    //Potential problems with MONO, using reflection for fallback
                    execCtx.Exec = (receiver, request) =>
                        mi.Invoke(receiver, new[] { request });
                }

                RequestTypeExecMap[requestType.Name] = execCtx;
                MethodNameExecMap[actionName] = execCtx;
            }
        }

        private static ActionInvokerFn CreateExecFn(Type requestType, MethodInfo mi)
        {
            //TODO optimize for PCL clients
#if PCL
            return (instance, request) =>
                mi.Invoke(instance, new[] { request });
#else
            var receiverType = typeof(T);

            var receiverParam = Expression.Parameter(typeof(object), "receiverObj");
            var receiverStrong = Expression.Convert(receiverParam, receiverType);

            var requestDtoParam = Expression.Parameter(typeof(object), "requestDto");
            var requestDtoStrong = Expression.Convert(requestDtoParam, requestType);

            Expression callExecute = Expression.Call(
            receiverStrong, mi, requestDtoStrong);

            if (mi.ReturnType != typeof(void))
            {
                var executeFunc = Expression.Lambda<ActionInvokerFn>
                (callExecute, receiverParam, requestDtoParam).Compile();

                return executeFunc;
            }
            else
            {
                var executeFunc = Expression.Lambda<VoidActionInvokerFn>
                (callExecute, receiverParam, requestDtoParam).Compile();

                return (service, request) =>
                {
                    executeFunc(service, request);
                    return null;
                };
            }
#endif
        }
    }

}