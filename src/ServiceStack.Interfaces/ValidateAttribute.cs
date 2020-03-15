using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Caching;
using ServiceStack.DataAnnotations;

namespace ServiceStack
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ValidateRequestAttribute : AttributeBase, IValidateRule
    {
        public ValidateRequestAttribute() { }

        public ValidateRequestAttribute(string field, string test)
        {
            Field = field;
            Condition = test;
        }

        /// <summary>
        /// The Name of Property to Validate and return
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Expression to create a validator registered in Validators.Types
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
        /// Custom ErrorCode to return 
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Custom Error Message to return
        ///  - {PropertyName}
        ///  - {PropertyValue}
        /// </summary>
        public string Message { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class ValidateAttribute : AttributeBase, IValidateRule
    {
        public ValidateAttribute() {}
        public ValidateAttribute(string validator) => Validator = validator;
        
        /// <summary>
        /// Expression to create a validator registered in Validators.Types
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

        public string[] AllConditions
        {
            get => throw new NotSupportedException(nameof(AllConditions));
            set => Condition = Combine("&&", value);
        }

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

    public class ValidateRuleBase : IValidateRule 
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

    public interface IValidationSourceWriter
    {
        void SaveValidationRules(List<ValidateRule> validateRules);
    }

    /// <summary>
    /// Data persistence Model 
    /// </summary>
    public class ValidateRule : ValidateRuleBase
    {
        [AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// The name of the Type 
        /// </summary>
        [Required]
        public string Type { get; set; }
        
        /// <summary>
        /// The property field
        /// </summary>
        [Required]
        public string Field { get; set; }
        
        /// <summary>
        /// Results sorted in ascending SortOrder, Id
        /// </summary>
        public int SortOrder { get; set; }
    }
}