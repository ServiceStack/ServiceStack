using System;
using System.Collections.Generic;
using Check.ServiceModel;
using ServiceStack;
using ServiceStack.FluentValidation;

namespace CheckWeb
{
    public static class ValidationConditions
    {
        public const string IsOdd = "it.isOdd()";
        public const string IsOver2Digits = "it.log() > 2";
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
        [Validate("CreditCard")]
        public string CreditCard { get; set; }
        [Validate("Email")]
        public string Email { get; set; }
        [Validate("Empty")]
        public string Empty { get; set; }
        [Validate("Equal('Equal')")]
        public string Equal { get; set; }
        [Validate("ExclusiveBetween(10, 20)")]
        public int ExclusiveBetween { get; set; }
        [Validate("GreaterThanOrEqual(10)")]
        public int GreaterThanOrEqual { get; set; }
        [Validate("GreaterThan(10)")]
        public int GreaterThan { get; set; }
        [Validate("InclusiveBetween(10, 20)")]
        public int InclusiveBetween { get; set; }
        [Validate("ExactLength(10)")]
        public string Length { get; set; }
        [Validate("LessThanOrEqual(10)")]
        public int LessThanOrEqual { get; set; }
        [Validate("LessThan(10)")]
        public int LessThan { get; set; }
        [Validate("NotEmpty")]
        public string NotEmpty { get; set; }
        [Validate("NotEqual('NotEqual')")]
        public string NotEqual { get; set; }
        [Validate("Null")]
        public string Null { get; set; }
        [Validate("RegularExpression('^[a-z]*$')")]
        public string RegularExpression { get; set; }
        [Validate("ScalePrecision(1,1)")]
        public decimal ScalePrecision { get; set; }
    }

    public class DynamicValidationRules
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        [Validate("NotNull")]
        public string FirstName { get; set; }
        
        //[Validate("NotNull")] added in IValidationSource
        public string LastName { get; set; }

        // [Validate("[NotNull,InclusiveBetween(13,100)]")]
        [Validate("NotNull")]
        //[Validate("InclusiveBetween(13,100)")] added in IValidationSource
        public int? Age { get; set; }
     
        [Validate("NotEmpty")]
        public DateTime DateOfBirth { get; set; }
     
        public LivingStatus LivingStatus { get; set; }
    }

    public class CustomValidationErrors
        : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdResponse>
    {
        // Just overrides ErrorCode
        [Validate("NotNull", ErrorCode = "ZERROR")]
        public string CustomErrorCode { get; set; }
        
        // Overrides both ErrorCode & Message
        [Validate("InclusiveBetween(1,2)", ErrorCode = "ZERROR", 
            Message = "{PropertyName} has to be between {From} and {To}, you: {PropertyValue}")]
        public int CustomErrorCodeAndMessage { get; set; }

        // Overrides ErrorCode & uses Message from Validators
        [Validate("NotNull", ErrorCode = "RuleMessage")]
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
}