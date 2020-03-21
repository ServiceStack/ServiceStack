using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack
{
    public abstract class AutoCrudEventsBase<Table>
        where Table : AutoCrudEvent
    {
        public Func<object, string> Serializer { get; set; } = JsonSerializer.SerializeToString;
        public Func<Table, AutoCrudContext, Table> EventFilter { get; set; }
        public Table ToEvent(AutoCrudContext context)
        {
            var urnValue = context.Id?.ToString() ?? context.Operation;
            if (urnValue.IndexOf(':') >= 0)
                urnValue = urnValue.Replace(":", "%3A");

            var userSession = context.Request.GetSession();
            if (userSession?.IsAuthenticated != true)
                userSession = null;

            var to = typeof(Table).CreateInstance<Table>();
            to.Urn = $"urn:{context.ModelType.Name}:{urnValue}";
            to.Operation = context.Operation;
            to.EventModel = context.ModelType.Name;
            to.EventId = context.Id?.ToString();
            to.EventDate = DateTime.UtcNow;
            to.RowsUpdated = context.RowsUpdated;
            to.RequestType = context.Dto.GetType().Name;
            to.RequestBody = Serializer?.Invoke(context.Dto);
            to.UserAuthId = userSession?.UserAuthId;
            to.UserAuthName = userSession?.GetUserAuthName();
            to.RemoteIp = context.Request.RemoteIp;
            return to;
        }
    }

    public interface IAutoCrudEvents
    {
        void Record(AutoCrudContext context);
        Task RecordAsync(AutoCrudContext context);
    }

    public class AutoCrudEvent
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Urn { get; set; }
        public string Operation { get; set; }
        public string EventModel { get; set; }
        public string EventId { get; set; }
        public DateTime EventDate { get; set; } //UTC
        public long? RowsUpdated { get; set; }
        public string RequestType { get; set; }
        public string RequestBody { get; set; }
        public string UserAuthId { get; set; }
        public string UserAuthName { get; set; }
        public string RemoteIp { get; set; }
    }

    public class OrmLiteAutoCrudEvents : OrmLiteAutoCrudEvents<AutoCrudEvent>
    {
        public OrmLiteAutoCrudEvents(IDbConnectionFactory dbFactory) : base(dbFactory) { }
    }

    public class OrmLiteAutoCrudEvents<Table> : AutoCrudEventsBase<Table>, IAutoCrudEvents, IRequiresSchema, IClearable
        where Table : AutoCrudEvent
    {
        public bool OnlyNamedConnections { get; set; }
        public List<string> NamedConnections { get; } = new List<string>();
        private IDbConnectionFactory DbFactory { get; }
        public OrmLiteAutoCrudEvents(IDbConnectionFactory dbFactory) => DbFactory = dbFactory;
        
        public void Record(AutoCrudContext context)
        {
            var row = ToEvent(context);
            if (EventFilter != null)
            {
                row = EventFilter(row, context);
                if (row == null)
                    return;
            }
            context.Db.Insert(row);
        }

        public Task RecordAsync(AutoCrudContext context)
        {
            var row = ToEvent(context);
            if (EventFilter != null)
            {
                row = EventFilter(row, context);
                if (row == null)
                    return TypeConstants.EmptyTask;
            }
            return context.Db.InsertAsync(row);
        }

        public void InitSchema()
        {
            if (!OnlyNamedConnections)
            {
                using (var db = DbFactory.OpenDbConnection())
                {
                    db.CreateTableIfNotExists<Table>();
                }
            }
            foreach (var namedConnection in NamedConnections)
            {
                using (var db = DbFactory.OpenDbConnection(namedConnection))
                {
                    db.CreateTableIfNotExists<Table>();
                }
            }
        }

        public void Clear()
        {
            if (!OnlyNamedConnections)
            {
                using (var db = DbFactory.OpenDbConnection())
                {
                    db.DeleteAll<Table>();
                }
            }
            foreach (var namedConnection in NamedConnections)
            {
                using (var db = DbFactory.OpenDbConnection(namedConnection))
                {
                    db.DeleteAll<Table>();
                }
            }
        }
    }

    public static class AutoCrudEventsUtils
    {
        public static void InitSchema(this IAutoCrudEvents events)
        {
            if (events is IRequiresSchema requiresSchema)
            {
                requiresSchema.InitSchema();
            }
        }

        public static void Clear(this IAutoCrudEvents events)
        {
            if (events is IClearable clearable)
            {
                clearable.Clear();
            }
            else throw new NotSupportedException($"{events.GetType().Name} does not implement IClearable");
        }
    }
}