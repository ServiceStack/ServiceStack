using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace ServiceStack
{
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
            get => new []{ Condition };
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
                PropertyArgs = new List<KeyValuePair<PropertyInfo, object>>()
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
    
    /* Default ITypeValidator defined in ValidateScripts */
    
    public class ValidateIsAuthenticatedAttribute : ValidateRequestAttribute
    {
        public ValidateIsAuthenticatedAttribute() : base("IsAuthenticated") { }
    }
    
    public class ValidateIsAdminAttribute : ValidateRequestAttribute
    {
        public ValidateIsAdminAttribute() : base("IsAdmin") { }
    }
    
    public class ValidateHasRoleAttribute : ValidateRequestAttribute
    {
        public ValidateHasRoleAttribute(string role) : base("HasRole(`" + role + "`)") { }
    }
    
    public class ValidateHasPermissionAttribute : ValidateRequestAttribute
    {
        public ValidateHasPermissionAttribute(string permission) : base("HasPermission(`" + permission + "`)") { }
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

        public static string Combine(string comparand, params string[] conditions)
        {
            var sb = new StringBuilder();
            var joiner = ") " + comparand + " ("; 
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
                PropertyArgs = new List<KeyValuePair<PropertyInfo, object>>()
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
    
    //Default IPropertyValidator defined in ValidateScripts
    public class ValidateNullAttribute : ValidateAttribute
    {
        public ValidateNullAttribute() : base("Null") { }
    }
    public class ValidateEmptyAttribute : ValidateAttribute
    {
        public ValidateEmptyAttribute() : base("Empty") { }
    }
    public class ValidateEmailAttribute : ValidateAttribute
    {
        public ValidateEmailAttribute() : base("Email") { }
    }
    public class ValidateNotNullAttribute : ValidateAttribute
    {
        public ValidateNotNullAttribute() : base("NotNull") { }
    }
    public class ValidateNotEmptyAttribute : ValidateAttribute
    {
        public ValidateNotEmptyAttribute() : base("NotEmpty") { }
    }
    /// <summary>
    /// Validate property against Fluent Validation CreditCardValidator
    /// </summary>
    public class ValidateCreditCardAttribute : ValidateAttribute
    {
        public ValidateCreditCardAttribute() : base("CreditCard") { }
    }
    public class ValidateLengthAttribute : ValidateAttribute
    {
        public ValidateLengthAttribute(int min, int max) : base($"Length({min},{max})") { }
    }
    public class ValidateExactLengthAttribute : ValidateAttribute
    {
        public ValidateExactLengthAttribute(int length) : base($"ExactLength({length})") { }
    }
    public class ValidateMaximumLengthAttribute : ValidateAttribute
    {
        public ValidateMaximumLengthAttribute(int max) : base($"MaximumLength({max})") { }
    }
    public class ValidateMinimumLengthAttribute : ValidateAttribute
    {
        public ValidateMinimumLengthAttribute(int min) : base($"MinimumLength({min})") { }
    }
    public class ValidateLessThanAttribute : ValidateAttribute
    {
        public ValidateLessThanAttribute(int value) : base($"LessThan({value})") { }
    }
    public class ValidateLessThanOrEqualAttribute : ValidateAttribute
    {
        public ValidateLessThanOrEqualAttribute(int value) : base($"LessThanOrEqual({value})") { }
    }
    public class ValidateGreaterThanAttribute : ValidateAttribute
    {
        public ValidateGreaterThanAttribute(int value) : base($"GreaterThan({value})") { }
    }
    public class ValidateGreaterThanOrEqualAttribute : ValidateAttribute
    {
        public ValidateGreaterThanOrEqualAttribute(int value) : base($"GreaterThanOrEqual({value})") { }
    }
    public class ValidateScalePrecisionAttribute : ValidateAttribute
    {
        public ValidateScalePrecisionAttribute(int scale, int precision) : base($"ScalePrecision({scale},{precision})") { }
    }
    public class ValidateRegularExpressionAttribute : ValidateAttribute
    {
        public ValidateRegularExpressionAttribute(string pattern) : base($"RegularExpression(`{pattern}`)") { }
    }
    public class ValidateEqualAttribute : ValidateAttribute
    {
        public ValidateEqualAttribute(string value) : base($"Equal(`{value}`)") { }
        public ValidateEqualAttribute(int value) : base($"Equal({value})") { }
    }
    public class ValidateNotEqualAttribute : ValidateAttribute
    {
        public ValidateNotEqualAttribute(string value) : base($"NotEqual(`{value}`)") { }
        public ValidateNotEqualAttribute(int value) : base($"NotEqual({value})") { }
    }
    public class ValidateInclusiveBetweenAttribute : ValidateAttribute
    {
        public ValidateInclusiveBetweenAttribute(string from, string to) : base($"InclusiveBetween(`{from}`,`{to}`)") { }
        public ValidateInclusiveBetweenAttribute(char from, char to) : base($"InclusiveBetween(`{from}`,`{to}`)") { }
        public ValidateInclusiveBetweenAttribute(int from, int to) : base($"InclusiveBetween({from},{to})") { }
    }
    public class ValidateExclusiveBetweenAttribute : ValidateAttribute
    {
        public ValidateExclusiveBetweenAttribute(string from, string to) : base($"ExclusiveBetween(`{from}`,`{to}`)") { }
        public ValidateExclusiveBetweenAttribute(char from, char to) : base($"ExclusiveBetween(`{from}`,`{to}`)") { }
        public ValidateExclusiveBetweenAttribute(int from, int to) : base($"ExclusiveBetween({from},{to})") { }
    }

    public interface IValidateRule
    {
        string Validator { get; set; }
        string Condition { get; set; }
        string ErrorCode { get; set; }
        string Message { get; set; }
    }

    public class ValidateRule : IValidateRule 
    {
        public string Validator { get; set; }
        public string Condition { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
    }

    public interface IValidationSource
    {
        IEnumerable<KeyValuePair<string, IValidateRule>> GetValidationRules(Type type);
    }

    public interface IValidationSourceAdmin
    {
        Task<List<ValidationRule>> GetAllValidateRulesAsync();
        Task<List<ValidationRule>> GetAllValidateRulesAsync(string typeName);
        Task SaveValidationRulesAsync(List<ValidationRule> validateRules);
        Task<List<ValidationRule>> GetValidateRulesByIdsAsync(params int[] ids);
        Task DeleteValidationRulesAsync(params int[] ids);
        Task ClearCacheAsync();
    }

    /// <summary>
    /// Data persistence Model 
    /// </summary>
    public class ValidationRule : ValidateRule
    {
        [AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// The name of the Type 
        /// </summary>
        [Required]
        public string Type { get; set; }
        
        /// <summary>
        /// The property field for Property Validators, null for Type Validators 
        /// </summary>
        public string Field { get; set; }
        
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        
        public string SuspendedBy { get; set; }
        [Index]
        public DateTime? SuspendedDate { get; set; }

        public string Notes { get; set; }

        protected bool Equals(ValidationRule other)
        {
            return Id == other.Id &&
                   Type == other.Type && Field == other.Field &&
                   CreatedBy == other.CreatedBy && Nullable.Equals(CreatedDate, other.CreatedDate) &&
                   ModifiedBy == other.ModifiedBy && Nullable.Equals(ModifiedDate, other.ModifiedDate) &&
                   SuspendedBy == other.SuspendedBy && Nullable.Equals(SuspendedDate, other.SuspendedDate) &&
                   Notes == other.Notes;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ValidationRule) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Field != null ? Field.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CreatedBy != null ? CreatedBy.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ CreatedDate.GetHashCode();
                hashCode = (hashCode * 397) ^ (ModifiedBy != null ? ModifiedBy.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ModifiedDate.GetHashCode();
                hashCode = (hashCode * 397) ^ (SuspendedBy != null ? SuspendedBy.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ SuspendedDate.GetHashCode();
                hashCode = (hashCode * 397) ^ (Notes != null ? Notes.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}