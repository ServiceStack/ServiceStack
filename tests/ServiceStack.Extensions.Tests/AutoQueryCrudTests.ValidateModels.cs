#if AUTOQUERY_CRUD
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.FluentValidation;
using ServiceStack.Model;

namespace ServiceStack.Extensions.Tests
{
    public static class ValidationConditions
    {
        public const string IsOdd = "it.isOdd()";
        public const string IsOver2Digits = "it.log10() > 2";
    }

    [DataContract]
    public class ValidateCreateRockstar 
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        [Validate(nameof(ValidateScripts.NotNull))]
        // [Validate("NotNull")]
        [DataMember(Order = 1)]
        public string FirstName { get; set; }
        
        //Added by Fluent Validator 
        [DataMember(Order = 2)]
        public string LastName { get; set; }
        
        // [Validate("[" + nameof(ValidateScripts.NotNull) + "," + nameof(ValidateScripts.Length) + "(13,100)]")] e.g. Typed
        // [Validate("[NotNull,Length(13,100)]")]
        [Validate("NotNull")]
        [Validate("InclusiveBetween(13,100)")]
        [DataMember(Order = 3)]
        public int? Age { get; set; }

        [Validate("NotEmpty(default('DateTime'))")]
        //[Validate("NotEmpty")] equivalent to above thanks to: Validators.AppendDefaultValueOnEmptyValidators
        [DataMember(Order = 4)]
        public DateTime DateOfBirth { get; set; }
        
        [DataMember(Order = 5)]
        public DateTime? DateDied { get; set; }
        
        [DataMember(Order = 6)]
        public LivingStatus LivingStatus { get; set; }
    }

    public class ValidateCreateRockstarValidator : AbstractValidator<ValidateCreateRockstar>
    {
        public ValidateCreateRockstarValidator()
        {
            RuleFor(x => x.LastName).NotNull();
        }
    }

    [AutoPopulate(nameof(LivingStatus), Value = LivingStatus.Alive)]
    [DataContract]
    public class NoAbstractValidator 
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        [Validate("NotNull")]
        [DataMember(Order = 1)]
        public string FirstName { get; set; }
        
        [Validate("NotNull")]
        [DataMember(Order = 2)]
        public string LastName { get; set; }

        [Validate("[NotNull,InclusiveBetween(13,100)]")]
        [DataMember(Order = 3)]
        public int? Age { get; set; }
     
        [Validate("NotEmpty")]
        [DataMember(Order = 4)]
        public DateTime DateOfBirth { get; set; }
     
        [DataMember(Order = 5)]
        public LivingStatus LivingStatus { get; set; }
    }

    [DataContract]
    public class EmptyValidators 
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        // [Validate("NotEmpty(0)")]
        [Validate("NotEmpty")]
        [DataMember(Order = 1)]
        public int Int { get; set; }
        [Validate("NotEmpty")]
        [DataMember(Order = 2)]
        public int? NInt { get; set; }
        [Validate("NotEmpty")]
        // [Validate("NotEmpty(default('System.TimeSpan'))")]
        [DataMember(Order = 3)]
        public TimeSpan TimeSpan { get; set; }
        [Validate("NotEmpty")]
        [DataMember(Order = 4)]
        public TimeSpan? NTimeSpan { get; set; }
        [Validate("NotEmpty")]
        [DataMember(Order = 5)]
        public string String { get; set; }
        [Validate("NotEmpty")]
        [DataMember(Order = 6)]
        public int[] IntArray { get; set; }
        [Validate("NotEmpty")]
        [DataMember(Order = 7)]
        public List<string> StringList { get; set; }
    }

    [DataContract]
    public class TriggerAllValidators 
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        [Validate("CreditCard")]
        [DataMember(Order = 1)]
        public string CreditCard { get; set; }
        [Validate("Email")]
        [DataMember(Order = 2)]
        public string Email { get; set; }
        [Validate("Empty")]
        [DataMember(Order = 3)]
        public string Empty { get; set; }
        [Validate("Equal('Equal')")]
        [DataMember(Order = 4)]
        public string Equal { get; set; }
        [Validate("ExclusiveBetween(10, 20)")]
        [DataMember(Order = 5)]
        public int ExclusiveBetween { get; set; }
        [Validate("GreaterThanOrEqual(10)")]
        [DataMember(Order = 6)]
        public int GreaterThanOrEqual { get; set; }
        [Validate("GreaterThan(10)")]
        [DataMember(Order = 7)]
        public int GreaterThan { get; set; }
        [Validate("InclusiveBetween(10, 20)")]
        [DataMember(Order = 8)]
        public int InclusiveBetween { get; set; }
        [Validate("ExactLength(10)")]
        [DataMember(Order = 9)]
        public string Length { get; set; }
        [Validate("LessThanOrEqual(10)")]
        [DataMember(Order = 10)]
        public int LessThanOrEqual { get; set; }
        [Validate("LessThan(10)")]
        [DataMember(Order = 11)]
        public int LessThan { get; set; }
        [Validate("NotEmpty")]
        [DataMember(Order = 12)]
        public string NotEmpty { get; set; }
        [Validate("NotEqual('NotEqual')")]
        [DataMember(Order = 13)]
        public string NotEqual { get; set; }
        [Validate("Null")]
        [DataMember(Order = 14)]
        public string Null { get; set; }
        [Validate("RegularExpression('^[a-z]*$')")]
        [DataMember(Order = 15)]
        public string RegularExpression { get; set; }
        [Validate("ScalePrecision(1,1)")]
        [DataMember(Order = 16)]
        public decimal ScalePrecision { get; set; }
    }

    [DataContract]
    public class DynamicValidationRules
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        [Validate("NotNull")]
        [DataMember(Order = 1)]
        public string FirstName { get; set; }
        
        //[Validate("NotNull")] added in IValidationSource
        [DataMember(Order = 2)]
        public string LastName { get; set; }

        // [Validate("[NotNull,InclusiveBetween(13,100)]")]
        [Validate("NotNull")]
        //[Validate("InclusiveBetween(13,100)")] added in IValidationSource
        [DataMember(Order = 3)]
        public int? Age { get; set; }
     
        [Validate("NotEmpty")]
        [DataMember(Order = 4)]
        public DateTime DateOfBirth { get; set; }
     
        [DataMember(Order = 5)]
        public LivingStatus LivingStatus { get; set; }
    }

    [DataContract]
    public class CustomValidationErrors
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        // Just overrides ErrorCode
        [Validate("NotNull", ErrorCode = "ZERROR")]
        [DataMember(Order = 1)]
        public string CustomErrorCode { get; set; }
        
        // Overrides both ErrorCode & Message
        [Validate("InclusiveBetween(1,2)", ErrorCode = "ZERROR", 
            Message = "{PropertyName} has to be between {From} and {To}, you: {PropertyValue}")]
        [DataMember(Order = 2)]
        public int CustomErrorCodeAndMessage { get; set; }

        // Overrides ErrorCode & uses Message from Validators
        [Validate("NotNull", ErrorCode = "RuleMessage")]
        [DataMember(Order = 3)]
        public string ErrorCodeRule { get; set; }

        // Overrides ErrorCode & uses Message from Validators
        [Validate(Condition = ValidationConditions.IsOdd)]
        [DataMember(Order = 4)]
        public int IsOddCondition { get; set; }

        // Combined typed conditions + Error code
        [Validate(AllConditions = new[]{ ValidationConditions.IsOdd, ValidationConditions.IsOver2Digits }, ErrorCode = "RuleMessage")]
        [DataMember(Order = 5)]
        public int IsOddAndOverTwoDigitsCondition { get; set; }

        // Combined typed conditions + unknown error code
        [Validate(AnyConditions = new[]{ ValidationConditions.IsOdd, ValidationConditions.IsOver2Digits })]
        [DataMember(Order = 6)]
        public int IsOddOrOverTwoDigitsCondition { get; set; }
    }

    [ValidateRequest("HasRole('Manager')")]
    [DataContract]
    public class TestAuthValidators
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        [Validate("NotNull")] //doesn't get validated if ValidateRequest is invalid
        [DataMember(Order = 1)]
        public string NotNull { get; set; }
    }

    [ValidateRequest("[IsAuthenticated,HasRole('Manager')]")]
    [DataContract]
    public class TestMultiAuthValidators
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        [Validate("NotNull")] //doesn't get validated if ValidateRequest is invalid
        [DataMember(Order = 1)]
        public string NotNull { get; set; }
    }

    [ValidateRequest("IsAdmin")]
    [DataContract]
    public class TestIsAdmin
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        [Validate("NotNull")] //doesn't get validated if ValidateRequest is invalid
        [DataMember(Order = 1)]
        public string NotNull { get; set; }
    }

    [ValidateRequest(Condition = "!dbExistsSync('SELECT * FROM RockstarAlbum WHERE RockstarId = @Id', { dto.Id })", 
        ErrorCode = "HasForeignKeyReferences")]
    [DataContract]
    public class TestDbCondition
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        
        [Validate("NotNull")] //doesn't get validated if ValidateRequest is invalid
        [DataMember(Order = 2)]
        public string NotNull { get; set; }
    }

    [ValidateRequest("NoRockstarAlbumReferences")]
    [DataContract]
    public class TestDbValidator
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>, IHasId<int>
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        
        [Validate("NotNull")] //doesn't get validated if ValidateRequest is invalid
        [DataMember(Order = 2)]
        public string NotNull { get; set; }
    }

    [ValidateRequest(Conditions = new[]{ "it.Test.isOdd()", "it.Test.log10() > 2" }, ErrorCode = "RuleMessage")]
    [ValidateRequest(Condition = "it.Test.log10() > 3", ErrorCode = "AssertFailed2", Message = "2nd Assert Failed", StatusCode = 401)]
    [DataContract]
    public class OnlyValidatesRequest
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        // Combined typed conditions + Error code
        [DataMember(Order = 1)]
        public int Test { get; set; }

        [Validate("NotNull")] //doesn't get validated if ValidateRequest is invalid
        [DataMember(Order = 2)]
        public string NotNull { get; set; }
    }

    
    [DataContract]
    public class DaoBase
    {
        [DataMember(Order = 1)]
        public virtual Guid Id { get; set; }
        [DataMember(Order = 2)]
        public virtual DateTime CreateDate { get; set; }
        [DataMember(Order = 3)]
        public virtual string CreatedBy { get; set; }
        [DataMember(Order = 4)]
        public virtual DateTime ModifiedDate { get; set; }
        [DataMember(Order = 5)]
        public virtual string ModifiedBy { get; set; }
    }
    
    [DataContract]
    public class Bookmark : DaoBase
    {
        [DataMember(Order = 1)]
        public string Slug { get; set; }
        [DataMember(Order = 2)]
        public string Title { get; set; }
        [DataMember(Order = 3)]
        public string Description { get; set; }
        [DataMember(Order = 4)]
        public string Url { get; set; }
    } 
    
    [DataContract]
    public class QueryBookmarks : QueryDb<Bookmark> { }

    // custom script methods
    [AutoPopulate(nameof(Bookmark.Id), Eval = "nguid")] 
    [AutoPopulate(nameof(Bookmark.CreatedBy), Eval = "userAuthId")]
    [AutoPopulate(nameof(Bookmark.CreateDate), Eval = "utcNow")]
    [AutoPopulate(nameof(Bookmark.ModifiedBy), Eval = "userAuthId")]
    [AutoPopulate(nameof(Bookmark.ModifiedDate), Eval = "utcNow")]
    [DataContract]
    public class CreateBookmark : ICreateDb<Bookmark>, IReturn<CreateBookmarkResponse>
    {
        [DataMember(Order = 1)]
        public string Slug { get; set; }
        [DataMember(Order = 2)]
        public string Title { get; set; }
        [DataMember(Order = 3)]
        public string Description { get; set; }
        [DataMember(Order = 4)]
        public string Url { get; set; }
    }
    
    [DataContract]
    public class CreateBookmarkResponse
    {
        [DataMember(Order = 1)]
        public Guid Id { get; set; }
        [DataMember(Order = 2)]
        public Bookmark Result { get; set; }
        [DataMember(Order = 3)]
        public ResponseStatus ResponseStatus { get; set; }
    }    
}
#endif