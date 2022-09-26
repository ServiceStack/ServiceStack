using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public interface ICrudEvents
    {
        /// <summary>
        /// Record an AutoCrudEvent Sync
        /// </summary>
        void Record(CrudContext context);

        /// <summary>
        /// Record an AutoCrudEvent Async
        /// </summary>
        Task RecordAsync(CrudContext context);
    }
    
    public interface ICrudEventsExecutor<in T>
        where T : CrudEvent
    {
        Task ExecuteAsync(T crudEvent);
    }
    
    public abstract class CrudEventsBase<T>
        where T : CrudEvent
    {
        public Func<object, string> Serializer { get; set; } = JsonSerializer.SerializeToString;
        public Func<string, string> IpMask { get; set; } = CrudEventsUtils.Identity;
        public Func<T, CrudContext, T> EventFilter { get; set; }
        
        public virtual T ToEvent(CrudContext context)
        {
            var urnValue = context.Id?.ToString() ?? context.Operation;
            if (urnValue.IndexOf(':') >= 0)
                urnValue = urnValue.Replace(":", "%3A");

            var userSession = context.Request.GetSession();
            if (userSession?.IsAuthenticated != true)
                userSession = null;

            var to = typeof(T).CreateInstance<T>();
            to.Urn = $"urn:{context.ModelType.Name}:{urnValue}";
            to.EventType = context.Operation;
            to.Model = context.ModelType.Name;
            to.ModelId = context.Id?.ToString();
            to.EventDate = DateTime.UtcNow;
            to.RowsUpdated = context.RowsUpdated;
            to.RequestType = context.Dto.GetType().Name;
            to.RequestBody = Serializer?.Invoke(context.Dto);
            to.UserAuthId = userSession?.UserAuthId;
            to.UserAuthName = userSession?.GetUserAuthName();
            to.RemoteIp = IpMask(context.Request.RemoteIp);
            return to;
        }
    }

    public class CrudEventsExecutor : CrudEventsExecutor<CrudEvent>
    {
        public CrudEventsExecutor(IAppHost appHost) : base(appHost) { }
        public CrudEventsExecutor(IServiceExecutor serviceExecutor, Func<string, Type> typeResolver) : base(serviceExecutor, typeResolver) { }
    }
    
    public class CrudEventsExecutor<T> : ICrudEventsExecutor<T> 
        where T : CrudEvent 
    {
        public IServiceExecutor ServiceExecutor { get; set; }
        public Func<string, Type> TypeResolver { get; set; }

        public Func<object, IRequest, bool> ExecuteFilter { get; set; }
        
        public List<Action<IRequest, IResponse, object>> RequestFilters { get; } = 
            new List<Action<IRequest, IResponse, object>>();
        
        List<Func<IRequest, IResponse, object, Task>> RequestFiltersAsync { get; } =
            new List<Func<IRequest, IResponse, object, Task>>();

        public CrudEventsExecutor(IAppHost appHost)
            : this(appHost.ServiceController, appHost.Metadata.GetOperationType)
        {
            RequestFilters = appHost.GlobalMessageRequestFilters;
            RequestFiltersAsync = appHost.GlobalMessageRequestFiltersAsync;
        }

        public CrudEventsExecutor(IServiceExecutor serviceExecutor, Func<string, Type> typeResolver)
        {
            ServiceExecutor = serviceExecutor ?? throw new ArgumentNullException(nameof(serviceExecutor));
            TypeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
        }
        
        /// <summary>
        /// (RequestDto, HttpMethod) => IRequest
        /// </summary>
        public Func<object, string, IRequest> RequestFactory { get; set; } = (dto,method) => new BasicRequest(dto, RequestAttributes.LocalSubnet) {
            Verb = method
        };
        
        public Func<string, Type, object> Deserializer { get; set; } = JsonSerializer.DeserializeFromString;

        public virtual async Task ExecuteAsync(T crudEvent)
        {
            var typeName = crudEvent.RequestType ?? throw new ArgumentNullException(nameof(crudEvent.RequestType));
            var requestType = TypeResolver(typeName) ?? throw new TypeLoadException($"'{typeName}' was not found");
            if (crudEvent.RequestBody == null)
                throw new ArgumentNullException(nameof(crudEvent.RequestBody));

            var requestDto = Deserializer(crudEvent.RequestBody, requestType);
            var method = AutoCrudOperation.ToHttpMethod(crudEvent.EventType);
            var req = RequestFactory(requestDto, method);

            req.SetInProcessRequest();
            if (crudEvent.RemoteIp != null && req is BasicRequest basicRequest)
                basicRequest.RemoteIp = crudEvent.RemoteIp;

            if (crudEvent.UserAuthId != null)
            {
                var result = await req.GetSessionFromSourceAsync(crudEvent.UserAuthId, validator:null).ConfigAwait(); 
                if (result == null)
                    throw new NotSupportedException("An AuthRepository or IUserSessionSource is required Execute Authenticated AutoCrudEvents");

                var session = result.Session;

                if (session.UserAuthName == null)
                    session.UserAuthName = session.UserName ?? session.Email;
                if (session.Roles == null)
                    session.Roles = result.Roles?.ToList();
                if (session.Permissions == null)
                    session.Permissions = result.Permissions?.ToList();

                req.Items[Keywords.Session] = session;
            }

            req.Items[Keywords.IgnoreEvent] = bool.TrueString; //don't record AutoCrudEvent

            if (crudEvent.ModelId != null)
                req.Items[Keywords.EventModelId] = crudEvent.ModelId;

            if (RequestFilters.Count > 0)
            {
                foreach (var requestFilter in RequestFilters)
                {
                    requestFilter(req, req.Response, req.Dto);
                    
                    if (req.Response.IsClosed)
                        throw new UnauthorizedAccessException($"RequestFilters short-circuited request denying executing {typeof(T).Name} {crudEvent.Id}");
                }
            }

            if (RequestFiltersAsync.Count > 0)
            {
                foreach (var requestFilter in RequestFiltersAsync)
                {
                    await requestFilter(req, req.Response, req.Dto).ConfigAwait();
                    
                    if (req.Response.IsClosed)
                        throw new UnauthorizedAccessException($"RequestFiltersAsync short-circuited request denying executing {typeof(T).Name} {crudEvent.Id}");
                }
            }
            
            await ServiceExecutor.ExecuteAsync(requestDto, req).ConfigAwait();
        }
    }

    public class OrmLiteCrudEvents : OrmLiteCrudEvents<CrudEvent>
    {
        public static int BatchSize { get; set; } = 1000;
        public OrmLiteCrudEvents(IDbConnectionFactory dbFactory) : base(dbFactory) { }
    }

    public class OrmLiteCrudEvents<T> : CrudEventsBase<T>, 
        ICrudEvents, IRequiresSchema, IClearable
        where T : CrudEvent
    {
        
        /// <summary>
        /// Don't persist CrudEvent's in primary IDbConnectionFactory
        /// </summary>
        public bool ExcludePrimaryDb { get; set; }
        
        /// <summary>
        /// Additional DB Connections CrudEvent's should be persisted in
        /// </summary>
        public List<string> NamedConnections { get; } = new();

        private IDbConnectionFactory DbFactory { get; }
        public OrmLiteCrudEvents(IDbConnectionFactory dbFactory) => DbFactory = dbFactory;

        public bool ShouldRecord(CrudContext context) => context.NamedConnection != null
            ? NamedConnections.Contains(context.NamedConnection)
            : !ExcludePrimaryDb;

        /// <summary>
        /// Record an CrudEvent Sync
        /// </summary>
        public virtual void Record(CrudContext context)
        {
            if (!ShouldRecord(context))
                return;
            
            var row = ToEvent(context);
            if (EventFilter != null)
            {
                row = EventFilter(row, context);
                if (row == null)
                    return;
            }
            context.Db.Insert(row);
        }

        /// <summary>
        /// Record an CrudEvent Async
        /// </summary>
        public virtual Task RecordAsync(CrudContext context)
        {
            if (!ShouldRecord(context))
                return Task.CompletedTask;
            
            var row = ToEvent(context);
            if (EventFilter != null)
            {
                row = EventFilter(row, context);
                if (row == null)
                    return TypeConstants.EmptyTask;
            }
            return context.Db.InsertAsync(row);
        }

        /// <summary>
        /// Returns all rows in CrudEvent Table, lazily paging in batches of OrmLiteCrudEvents.BatchSize
        /// </summary>
        public virtual IEnumerable<T> GetEvents(IDbConnection db)
        {
            List<T> results;
            long lastId = 0;
            do
            {
                var q = db.From<T>()
                    .Take(OrmLiteCrudEvents.BatchSize)
                    .OrderBy(x => x.Id);

                if (lastId != default)
                    q.Where(x => x.Id > lastId);

                results = db.Select(q);
                foreach (var result in results)
                {
                    lastId = result.Id;
                    yield return result;
                }
            } while (results.Count > 0);
        }

        public virtual IEnumerable<T> GetEvents(IDbConnection db, string table, string id=null)
        {
            var q = db.From<T>()
                .Where(x => x.Model == table);
            if (id != null)
            {
                q.And(x =>  x.ModelId == id);
            }
            q.OrderBy(x => x.Id);
            return db.Select(q);
        }

        /// <summary>
        /// Create CrudEvent if it doesn't already exist
        /// </summary>
        public virtual void InitSchema()
        {
            if (!ExcludePrimaryDb)
            {
                using var db = DbFactory.OpenDbConnection();
                db.CreateTableIfNotExists<T>();
            }
            foreach (var namedConnection in NamedConnections)
            {
                using var db = DbFactory.OpenDbConnection(namedConnection);
                db.CreateTableIfNotExists<T>();
            }
        }

        /// <summary>
        /// Delete all entries in CrudEvent Table
        /// </summary>
        public virtual void Clear()
        {
            if (!ExcludePrimaryDb)
            {
                using var db = DbFactory.OpenDbConnection();
                db.DeleteAll<T>();
            }
            foreach (var namedConnection in NamedConnections)
            {
                using var db = DbFactory.OpenDbConnection(namedConnection);
                db.DeleteAll<T>();
            }
        }

        /// <summary>
        /// WARNING: DROP and RE-CREATE CrudEvent
        /// </summary>
        /// <returns></returns>
        public virtual OrmLiteCrudEvents<T> Reset()
        {
            if (!ExcludePrimaryDb)
            {
                using var db = DbFactory.OpenDbConnection();
                db.DropAndCreateTable<T>();
            }
            foreach (var namedConnection in NamedConnections)
            {
                using var db = DbFactory.OpenDbConnection(namedConnection);
                db.DropAndCreateTable<T>();
            }
            return this;
        }
    }

    public static class CrudEventsUtils
    {
        /// <summary>
        /// Returns null
        /// </summary>
        public static string Null(string value) => null;
        /// <summary>
        /// Returns itself as-is
        /// </summary>
        public static string Identity(string value) => value;

        /// <summary>
        /// Returns Single IP with empty last segment 
        /// </summary>
        public static string AnonymizeLastIpSegment(string remoteIp)
        {
            if (string.IsNullOrEmpty(remoteIp))
                return remoteIp;

            if (remoteIp.IndexOf(',') >= 0)
                remoteIp = remoteIp.LeftPart(','); // take 1st of multiple IPs

            if (remoteIp == "::1")
                return remoteIp;

            if (remoteIp.IndexOf('.') >= 0)
                return remoteIp.LastLeftPart('.') + ".0";
            
            if (remoteIp.IndexOf(':') >= 0)
                return remoteIp.LastLeftPart(':') + ":0000";

            return remoteIp;
        }
        
        public static void InitSchema(this ICrudEvents events)
        {
            if (events is IRequiresSchema requiresSchema)
            {
                requiresSchema.InitSchema();
            }
        }

        public static void Clear(this ICrudEvents events)
        {
            if (events is IClearable clearable)
            {
                clearable.Clear();
            }
            else throw new NotSupportedException($"{events.GetType().Name} does not implement IClearable");
        }
    }
}