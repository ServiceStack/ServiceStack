using System;
using System.Collections.Generic;
using ServiceStack.FluentValidation;
using ServiceStack.Model;

namespace ServiceStack.WebHost.Endpoints.Tests;

public static class ValidationConditions
{
    public const string IsOdd = "it.isOdd()";
    public const string IsOver2Digits = "it.log10() > 2";
}
    
public class ValidateCreateRockstar 
    : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
{
    [Validate(nameof(ValidateScripts.NotNull))]
    // [Validate("NotNull")]
    public string FirstName { get; set; }
        
    //Added by Fluent Validator 
    public string LastName { get; set; }
        
    // [Validate("[" + nameof(ValidateScripts.NotNull) + "," + nameof(ValidateScripts.Length) + "(13,100)]")] e.g. Typed
    // [Validate("[NotNull,Length(13,100)]")]
    [Validate("NotNull")]
    [Validate("InclusiveBetween(13,100)")]
    public int? Age { get; set; }

    [Validate("NotEmpty(default('DateTime'))")]
    //[Validate("NotEmpty")] equivalent to above thanks to: Validators.AppendDefaultValueOnEmptyValidators
    public DateTime DateOfBirth { get; set; }
        
    public DateTime? DateDied { get; set; }
        
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
public class NoAbstractValidator 
    : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
{
    [Validate("NotNull")]
    public string FirstName { get; set; }
        
    [Validate("NotNull")]
    public string LastName { get; set; }

    [Validate("[NotNull,InclusiveBetween(13,100)]")]
    public int? Age { get; set; }
     
    [Validate("NotEmpty")]
    public DateTime DateOfBirth { get; set; }
     
    public LivingStatus LivingStatus { get; set; }
}

public class EmptyValidators 
    : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
{
    // [Validate("NotEmpty(0)")]
    [Validate("NotEmpty")]
    public int Int { get; set; }
    [Validate("NotEmpty")]
    public int? NInt { get; set; }
    [Validate("NotEmpty")]
    // [Validate("NotEmpty(default('System.TimeSpan'))")]
    public TimeSpan TimeSpan { get; set; }
    [Validate("NotEmpty")]
    public TimeSpan? NTimeSpan { get; set; }
    [Validate("NotEmpty")]
    public string String { get; set; }
    [Validate("NotEmpty")]
    public int[] IntArray { get; set; }
    [Validate("NotEmpty")]
    public List<string> StringList { get; set; }
}

public class TriggerAllValidators 
    : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
{
    [ValidateCreditCard]
    public string CreditCard { get; set; }
    [ValidateEmail]
    public string Email { get; set; }
    [ValidateEmpty]
    public string Empty { get; set; }
    [ValidateEqual("Equal")]
    public string Equal { get; set; }
    [ValidateExclusiveBetween(10, 20)]
    public int ExclusiveBetween { get; set; }
    [ValidateGreaterThanOrEqual(10)]
    public int GreaterThanOrEqual { get; set; }
    [ValidateGreaterThan(10)]
    public int GreaterThan { get; set; }
    [ValidateInclusiveBetween(10, 20)]
    public int InclusiveBetween { get; set; }
    [ValidateExactLength(10)]
    public string Length { get; set; }
    [ValidateLessThanOrEqual(10)]
    public int LessThanOrEqual { get; set; }
    [ValidateLessThan(10)]
    public int LessThan { get; set; }
    [ValidateNotEmpty]
    public string NotEmpty { get; set; }
    [ValidateNotEqual("NotEqual")]
    public string NotEqual { get; set; }
    [ValidateNull]
    public string Null { get; set; }
    [ValidateRegularExpression("^[a-z]*$")]
    public string RegularExpression { get; set; }
    [ValidateScalePrecision(1,1)]
    public decimal ScalePrecision { get; set; }
}

public class ValidateRegex
    : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
{
    [ValidateRegularExpression(@"^\+[1-9]\d{1,14}$")]
    public string Phone { get; set; }

    public DateTime DateOfBirth { get; set; } = DateTime.Today;
    public LivingStatus LivingStatus { get; set; }
}

public class DynamicValidationRules
    : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
{
    [ValidateNotNull]
    public string FirstName { get; set; }
        
    //[Validate("NotNull")] added in IValidationSource
    public string LastName { get; set; }

    // [Validate("[NotNull,InclusiveBetween(13,100)]")]
    [ValidateNotNull]
    //[Validate("InclusiveBetween(13,100)")] added in IValidationSource
    public int? Age { get; set; }
     
    [ValidateNotEmpty]
    public DateTime DateOfBirth { get; set; }
     
    public LivingStatus LivingStatus { get; set; }
}

public class CustomValidationErrors
    : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
{
    // Just overrides ErrorCode
    [ValidateNotNull(ErrorCode = "ZERROR")]
    public string CustomErrorCode { get; set; }
        
    // Overrides both ErrorCode & Message
    [Validate("InclusiveBetween(1,2)", ErrorCode = "ZERROR", 
        Message = "{PropertyName} has to be between {From} and {To}, you: {PropertyValue}")]
    public int CustomErrorCodeAndMessage { get; set; }

    // Overrides ErrorCode & uses Message from Validators
    [ValidateNotNull(ErrorCode = "RuleMessage")]
    public string ErrorCodeRule { get; set; }

    // Overrides ErrorCode & uses Message from Validators
    [Validate(Condition = ValidationConditions.IsOdd)]
    public int IsOddCondition { get; set; }

    // Combined typed conditions + Error code
    [Validate(AllConditions = new[]{ ValidationConditions.IsOdd, ValidationConditions.IsOver2Digits }, ErrorCode = "RuleMessage")]
    public int IsOddAndOverTwoDigitsCondition { get; set; }

    // Combined typed conditions + unknown error code
    [Validate(AnyConditions = new[]{ ValidationConditions.IsOdd, ValidationConditions.IsOver2Digits })]
    public int IsOddOrOverTwoDigitsCondition { get; set; }
}

[ValidateHasRole("Manager")]
public class TestAuthValidators
    : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
{
    [Validate("NotNull")] //doesn't get validated if ValidateRequest is invalid
    public string NotNull { get; set; }
}

[ValidateRequest("[IsAuthenticated,HasRole('Manager')]")]
public class TestMultiAuthValidators
    : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
{
    [ValidateNotNull] //doesn't get validated if ValidateRequest is invalid
    public string NotNull { get; set; }
}

[ValidateIsAdmin]
public class TestIsAdmin
    : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
{
    [ValidateNotNull] //doesn't get validated if ValidateRequest is invalid
    public string NotNull { get; set; }
}

[ValidateRequest(Condition = "!dbExistsSync('SELECT * FROM RockstarAlbum WHERE RockstarId = @Id', { dto.Id })", 
    ErrorCode = "HasForeignKeyReferences")]
public class TestDbCondition
    : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
{
    public int Id { get; set; }
        
    [ValidateNotNull] //doesn't get validated if ValidateRequest is invalid
    public string NotNull { get; set; }
}

[ValidateRequest("NoRockstarAlbumReferences")]
public class TestDbValidator
    : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>, IHasId<int>
{
    public int Id { get; set; }
        
    [Validate("NotNull")] //doesn't get validated if ValidateRequest is invalid
    public string NotNull { get; set; }
}

[ValidateRequest(Conditions = new[]{ "it.Test.isOdd()", "it.Test.log10() > 2" }, ErrorCode = "RuleMessage")]
[ValidateRequest(Condition = "it.Test.log10() > 3", ErrorCode = "AssertFailed2", Message = "2nd Assert Failed", StatusCode = 401)]
public class OnlyValidatesRequest
    : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
{
    // Combined typed conditions + Error code
    public int Test { get; set; }

    [Validate("NotNull")] //doesn't get validated if ValidateRequest is invalid
    public string NotNull { get; set; }
}

    
public class DaoBase
{
    public virtual Guid Id { get; set; }
    public virtual DateTimeOffset CreateDate { get; set; }
    public virtual string CreatedBy { get; set; }
    public virtual DateTimeOffset ModifiedDate { get; set; }
    public virtual string ModifiedBy { get; set; }
}
    
public class Bookmark : DaoBase
{
    public string Slug { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
} 
    
public class QueryBookmarks : QueryDb<Bookmark> { }

// custom script methods
[AutoPopulate(nameof(Bookmark.Id), Eval = "nguid", NoCache = true)] 
[AutoPopulate(nameof(Bookmark.CreatedBy), Eval = "userAuthId")]
[AutoPopulate(nameof(Bookmark.CreateDate), Eval = "utcNowOffset")]
[AutoPopulate(nameof(Bookmark.ModifiedBy), Eval = "userAuthId")]
[AutoPopulate(nameof(Bookmark.ModifiedDate), Eval = "utcNowOffset")]
public class CreateBookmark : ICreateDb<Bookmark>, IReturn<CreateBookmarkResponse>
{
    public string Slug { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
}
    
public class CreateBookmarkResponse
{
    public Guid Id { get; set; }
    public Bookmark Result { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}