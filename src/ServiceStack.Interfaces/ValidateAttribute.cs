using System;

namespace ServiceStack
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ValidateRequestAttribute : AttributeBase
    {
        public ValidateRequestAttribute(string fieldName, string test)
        {
            FieldName = fieldName;
            Test = test;
        }

        /// <summary>
        /// The Name of Property to Validate and return
        /// </summary>
        public string FieldName { get; set; }
        
        /// <summary>
        /// #Script Code Expression to Test
        /// ARGS:
        ///   - dto: Request DTO
        ///   -  it: Property Value
        ///   - req: IRequest
        /// </summary>
        public string Test { get; set; }
        
        /// <summary>
        /// Custom Error Message to return using string.Format(), ARGS:
        ///     - {0}: Property Value
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Custom ErrorCode to return 
        /// </summary>
        public string ErrorCode { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ValidateAttribute : AttributeBase
    {
        public ValidateAttribute(string test) => Test = test;

        /// <summary>
        /// #Script Code Expression to Test
        /// ARGS:
        ///   - dto: Request DTO
        ///   -  it: Property Value
        ///   - req: IRequest
        /// </summary>
        public string Test { get; set; }

        /// <summary>
        /// Custom Error Message to return using string.Format(), ARGS:
        ///     - {0}: Property Value
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Custom ErrorCode to return 
        /// </summary>
        public string ErrorCode { get; set; }
    }
}