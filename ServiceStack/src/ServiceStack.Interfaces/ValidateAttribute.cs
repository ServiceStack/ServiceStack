using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ServiceStack.DataAnnotations;

namespace ServiceStack;

/// <summary>
/// Assert pre-conditions before DTO's Fluent Validation properties are evaluated
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
[Tag("PropertyOrder")]
public class ValidateRequestAttribute : AttributeBase, IValidateRule, IReflectAttributeConverter
{
    public ValidateRequestAttribute() {}
    public ValidateRequestAttribute(string validator) => Validator = validator;
        
    /// <summary>
    /// Script Expression to create an IPropertyValidator registered in Validators.Types
    /// </summary>
    public string Validator { get; set; }

    /// <summary>
    /// Boolean #Script Code Expression to Test
    /// ARGS:
    ///   - Request: IRequest
    ///   -     dto: Request DTO
    ///   -      it: Request DTO
    /// </summary>
    public string Condition { get; set; }

    /// <summary>
    /// Combine multiple conditions
    /// </summary>
    [Ignore]
    public string[] Conditions
    {
        get => [Condition];
        set => Condition = ValidateAttribute.Combine("&&", value);
    }

    /// <summary>
    /// Custom ErrorCode to return 
    /// </summary>
    public string ErrorCode { get; set; }

    /// <summary>
    /// Custom Error Message to return
    ///  - {PropertyName}
    ///  - {PropertyValue}
    /// </summary>
    public string Message { get; set; }
        
    /// <summary>
    /// Custom Status Code to return when invalid
    /// </summary>
    public int StatusCode { get; set; }
        
    [Ignore]
    public string[] AllConditions
    {
        get => throw new NotSupportedException(nameof(AllConditions));
        set => Condition = ValidateAttribute.Combine("&&", value);
    }

    [Ignore]
    public string[] AnyConditions
    {
        get => throw new NotSupportedException(nameof(AnyConditions));
        set => Condition = ValidateAttribute.Combine("||", value);
    }

    public ReflectAttribute ToReflectAttribute()
    {
        var to = new ReflectAttribute {
            Name = "ValidateRequest",
            PropertyArgs = []
        };
        if (!string.IsNullOrEmpty(Validator))
            to.PropertyArgs.Add(new KeyValuePair<PropertyInfo, object>(GetType().GetProperty(nameof(Validator)), Validator));
        else if (!string.IsNullOrEmpty(Condition))
            to.PropertyArgs.Add(new KeyValuePair<PropertyInfo, object>(GetType().GetProperty(nameof(Condition)), Condition));
        if (!string.IsNullOrEmpty(ErrorCode))
            to.PropertyArgs.Add(new KeyValuePair<PropertyInfo, object>(GetType().GetProperty(nameof(ErrorCode)), ErrorCode));
        if (!string.IsNullOrEmpty(Message))
            to.PropertyArgs.Add(new KeyValuePair<PropertyInfo, object>(GetType().GetProperty(nameof(Message)), Message));
        if (StatusCode != default)
            to.PropertyArgs.Add(new KeyValuePair<PropertyInfo, object>(GetType().GetProperty(nameof(StatusCode)), StatusCode));
        return to;
    }
}

public interface IRequireAuthentication {}

/* Default ITypeValidator defined in ValidateScripts */
    
public class ValidateIsAuthenticatedAttribute() : ValidateRequestAttribute("IsAuthenticated"), IRequireAuthentication;

public class ValidateIsAdminAttribute() : ValidateRequestAttribute("IsAdmin"), IRequireAuthentication;

public class ValidateHasRoleAttribute(string role) : ValidateRequestAttribute("HasRole(`" + role + "`)"), IRequireAuthentication
{
    public string Role => role;
};

public class ValidateHasPermissionAttribute(string permission) : ValidateRequestAttribute("HasPermission(`" + permission + "`)"), IRequireAuthentication
{
    public string Permission => permission;
}

public class ValidateHasClaimAttribute(string type, string value) : ValidateRequestAttribute("HasClaim(`" + type + "`,`" + value + "`)"), IRequireAuthentication
{
    public string Type => type;
    public string Value => value;
}

public class ValidateHasScopeAttribute(string scope) : ValidateRequestAttribute("HasScope(`" + scope + "`)"), IRequireAuthentication
{
    public string Scope => scope;
}

/* ApiKeysFeature */
public class ValidateAuthSecretAttribute() : ValidateRequestAttribute("AuthSecret()");
public class ValidateApiKeyAttribute : ValidateRequestAttribute
{
    public ValidateApiKeyAttribute() : base("ApiKey"){}
    public ValidateApiKeyAttribute(string scope) : base("ApiKey(′" + scope + "′)"){}
}
    
/// <summary>
/// Validate property against registered Validator expression
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
public class ValidateAttribute : AttributeBase, IValidateRule, IReflectAttributeConverter
{
    public ValidateAttribute() {}
    public ValidateAttribute(string validator) => Validator = validator;
        
    /// <summary>
    /// Script Expression to create an IPropertyValidator registered in Validators.Types
    /// </summary>
    public string Validator { get; set; }

    /// <summary>
    /// Boolean #Script Code Expression to Test
    /// ARGS:
    ///   - Request: IRequest
    ///   -     dto: Request DTO
    ///   -   field: Property Name
    ///   -      it: Property Value
    /// </summary>
    public string Condition { get; set; }

    [Ignore]
    public string[] AllConditions
    {
        get => throw new NotSupportedException(nameof(AllConditions));
        set => Condition = Combine("&&", value);
    }

    [Ignore]
    public string[] AnyConditions
    {
        get => throw new NotSupportedException(nameof(AnyConditions));
        set => Condition = Combine("||", value);
    }

    /// <summary>
    /// Custom ErrorCode to return 
    /// </summary>
    public string ErrorCode { get; set; }

    /// <summary>
    /// Refer to FluentValidation docs for Variable
    ///  - {PropertyName}
    ///  - {PropertyValue}
    /// </summary>
    public string Message { get; set; }

    public static string Combine(string comperand, params string[] conditions)
    {
        var sb = new StringBuilder();
        var joiner = ") " + comperand + " ("; 
        foreach (var condition in conditions)
        {
            if (string.IsNullOrEmpty(condition))
                continue;
            if (sb.Length > 0)
                sb.Append(joiner);
            sb.Append(condition);
        }

        sb.Insert(0, '(');
        sb.Append(')');
        return sb.ToString();
    }

    public ReflectAttribute ToReflectAttribute()
    {
        var to = new ReflectAttribute {
            Name = "Validate",
            PropertyArgs = []
        };
        if (!string.IsNullOrEmpty(Validator))
            to.PropertyArgs.Add(new KeyValuePair<PropertyInfo, object>(GetType().GetProperty(nameof(Validator)), Validator));
        else if (!string.IsNullOrEmpty(Condition))
            to.PropertyArgs.Add(new KeyValuePair<PropertyInfo, object>(GetType().GetProperty(nameof(Condition)), Condition));
        if (!string.IsNullOrEmpty(ErrorCode))
            to.PropertyArgs.Add(new KeyValuePair<PropertyInfo, object>(GetType().GetProperty(nameof(ErrorCode)), ErrorCode));
        if (!string.IsNullOrEmpty(Message))
            to.PropertyArgs.Add(new KeyValuePair<PropertyInfo, object>(GetType().GetProperty(nameof(Message)), Message));
        return to;
    }
}

/// <summary>
/// Override to allow a property to be reset back to their default values using partial updates.
/// By default properties with any validators cannot be reset
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class AllowResetAttribute : AttributeBase {}

/// <summary>
/// Don't allow property to be reset
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class DenyResetAttribute : AttributeBase {}
    
//Default IPropertyValidator defined in ValidateScripts
public class ValidateNullAttribute() : ValidateAttribute("Null");
public class ValidateEmptyAttribute() : ValidateAttribute("Empty");
public class ValidateEmailAttribute() : ValidateAttribute("Email");
public class ValidateNotNullAttribute() : ValidateAttribute("NotNull");
public class ValidateNotEmptyAttribute() : ValidateAttribute("NotEmpty");
/// <summary>
/// Validate property against Fluent Validation CreditCardValidator
/// </summary>
public class ValidateCreditCardAttribute() : ValidateAttribute("CreditCard");
public class ValidateLengthAttribute(int min, int max) : ValidateAttribute($"Length({min},{max})");
public class ValidateExactLengthAttribute(int length) : ValidateAttribute($"ExactLength({length})");
public class ValidateMaximumLengthAttribute(int max) : ValidateAttribute($"MaximumLength({max})");
public class ValidateMinimumLengthAttribute(int min) : ValidateAttribute($"MinimumLength({min})");
public class ValidateLessThanAttribute(int value) : ValidateAttribute($"LessThan({value})");
public class ValidateLessThanOrEqualAttribute(int value) : ValidateAttribute($"LessThanOrEqual({value})");
public class ValidateGreaterThanAttribute(int value) : ValidateAttribute($"GreaterThan({value})");
public class ValidateGreaterThanOrEqualAttribute(int value) : ValidateAttribute($"GreaterThanOrEqual({value})");
public class ValidateScalePrecisionAttribute(int scale, int precision) : ValidateAttribute($"ScalePrecision({scale},{precision})");
public class ValidateRegularExpressionAttribute(string pattern) : ValidateAttribute($"RegularExpression(′{pattern}′)");
public class ValidateEqualAttribute : ValidateAttribute
{
    public ValidateEqualAttribute(string value) : base($"Equal(′{value}′)") { }
    public ValidateEqualAttribute(int value) : base($"Equal({value})") { }
    public ValidateEqualAttribute(bool value) : base($"Equal({value.ToString().ToLower()})") { }
}
public class ValidateNotEqualAttribute : ValidateAttribute
{
    public ValidateNotEqualAttribute(string value) : base($"NotEqual(′{value}′)") { }
    public ValidateNotEqualAttribute(int value) : base($"NotEqual({value})") { }
    public ValidateNotEqualAttribute(bool value) : base($"NotEqual({value.ToString().ToLower()})") { }
}
public class ValidateInclusiveBetweenAttribute : ValidateAttribute
{
    public ValidateInclusiveBetweenAttribute(string from, string to) : base($"InclusiveBetween(′{from}′,′{to}′)") { }
    public ValidateInclusiveBetweenAttribute(char from, char to) : base($"InclusiveBetween(′{from}′,′{to}′)") { }
    public ValidateInclusiveBetweenAttribute(int from, int to) : base($"InclusiveBetween({from},{to})") { }
}
public class ValidateExclusiveBetweenAttribute : ValidateAttribute
{
    public ValidateExclusiveBetweenAttribute(string from, string to) : base($"ExclusiveBetween(′{from}′,′{to}′)") { }
    public ValidateExclusiveBetweenAttribute(char from, char to) : base($"ExclusiveBetween(′{from}′,′{to}′)") { }
    public ValidateExclusiveBetweenAttribute(int from, int to) : base($"ExclusiveBetween({from},{to})") { }
}
