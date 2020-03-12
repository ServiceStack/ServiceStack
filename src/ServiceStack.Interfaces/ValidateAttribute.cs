using System;

namespace ServiceStack
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ValidateRequestAttribute : AttributeBase
    {
        public ValidateRequestAttribute() { }

        public ValidateRequestAttribute(string field, string test)
        {
            Field = field;
            Test = test;
        }

        /// <summary>
        /// The Name of Property to Validate and return
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Boolean #Script Code Expression to Test
        /// ARGS:
        ///   - Request: IRequest
        ///   -     dto: Request DTO
        ///   -    name: Property Name
        ///   -      it: Property Value
        /// </summary>
        public string Test { get; set; }

        /// <summary>
        /// Custom ErrorCode to return 
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Custom Error Message to return
        ///  - {PropertyName}
        ///  - {Value}
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Return evaluated Error Message #Script Expression 
        ///   - Request: IRequest
        ///   -     dto: Request DTO
        ///   -    name: Property Name
        ///   -      it: Property Value
        /// </summary>
        public string EvalMessage { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class ValidateAttribute : AttributeBase
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
        ///   -    name: Property Name
        ///   -      it: Property Value
        /// </summary>
        public string Test { get; set; }

        /// <summary>
        /// Custom ErrorCode to return 
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Custom Error Message to return
        ///  - {PropertyName}
        ///  - {Value}
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Return evaluated Error Message #Script Expression 
        ///   - Request: IRequest
        ///   -     dto: Request DTO
        ///   -    name: Property Name
        ///   -      it: Property Value
        /// </summary>
        public string EvalMessage { get; set; }
    }

}