#if AUTOQUERY_CRUD
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Extensions.Tests
{
    [DataContract]
    public abstract class RockstarBase
    {
        [DataMember(Order = 1)]
        public string FirstName { get; set; }
        [DataMember(Order = 2)]
        public string LastName { get; set; }
        [DataMember(Order = 3)]
        public int? Age { get; set; }
        [DataMember(Order = 4)]
        public DateTime DateOfBirth { get; set; }
        [DataMember(Order = 5)]
        public DateTime? DateDied { get; set; }
        [DataMember(Order = 6)]
        public LivingStatus LivingStatus { get; set; }
    }

    [Alias(nameof(Rockstar))]
    [DataContract]
    public class RockstarAuto : RockstarBase
    {
        [AutoIncrement]
        [DataMember(Order = 1)]
        public int Id { get; set; }
    }

    [DataContract]
    public class RockstarAutoGuid : RockstarBase
    {
        [AutoId]
        [DataMember(Order = 1)]
        public Guid Id { get; set; }
    }

    [DataContract]
    public class RockstarAudit : RockstarBase
    {
        [AutoIncrement]
        [DataMember(Order = 1)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public DateTime CreatedDate { get; set; }
        [DataMember(Order = 3)]
        public string CreatedBy { get; set; }
        [DataMember(Order = 4)]
        public string CreatedInfo { get; set; }
        [DataMember(Order = 5)]
        public DateTime ModifiedDate { get; set; }
        [DataMember(Order = 6)]
        public string ModifiedBy { get; set; }
        [DataMember(Order = 7)]
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

    [DataContract]
    public abstract class AuditBase : IAudit
    {
        [DataMember(Order = 1)]
        public DateTime CreatedDate { get; set; }

        [Required]
        [DataMember(Order = 2)]
        public string CreatedBy { get; set; }

        [Required]
        [DataMember(Order = 3)]
        public string CreatedInfo { get; set; }

        [DataMember(Order = 4)]
        public DateTime ModifiedDate { get; set; }

        [Required]
        [DataMember(Order = 5)]
        public string ModifiedBy { get; set; }

        [Required]
        [DataMember(Order = 6)]
        public string ModifiedInfo { get; set; }

        [Index] //Check if Deleted
        [DataMember(Order = 7)]
        public DateTime? SoftDeletedDate { get; set; }

        [DataMember(Order = 8)]
        public string SoftDeletedBy { get; set; }
        [DataMember(Order = 9)]
        public string SoftDeletedInfo { get; set; }
    }

    [DataContract]
    public class RockstarAuditTenant : AuditBase
    {
        [Index]
        [DataMember(Order = 1)]
        public int TenantId { get; set; }

        [AutoIncrement]
        [DataMember(Order = 2)]
        public int Id { get; set; }

        [DataMember(Order = 3)]
        public string FirstName { get; set; }
        [DataMember(Order = 4)]
        public string LastName { get; set; }
        [DataMember(Order = 5)]
        public int? Age { get; set; }
        [DataMember(Order = 6)]
        public DateTime DateOfBirth { get; set; }
        [DataMember(Order = 7)]
        public DateTime? DateDied { get; set; }
        [DataMember(Order = 8)]
        public LivingStatus LivingStatus { get; set; }
    }

    [DataContract]
    public class RockstarVersion : RockstarBase
    {
        [AutoIncrement]
        [DataMember(Order = 1)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public ulong RowVersion { get; set; }
    }

    [DataContract]
    public class CreateRockstar : RockstarBase, ICreateDb<RockstarAuto>, IReturn<CreateRockstarResponse> { }

    [DataContract]
    public class CreateRockstarResponse
    {
        [DataMember(Order = 1)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class CreateRockstarWithReturn : RockstarBase, ICreateDb<RockstarAuto>,
        IReturn<RockstarWithIdAndResultResponse> { }

    [DataContract]
    public class CreateRockstarWithVoidReturn : RockstarBase, ICreateDb<RockstarAuto>, IReturnVoid { }

    [DataContract]
    public class CreateRockstarWithAutoGuid : RockstarBase, ICreateDb<RockstarAutoGuid>,
        IReturn<CreateRockstarWithReturnGuidResponse> { }

    [ValidateRequest("IsAuthenticated")]
    [AutoPopulate(nameof(RockstarAudit.CreatedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(RockstarAudit.CreatedBy), Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(RockstarAudit.CreatedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    [AutoPopulate(nameof(RockstarAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(RockstarAudit.ModifiedBy), Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(RockstarAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    [DataContract]
    public class CreateRockstarAudit : RockstarBase, ICreateDb<RockstarAudit>, IReturn<RockstarWithIdResponse> { }

    [ValidateRequest("IsAuthenticated")]
    [AutoPopulate(nameof(IAudit.CreatedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(IAudit.CreatedBy), Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(IAudit.CreatedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    [AutoPopulate(nameof(IAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(IAudit.ModifiedBy), Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(IAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    [DataContract]
    public abstract class CreateAuditBase<Table, TResponse> : ICreateDb<Table>, IReturn<TResponse> { }

    [AutoPopulate(nameof(IAuditTenant.TenantId), Eval = "Request.Items.TenantId")]
    [DataContract]
    public abstract class CreateAuditTenantBase<Table, TResponse> : CreateAuditBase<Table, TResponse> { }

    [ValidateRequest("IsAuthenticated")]
    [AutoPopulate(nameof(IAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(IAudit.ModifiedBy), Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(IAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    [DataContract]
    public abstract class UpdateAuditBase<Table, TResponse> : IUpdateDb<Table>, IReturn<TResponse> { }

    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId), Eval = "Request.Items.TenantId")]
    [DataContract]
    public abstract class UpdateAuditTenantBase<Table, TResponse> : UpdateAuditBase<Table, TResponse> { }

    [ValidateRequest("IsAuthenticated")]
    [AutoPopulate(nameof(IAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(IAudit.ModifiedBy), Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(IAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    [DataContract]
    public abstract class PatchAuditBase<Table, TResponse> : IPatchDb<Table>, IReturn<TResponse> { }

    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId), Eval = "Request.Items.TenantId")]
    [DataContract]
    public abstract class PatchAuditTenantBase<Table, TResponse> : PatchAuditBase<Table, TResponse> { }

    [ValidateRequest("IsAuthenticated")]
    [AutoPopulate(nameof(IAudit.SoftDeletedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(IAudit.SoftDeletedBy), Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(IAudit.SoftDeletedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    [DataContract]
    public abstract class SoftDeleteAuditBase<Table, TResponse> : IUpdateDb<Table>, IReturn<TResponse> { }

    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId), Eval = "Request.Items.TenantId")]
    [DataContract]
    public abstract class SoftDeleteAuditTenantBase<Table, TResponse> : SoftDeleteAuditBase<Table, TResponse> { }

    [ValidateRequest("IsAuthenticated")]
    [AutoFilter(QueryTerm.Ensure, nameof(IAudit.SoftDeletedDate), Template = SqlTemplate.IsNull)]
    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId), Eval = "Request.Items.TenantId")]
    [DataContract]
    public abstract class QueryDbTenant<From, Into> : QueryDb<From, Into> { }

    [DataContract]
    public class CreateRockstarAuditTenant : CreateAuditTenantBase<RockstarAuditTenant, RockstarWithIdAndResultResponse>, IHasBearerToken
    {
        [DataMember(Order = 1)]
        public string BearerToken { get; set; } //Authenticate MQ Requests
        [DataMember(Order = 2)]
        public string FirstName { get; set; }
        [DataMember(Order = 3)]
        public string LastName { get; set; }
        [DataMember(Order = 4)]
        public int? Age { get; set; }
        [DataMember(Order = 5)]
        public DateTime DateOfBirth { get; set; }
        [DataMember(Order = 6)]
        public DateTime? DateDied { get; set; }
        [DataMember(Order = 7)]
        public LivingStatus LivingStatus { get; set; }
    }

    [DataContract]
    public class UpdateRockstarAuditTenant : UpdateAuditTenantBase<RockstarAuditTenant, RockstarWithIdAndResultResponse>, IHasBearerToken
    {
        [DataMember(Order = 1)]
        public string BearerToken { get; set; } //Authenticate MQ Requests
        [DataMember(Order = 2)]
        public int Id { get; set; }
        [DataMember(Order = 3)]
        public string FirstName { get; set; }
        [DataMember(Order = 4)]
        public LivingStatus? LivingStatus { get; set; }
    }

    [DataContract]
    public class PatchRockstarAuditTenant : PatchAuditTenantBase<RockstarAuditTenant, RockstarWithIdAndResultResponse>, IHasBearerToken
    {
        [DataMember(Order = 1)]
        public string BearerToken { get; set; } //Authenticate MQ Requests
        [DataMember(Order = 2)]
        public int Id { get; set; }
        [DataMember(Order = 3)]
        public string FirstName { get; set; }
        [DataMember(Order = 4)]
        public LivingStatus? LivingStatus { get; set; }
    }

    [DataContract]
    public class CreateRockstarAuditTenantGateway : IReturn<RockstarWithIdAndResultResponse>, IPost
    {
        [DataMember(Order = 1)]
        public string FirstName { get; set; }
        [DataMember(Order = 2)]
        public string LastName { get; set; }
        [DataMember(Order = 3)]
        public int? Age { get; set; }
        [DataMember(Order = 4)]
        public DateTime DateOfBirth { get; set; }
        [DataMember(Order = 5)]
        public DateTime? DateDied { get; set; }
        [DataMember(Order = 6)]
        public LivingStatus LivingStatus { get; set; }
    }

    [DataContract]
    public class UpdateRockstarAuditTenantGateway : IReturn<RockstarWithIdAndResultResponse>, IPut
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string FirstName { get; set; }
        [DataMember(Order = 3)]
        public LivingStatus? LivingStatus { get; set; }
    }

    [DataContract]
    public class PatchRockstarAuditTenantGateway : IReturn<RockstarWithIdAndResultResponse>, IPatch
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string FirstName { get; set; }
        [DataMember(Order = 3)]
        public LivingStatus? LivingStatus { get; set; }
    }

    [DataContract]
    public class RealDeleteAuditTenantGateway : IReturn<RockstarWithIdAndCountResponse>, IDelete
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
    }

    [DataContract]
    public class SoftDeleteAuditTenant : SoftDeleteAuditTenantBase<RockstarAuditTenant, RockstarWithIdAndResultResponse>
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
    }

    [Authenticate]
    [DataContract]
    public class CreateRockstarAuditTenantMq : IReturnVoid
    {
        [DataMember(Order = 1)]
        public string FirstName { get; set; }
        [DataMember(Order = 2)]
        public string LastName { get; set; }
        [DataMember(Order = 3)]
        public int? Age { get; set; }
        [DataMember(Order = 4)]
        public DateTime DateOfBirth { get; set; }
        [DataMember(Order = 5)]
        public DateTime? DateDied { get; set; }
        [DataMember(Order = 6)]
        public LivingStatus LivingStatus { get; set; }
    }

    [Authenticate]
    [DataContract]
    public class UpdateRockstarAuditTenantMq : IPut, IReturnVoid
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string FirstName { get; set; }
        [DataMember(Order = 3)]
        public LivingStatus? LivingStatus { get; set; }
    }

    [DataContract]
    public class PatchRockstarAuditTenantMq : IPatch, IReturnVoid
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string FirstName { get; set; }
        [DataMember(Order = 3)]
        public LivingStatus? LivingStatus { get; set; }
    }

    [DataContract]
    public class RealDeleteAuditTenantMq : IDelete, IReturnVoid
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
    }

    [Authenticate]
    [AutoPopulate(nameof(RockstarAudit.CreatedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(RockstarAudit.CreatedBy), Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(RockstarAudit.CreatedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    [AutoPopulate(nameof(RockstarAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(RockstarAudit.ModifiedBy), Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(RockstarAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    [DataContract]
    public class CreateRockstarAuditMqToken : RockstarBase, ICreateDb<RockstarAudit>, IReturn<RockstarWithIdResponse>, IHasBearerToken
    {
        [DataMember(Order = 1)]
        public string BearerToken { get; set; }
    }


    [Authenticate]
    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId), Eval = "Request.Items.TenantId")]
    [DataContract]
    public class RealDeleteAuditTenant : IDeleteDb<RockstarAuditTenant>, IReturn<RockstarWithIdAndCountResponse>, IHasBearerToken
    {
        [DataMember(Order = 1)]
        public string BearerToken { get; set; } //Authenticate MQ Requests
        [DataMember(Order = 2)]
        public int Id { get; set; }
        [DataMember(Order = 3)]
        public int? Age { get; set; }
    }

    [DataContract]
    public class QueryRockstarAudit : QueryDbTenant<RockstarAuditTenant, RockstarAuto>
    {
        [DataMember(Order = 1)]
        public int? Id { get; set; }
    }

    [QueryDb(QueryTerm.Or)]
    [AutoFilter(QueryTerm.Ensure, nameof(AuditBase.SoftDeletedDate), SqlTemplate.IsNull)]
    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId), Eval = "Request.Items.TenantId")]
    [DataContract]
    public class QueryRockstarAuditSubOr : QueryDb<RockstarAuditTenant, RockstarAuto>
    {
        [DataMember(Order = 1)]
        public string FirstNameStartsWith { get; set; }
        [DataMember(Order = 2)]
        public int? AgeOlderThan { get; set; }
    }

    [DataContract]
    public class CreateRockstarVersion : RockstarBase, ICreateDb<RockstarVersion>,
        IReturn<RockstarWithIdAndRowVersionResponse> { }

    [DataContract]
    public class RockstarWithIdResponse
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class RockstarWithIdAndCountResponse
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public int Count { get; set; }
        [DataMember(Order = 3)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class RockstarWithIdAndRowVersionResponse
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public uint RowVersion { get; set; }
        [DataMember(Order = 3)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class RockstarWithIdAndResultResponse
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public RockstarAuto Result { get; set; }
        [DataMember(Order = 3)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class CreateRockstarWithReturnGuidResponse
    {
        [DataMember(Order = 1)]
        public Guid Id { get; set; }
        [DataMember(Order = 2)]
        public RockstarAutoGuid Result { get; set; }
        [DataMember(Order = 3)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class CreateRockstarAdhocNonDefaults : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdAndResultResponse>
    {
        [DataMember(Order = 1)]
        public string FirstName { get; set; }
        [DataMember(Order = 2)]
        public string LastName { get; set; }

        [AutoDefault(Value = 21)]
        [DataMember(Order = 3)]
        public int? Age { get; set; }

        [AutoDefault(Expression = "date(2001,1,1)")]
        [DataMember(Order = 4)]
        public DateTime DateOfBirth { get; set; }

        [AutoDefault(Eval = "utcNow")]
        [DataMember(Order = 5)]
        public DateTime? DateDied { get; set; }

        [AutoDefault(Value = global::ServiceStack.Extensions.Tests.LivingStatus.Dead)]
        [DataMember(Order = 6)]
        public LivingStatus? LivingStatus { get; set; }
    }

    [DataContract]
    public class CreateRockstarAutoMap : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdAndResultResponse>
    {
        [AutoMap(nameof(RockstarAuto.FirstName))]
        [DataMember(Order = 1)]
        public string MapFirstName { get; set; }

        [AutoMap(nameof(RockstarAuto.LastName))]
        [DataMember(Order = 2)]
        public string MapLastName { get; set; }

        [AutoMap(nameof(RockstarAuto.Age))]
        [AutoDefault(Value = 21)]
        [DataMember(Order = 3)]
        public int? MapAge { get; set; }

        [AutoMap(nameof(RockstarAuto.DateOfBirth))]
        [AutoDefault(Expression = "date(2001,1,1)")]
        [DataMember(Order = 4)]
        public DateTime MapDateOfBirth { get; set; }

        [AutoMap(nameof(RockstarAuto.DateDied))]
        [AutoDefault(Eval = "utcNow")]
        [DataMember(Order = 5)]
        public DateTime? MapDateDied { get; set; }

        [AutoMap(nameof(RockstarAuto.LivingStatus))]
        [AutoDefault(Value = LivingStatus.Dead)]
        [DataMember(Order = 6)]
        public LivingStatus? MapLivingStatus { get; set; }
    }

    [DataContract]
    public class UpdateRockstar : RockstarBase, IUpdateDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
    }

    [Authenticate]
    [AutoPopulate(nameof(RockstarAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(RockstarAudit.ModifiedBy), Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(RockstarAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    [DataContract]
    public class UpdateRockstarAudit : RockstarBase, IPatchDb<RockstarAudit>, IReturn<EmptyResponse>
    {
        [DataMember(Order = 1)]
        // [DataMember(Order = 11)]
        public int Id { get; set; }
        
        // [DataMember(Order = 2)]
        // [DataMember(Order = 12)]
        // public new string FirstName { get; set; }
        
        //1. Commenting out property resolves issue
        //2. When using 1,2 index throws Grpc.Core.RpcException: Status(StatusCode=Unknown, Detail="Exception was thrown by handler.")
        //3. When Index changed to 11,12 causes empty DTO to be sent
        // [DataMember(Order = 13)]
        // public new LivingStatus? LivingStatus { get; set; } //overridden property
    }

    [Authenticate]
    [DataContract]
    public class DeleteRockstarAudit : IDeleteDb<RockstarAudit>, IReturn<RockstarWithIdAndCountResponse>
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
    }

    [DataContract]
    public class UpdateRockstarVersion : RockstarBase, IPatchDb<RockstarVersion>,
        IReturn<RockstarWithIdAndRowVersionResponse>
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public ulong RowVersion { get; set; }
    }

    [DataContract]
    public class PatchRockstar : IPatchDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        
        [DataMember(Order = 2)]
        public string FirstName { get; set; }
        
        [DataMember(Order = 3)]
        public string LastName { get; set; }
        
        [DataMember(Order = 4)]
        public int? Age { get; set; }
        
        [DataMember(Order = 5)]
        public DateTime? DateOfBirth { get; set; }
        
        [DataMember(Order = 6)]
        public DateTime? DateDied { get; set; }
        
        [DataMember(Order = 7)]
        public LivingStatus? LivingStatus { get; set; }
    }

    [DataContract]
    public class UpdateRockstarAdhocNonDefaults : IUpdateDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }

        [AutoUpdate(AutoUpdateStyle.NonDefaults)]
        [DataMember(Order = 2)]
        public string FirstName { get; set; }

        [DataMember(Order = 3)]
        public string LastName { get; set; }

        [AutoDefault(Value = 21)]
        [DataMember(Order = 4)]
        public int? Age { get; set; }

        [AutoDefault(Expression = "date(2001,1,1)")]
        [DataMember(Order = 5)]
        public DateTime DateOfBirth { get; set; }

        [AutoDefault(Eval = "utcNow")]
        [DataMember(Order = 6)]
        public DateTime? DateDied { get; set; }

        [AutoUpdate(AutoUpdateStyle.NonDefaults), AutoDefault(Value = Tests.LivingStatus.Dead)]
        [DataMember(Order = 7)]
        public LivingStatus? LivingStatus { get; set; }
    }

    [DataContract]
    public class DeleteRockstar : IDeleteDb<Rockstar>, IReturn<EmptyResponse>
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
    }

    [DataContract]
    public class DeleteRockstarFilters : IDeleteDb<Rockstar>, IReturn<DeleteRockstarCountResponse>
    {
        [DataMember(Order = 1)]
        public string FirstName { get; set; }
        [DataMember(Order = 2)]
        public string LastName { get; set; }
        [DataMember(Order = 3)]
        public int? Age { get; set; }
    }

    [DataContract]
    public class DeleteRockstarCountResponse
    {
        [DataMember(Order = 1)]
        public int Count { get; set; }
        [DataMember(Order = 2)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class CreateNamedRockstar : RockstarBase, ICreateDb<NamedRockstar>, IReturn<RockstarWithIdAndResultResponse>
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
    }

    [DataContract]
    public class UpdateNamedRockstar : RockstarBase, IUpdateDb<NamedRockstar>, IReturn<RockstarWithIdAndResultResponse>
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
    }

    //[ConnectionInfo] on AutoCrudConnectionInfoServices
    [DataContract]
    public class CreateConnectionInfoRockstar : RockstarBase, ICreateDb<NamedRockstar>,
        IReturn<RockstarWithIdAndResultResponse>
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
    }

    [DataContract]
    public class UpdateConnectionInfoRockstar : RockstarBase, IUpdateDb<NamedRockstar>,
        IReturn<RockstarWithIdAndResultResponse>
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
    }
    

    [DataContract]
    public class Booking : ServiceStack.AuditBase
    {
        [AutoIncrement]
        [DataMember(Order = 1)] public int Id { get; set; }
        [DataMember(Order = 2)] public RoomType RoomType { get; set; }
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
}
#endif