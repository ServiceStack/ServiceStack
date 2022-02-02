using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace ServiceStack.WebHost.Endpoints.Tests 
{
   public abstract class RockstarBase
   {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateDied { get; set; }
        public LivingStatus LivingStatus { get; set; }
    }
    
    [Alias(nameof(Rockstar))]
    public class RockstarAuto : RockstarBase
    {
        [AutoIncrement]
        public int Id { get; set; }
    }
    
    public class RockstarAutoGuid : RockstarBase
    {
        [AutoId]
        public Guid Id { get; set; }
    }
    
    public class RockstarAudit : RockstarBase
    {
        [AutoIncrement]
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedInfo { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedInfo { get; set; }
    }

    public interface IAudit 
    {
        DateTime CreatedDate { get; set; }
        string CreatedBy { get; set; }
        string CreatedInfo { get; set; }
        DateTime ModifiedDate { get; set; }
        string ModifiedBy { get; set; }
        string ModifiedInfo { get; set; }
        DateTime? SoftDeletedDate { get; set; }
        string SoftDeletedBy { get; set; }
        string SoftDeletedInfo { get; set; }
    }

    public interface IAuditTenant : IAudit
    {
        int TenantId { get; set; }
    }

    public abstract class AuditBase : IAudit
    {
        public DateTime CreatedDate { get; set; }
        [Required]
        public string CreatedBy { get; set; }
        [Required]
        public string CreatedInfo { get; set; }

        public DateTime ModifiedDate { get; set; }
        [Required]
        public string ModifiedBy { get; set; }
        [Required]
        public string ModifiedInfo { get; set; }

        [Index] //Check if Deleted
        public DateTime? SoftDeletedDate { get; set; }
        public string SoftDeletedBy { get; set; }
        public string SoftDeletedInfo { get; set; }
    }
        
    public class RockstarAuditTenant : AuditBase
    {
        [Index]
        public int TenantId { get; set; }
        [AutoIncrement]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateDied { get; set; }
        public LivingStatus LivingStatus { get; set; }
    }

    public class RockstarVersion : RockstarBase
    {
        [AutoIncrement]
        public int Id { get; set; }
        public ulong RowVersion { get; set; }
    }
    
    public class CreateRockstarWithId : Rockstar, ICreateDb<Rockstar>, IReturn<RockstarWithIdAndResultResponse>
    {
    }

    public class CreateRockstar : RockstarBase, ICreateDb<RockstarAuto>, IReturn<CreateRockstarResponse>
    {
    }

    public class CreateRockstarResponse
    {
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class CreateRockstarWithReturn : RockstarBase, ICreateDb<RockstarAuto>, IReturn<RockstarWithIdAndResultResponse>
    {
    }
    public class CreateRockstarWithVoidReturn : RockstarBase, ICreateDb<RockstarAuto>, IReturnVoid
    {
    }

    public class CreateRockstarWithAutoGuid : RockstarBase, ICreateDb<RockstarAutoGuid>, IReturn<CreateRockstarWithReturnGuidResponse>
    {
    }

    [ValidateRequest("IsAuthenticated")]
    [AutoPopulate(nameof(RockstarAudit.CreatedDate),  Eval = "utcNow")]
    [AutoPopulate(nameof(RockstarAudit.CreatedBy),    Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(RockstarAudit.CreatedInfo),  Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    [AutoPopulate(nameof(RockstarAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(RockstarAudit.ModifiedBy),   Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(RockstarAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    public class CreateRockstarAudit : RockstarBase, ICreateDb<RockstarAudit>, IReturn<RockstarWithIdResponse>
    {
    }

    [ValidateRequest("IsAuthenticated")]
    [AutoPopulate(nameof(IAudit.CreatedDate),  Eval = "utcNow")]
    [AutoPopulate(nameof(IAudit.CreatedBy),    Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(IAudit.CreatedInfo),  Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    [AutoPopulate(nameof(IAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(IAudit.ModifiedBy),   Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(IAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    public abstract class CreateAuditBase<Table,TResponse> : ICreateDb<Table>, IReturn<TResponse> {}

    [AutoPopulate(nameof(IAuditTenant.TenantId), Eval = "Request.Items.TenantId")]
    public abstract class CreateAuditTenantBase<Table,TResponse> : CreateAuditBase<Table,TResponse> {}

    [ValidateRequest("IsAuthenticated")]
    [AutoPopulate(nameof(IAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(IAudit.ModifiedBy),   Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(IAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    public abstract class UpdateAuditBase<Table,TResponse> : IUpdateDb<Table>, IReturn<TResponse> {}

    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId),  Eval = "Request.Items.TenantId")]
    public abstract class UpdateAuditTenantBase<Table,TResponse> : UpdateAuditBase<Table,TResponse> {}

    [ValidateRequest("IsAuthenticated")]
    [AutoPopulate(nameof(IAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(IAudit.ModifiedBy),   Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(IAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    public abstract class PatchAuditBase<Table,TResponse> : IPatchDb<Table>, IReturn<TResponse> {}

    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId),  Eval = "Request.Items.TenantId")]
    public abstract class PatchAuditTenantBase<Table,TResponse> : PatchAuditBase<Table,TResponse> {}

    [ValidateRequest("IsAuthenticated")]
    [AutoPopulate(nameof(IAudit.SoftDeletedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(IAudit.SoftDeletedBy),   Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(IAudit.SoftDeletedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    public abstract class SoftDeleteAuditBase<Table,TResponse> : IUpdateDb<Table>, IReturn<TResponse> {}
    
    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId),  Eval = "Request.Items.TenantId")]
    public abstract class SoftDeleteAuditTenantBase<Table,TResponse> : SoftDeleteAuditBase<Table,TResponse> {}
    
    [ValidateRequest("IsAuthenticated")]
    [AutoFilter(QueryTerm.Ensure, nameof(IAudit.SoftDeletedDate), Template = SqlTemplate.IsNull)]
    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId),  Eval = "Request.Items.TenantId")]
    public abstract class QueryDbTenant<From, Into> : QueryDb<From, Into> {}

    public class CreateRockstarAuditTenant : CreateAuditTenantBase<RockstarAuditTenant, RockstarWithIdAndResultResponse>, IHasBearerToken
    {
        public string BearerToken { get; set; } //Authenticate MQ Requests
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateDied { get; set; }
        public LivingStatus LivingStatus { get; set; }
    }
    
    public class UpdateRockstarAuditTenant : UpdateAuditTenantBase<RockstarAuditTenant, RockstarWithIdAndResultResponse>, IHasBearerToken
    {
        public string BearerToken { get; set; } //Authenticate MQ Requests
        public int Id { get; set; }
        public string FirstName { get; set; }
        public LivingStatus? LivingStatus { get; set; }
    }
    
    public class PatchRockstarAuditTenant : PatchAuditTenantBase<RockstarAuditTenant, RockstarWithIdAndResultResponse>, IHasBearerToken
    {
        public string BearerToken { get; set; } //Authenticate MQ Requests
        public int Id { get; set; }
        public string FirstName { get; set; }
        public LivingStatus? LivingStatus { get; set; }
    }
    
    public class CreateRockstarAuditTenantGateway : IReturn<RockstarWithIdAndResultResponse>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateDied { get; set; }
        public LivingStatus LivingStatus { get; set; }
    }
    
    public class UpdateRockstarAuditTenantGateway : IReturn<RockstarWithIdAndResultResponse>
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public LivingStatus? LivingStatus { get; set; }
    }
    
    public class PatchRockstarAuditTenantGateway : IReturn<RockstarWithIdAndResultResponse>
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public LivingStatus? LivingStatus { get; set; }
    }
    
    public class RealDeleteAuditTenantGateway : IReturn<RockstarWithIdAndCountResponse>
    {
        public int Id { get; set; }
    }
    
    public class SoftDeleteAuditTenant : SoftDeleteAuditTenantBase<RockstarAuditTenant, RockstarWithIdAndResultResponse>
    {
        public int Id { get; set; }
    }
    
    [Authenticate]
    public class CreateRockstarAuditTenantMq : IReturnVoid
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateDied { get; set; }
        public LivingStatus LivingStatus { get; set; }
    }
    
    [Authenticate]
    public class UpdateRockstarAuditTenantMq : IReturnVoid
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public LivingStatus? LivingStatus { get; set; }
    }

    public class PatchRockstarAuditTenantMq : IReturnVoid
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public LivingStatus? LivingStatus { get; set; }
    }

    public class RealDeleteAuditTenantMq : IReturnVoid
    {
        public int Id { get; set; }
    }
    
    [Authenticate]
    [AutoPopulate(nameof(RockstarAudit.CreatedDate),  Eval = "utcNow")]
    [AutoPopulate(nameof(RockstarAudit.CreatedBy),    Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(RockstarAudit.CreatedInfo),  Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    [AutoPopulate(nameof(RockstarAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(RockstarAudit.ModifiedBy),   Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(RockstarAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    public class CreateRockstarAuditMqToken : RockstarBase, ICreateDb<RockstarAudit>, IReturn<RockstarWithIdResponse>, IHasBearerToken
    {
        public string BearerToken { get; set; }
    }
    
    
    [Authenticate]
    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId),  Eval = "Request.Items.TenantId")]
    public class RealDeleteAuditTenant : IDeleteDb<RockstarAuditTenant>, IReturn<RockstarWithIdAndCountResponse>, IHasBearerToken
    {
        public string BearerToken { get; set; } //Authenticate MQ Requests
        public int Id { get; set; }
        public int? Age { get; set; }
    }

    public class QueryRockstarAudit : QueryDbTenant<RockstarAuditTenant, RockstarAuto>
    {
        public int? Id { get; set; }
    }

    [QueryDb(QueryTerm.Or)]
    [AutoFilter(QueryTerm.Ensure, nameof(AuditBase.SoftDeletedDate), SqlTemplate.IsNull)]
    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId),  Eval = "Request.Items.TenantId")]
    public class QueryRockstarAuditSubOr : QueryDb<RockstarAuditTenant, RockstarAuto>
    {
        public string FirstNameStartsWith { get; set; }
        public int? AgeOlderThan { get; set; }
    }

    public class CreateRockstarVersion : RockstarBase, ICreateDb<RockstarVersion>, IReturn<RockstarWithIdAndRowVersionResponse>
    {
    }
    
    public class RockstarWithIdResponse
    {
        public int Id { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
    public class RockstarWithIdAndCountResponse
    {
        public int Id { get; set; }
        public int Count { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
    
    public class RockstarWithIdAndRowVersionResponse
    {
        public int Id { get; set; }
        public uint RowVersion { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
    
    public class RockstarWithIdAndResultResponse
    {
        public int Id { get; set; }
        public RockstarAuto Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
    
    public class CreateRockstarWithReturnGuidResponse
    {
        public Guid Id { get; set; }
        public RockstarAutoGuid Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class CreateRockstarAdhocNonDefaults : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdAndResultResponse>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [AutoDefault(Value = 21)]
        public int? Age { get; set; }
        [AutoDefault(Expression = "date(2001,1,1)")]
        public DateTime DateOfBirth { get; set; }
        [AutoDefault(Eval = "utcNow")]
        public DateTime? DateDied { get; set; }
        [AutoDefault(Value = global::ServiceStack.WebHost.Endpoints.Tests.LivingStatus.Dead)]
        public LivingStatus? LivingStatus { get; set; }
    }

    public class CreateRockstarAutoMap : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdAndResultResponse>
    {
        [AutoMap(nameof(RockstarAuto.FirstName))]
        public string MapFirstName { get; set; }

        [AutoMap(nameof(RockstarAuto.LastName))]
        public string MapLastName { get; set; }
        
        [AutoMap(nameof(RockstarAuto.Age))]
        [AutoDefault(Value = 21)]
        public int? MapAge { get; set; }
        
        [AutoMap(nameof(RockstarAuto.DateOfBirth))]
        [AutoDefault(Expression = "date(2001,1,1)")]
        public DateTime MapDateOfBirth { get; set; }

        [AutoMap(nameof(RockstarAuto.DateDied))]
        [AutoDefault(Eval = "utcNow")]
        public DateTime? MapDateDied { get; set; }
        
        [AutoMap(nameof(RockstarAuto.LivingStatus))]
        [AutoDefault(Value = LivingStatus.Dead)]
        public LivingStatus? MapLivingStatus { get; set; }
    }

    public class UpdateRockstar : RockstarBase, IUpdateDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
    }

    [Authenticate]
    [AutoPopulate(nameof(RockstarAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(RockstarAudit.ModifiedBy),   Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(RockstarAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    public class UpdateRockstarAudit : RockstarBase, IPatchDb<RockstarAudit>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public LivingStatus? LivingStatus { get; set; }
    }

    [Authenticate]
    public class DeleteRockstarAudit : IDeleteDb<RockstarAudit>, IReturn<RockstarWithIdAndCountResponse>
    {
        public int Id { get; set; }
    }

    public class UpdateRockstarVersion : RockstarBase, IPatchDb<RockstarVersion>, IReturn<RockstarWithIdAndRowVersionResponse>
    {
        public int Id { get; set; }
        public ulong RowVersion { get; set; }
    }
    
    public class PatchRockstar : RockstarBase, IPatchDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
    }

    public class UpdateRockstarAdhocNonDefaults : IUpdateDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
        [AutoUpdate(AutoUpdateStyle.NonDefaults)]
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [AutoDefault(Value = 21)]
        public int? Age { get; set; }
        [AutoDefault(Expression = "date(2001,1,1)")]
        public DateTime DateOfBirth { get; set; }
        [AutoDefault(Eval = "utcNow")]
        public DateTime? DateDied { get; set; }
        [AutoUpdate(AutoUpdateStyle.NonDefaults), AutoDefault(Value = LivingStatus.Dead)]
        public LivingStatus LivingStatus { get; set; }
    }
    
    public class DeleteRockstar : IDeleteDb<Rockstar>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
    }
    
    public class DeleteRockstarFilters : IDeleteDb<Rockstar>, IReturn<DeleteRockstarCountResponse>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
    }

    public class DeleteRockstarCountResponse
    {
        public int Count { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class CreateNamedRockstar : RockstarBase, ICreateDb<NamedRockstar>, IReturn<RockstarWithIdAndResultResponse>
    {
        public int Id { get; set; }
    }

    public class UpdateNamedRockstar : RockstarBase, IUpdateDb<NamedRockstar>, IReturn<RockstarWithIdAndResultResponse>
    {
        public int Id { get; set; }
    }
    
    //[ConnectionInfo] on AutoCrudConnectionInfoServices
    public class CreateConnectionInfoRockstar : RockstarBase, ICreateDb<NamedRockstar>, IReturn<RockstarWithIdAndResultResponse>
    {
        public int Id { get; set; }
    }

    public class UpdateConnectionInfoRockstar : RockstarBase, IUpdateDb<NamedRockstar>, IReturn<RockstarWithIdAndResultResponse>
    {
        public int Id { get; set; }
    }

    public class DefaultValue
    {
        public int Id { get; set; }
        public int Int { get; set; }
        public int? NInt { get; set; }
        public bool Bool { get; set; }
        public bool? NBool { get; set; }
        public string String { get; set; }
    }

    public class CreateDefaultValues : ICreateDb<DefaultValue>, IReturn<IdResponse>
    {
        public int Id { get; set; }
        public int Int { get; set; }
        public int? NInt { get; set; }
        public bool Bool { get; set; }
        public bool? NBool { get; set; }
        public string String { get; set; }
    }

    public class PatchDefaultValues : IPatchDb<DefaultValue>, IReturnVoid
    {
        public int Id { get; set; }
        public int Int { get; set; }
        public int? NInt { get; set; }
        public bool Bool { get; set; }
        public bool? NBool { get; set; }
        public string String { get; set; }
        public string[] Reset { get; set; }
    }

    public class QueryRockstarsUnknownField : QueryDb<Rockstar>
    {
        public int Id { get; set; }
        public string Unknown { get; set; }
    }
    public class CreateRockstarUnknownField : RockstarBase, ICreateDb<RockstarAuto>, IReturn<RockstarWithIdAndResultResponse>
    {
        public string Unknown { get; set; }
    }
    public class UpdateRockstarUnknownField : RockstarBase, IUpdateDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
        public string Unknown { get; set; }
    }
    public class PatchRockstarUnknownField : RockstarBase, IPatchDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
        public string Unknown { get; set; }
    }
    public class DeleteRockstarUnknownField : IDeleteDb<Rockstar>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
        public string Unknown { get; set; }
    }

    [DataContract]
    public class Booking : ServiceStack.AuditBase
    {
        [AutoIncrement]
        [DataMember(Order = 1)] public int Id { get; set; }
        [DataMember(Order = 2)] public RoomType RoomType { get; set; }
        [CheckConstraint("RoomNumber < 500")]
        [DataMember(Order = 3)] public int RoomNumber { get; set; }
        [DataMember(Order = 4)] public DateTime BookingStartDate { get; set; }
        [DataMember(Order = 5)] public DateTime? BookingEndDate { get; set; }
        [DataMember(Order = 6)] public string Notes { get; set; }
        [DataMember(Order = 7)] public bool? Cancelled { get; set; }
        [DataMember(Order = 8)] public decimal Cost { get; set; }
    }

    public enum RoomType
    {
        Single,
        Double,
        Queen,
        Twin,
        Suite,
    }

    [DataContract]
    [AutoApply(Behavior.AuditQuery)]
    public class QueryBookings : QueryDb<Booking>
    {
        [DataMember(Order = 1)] public int[] Ids { get; set; }
    }

    [DataContract]
    [ValidateIsAuthenticated]
    [AutoPopulate(nameof(CreatedBy), Eval = "userAuthName")]
    [AutoApply(Behavior.AuditQuery)]
    public class QueryUserBookings : QueryDb<Booking>
    {
        [DataMember(Order = 1)] public string CreatedBy { get; set; }
    }

    [DataContract]
    [ValidateIsAuthenticated]
    [AutoPopulate(nameof(UserName), Eval = "userAuthName")]
    [AutoApply(Behavior.AuditQuery)]
    public class QueryUserMapBookings : QueryDb<Booking>
    {
        [AutoMap(nameof(ServiceStack.AuditBase.CreatedBy))]
        [DataMember(Order = 1)] public string UserName { get; set; }
    }

    [DataContract]
    [ValidateIsAuthenticated]
    [AutoFilter(QueryTerm.Ensure, nameof(ServiceStack.AuditBase.CreatedBy),  Eval = "userAuthName")]
    [AutoApply(Behavior.AuditQuery)]
    public class QueryEnsureUserBookings : QueryDb<Booking> {}

    [DataContract]
    [ValidateIsAuthenticated]
    [AutoApply(Behavior.AuditCreate)]
    public class CreateBooking
        : ICreateDb<Booking>, IReturn<IdResponse>
    {
        [ApiAllowableValues(typeof(RoomType))]
        [DataMember(Order = 1)] public RoomType RoomType { get; set; }
        [ValidateGreaterThan(0)]
        [DataMember(Order = 2)] public int RoomNumber { get; set; }
        [DataMember(Order = 3)] public DateTime BookingStartDate { get; set; }
        [DataMember(Order = 4)] public DateTime? BookingEndDate { get; set; }
        [DataMember(Order = 5)] public string Notes { get; set; }
        [ValidateGreaterThan(0)]
        [DataMember(Order = 6)] public decimal Cost { get; set; }
    }

    [DataContract]
    [ValidateIsAuthenticated]
    [AutoApply(Behavior.AuditModify)]
    public class UpdateBooking
        : IPatchDb<Booking>, IReturn<IdResponse>
    {
        [DataMember(Order = 1)] public int Id { get; set; }
        [ApiAllowableValues(typeof(RoomType))]
        [DataMember(Order = 2)] public RoomType? RoomType { get; set; }
        [ValidateGreaterThan(0)]
        [DataMember(Order = 3)] public int? RoomNumber { get; set; }
        [DataMember(Order = 4)] public DateTime? BookingStartDate { get; set; }
        [DataMember(Order = 5)] public DateTime? BookingEndDate { get; set; }
        [DataMember(Order = 6)] public string Notes { get; set; }
        [DataMember(Order = 7)] public bool? Cancelled { get; set; }
        [ValidateGreaterThan(0)]
        [DataMember(Order = 8)] public decimal? Cost { get; set; }
    }

    [DataContract]
    [ValidateIsAuthenticated]
    [AutoApply(Behavior.AuditSoftDelete)]
    public class DeleteBooking : IDeleteDb<Booking>, IReturnVoid
    {
        [DataMember(Order = 1)] public int Id { get; set; }
    }
    

    [ValidateIsAuthenticated]
    [AutoApply(Behavior.AuditCreate)]
    public class CustomCreateBooking
        : ICreateDb<Booking>, IReturn<IdResponse>
    {
        [ApiAllowableValues(typeof(RoomType))]
        public RoomType RoomType { get; set; }
        [ValidateGreaterThan(0)]
        public int RoomNumber { get; set; }
        public DateTime BookingStartDate { get; set; }
        public DateTime? BookingEndDate { get; set; }
        public string Notes { get; set; }
        [ValidateGreaterThan(0)]
        public decimal Cost { get; set; }
    }
    
}