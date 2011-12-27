using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.FluentValidation
{
    public static class ValidationErrors
    {
        public const string CreditCard = "CreditCard";
        public const string Email = "Email";
        public const string Equal = "Equal";
        public const string ExclusiveBetween = "ExclusiveBetween";
        public const string GreaterThanOrEqual = "GreaterThanOrEqual";
        public const string GreaterThan = "GreaterThan";
        public const string InclusiveBetween = "InclusiveBetween";
        public const string Length = "Length";
        public const string LessThanOrEqual = "LessThanOrEqual";
        public const string LessThan = "LessThan";
        public const string NotEmpty = "NotEmpty";
        public const string NotEqual = "NotEqual";
        public const string NotNull = "NotNull";
        public const string Predicate = "Predicate";
        public const string RegularExpression = "RegularExpression";
    }
}
