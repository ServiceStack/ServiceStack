using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Caching;
using ServiceStack.DataAnnotations;

namespace ServiceStack
{
    /// <summary>
    /// Assert pre-conditions before DTO's Fluent Validation properties are evaluated
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    [Tag("PropertyOrder")]
    public class ValidateRequestAttribute : AttributeBase, IValidateRule
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
        ///   -   field: Property Name
        ///   -      it: Property Value
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
        
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class ValidateAttribute : AttributeBase, IValidateRule
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
        Task<List<ValidationRule>> GetAllValidateRulesAsync(string typeName);
        Task SaveValidationRulesAsync(List<ValidationRule> validateRules);
        Task<List<ValidationRule>> GetValidateRulesByIdsAsync(params int[] ids);
        Task DeleteValidationRulesAsync(params int[] ids);
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
        
        /// <summary>
        /// Results sorted in ascending SortOrder, Id
        /// </summary>
        public int SortOrder { get; set; }
        
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
                   Type == other.Type && Field == other.Field && SortOrder == other.SortOrder &&
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
                hashCode = (hashCode * 397) ^ SortOrder;
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