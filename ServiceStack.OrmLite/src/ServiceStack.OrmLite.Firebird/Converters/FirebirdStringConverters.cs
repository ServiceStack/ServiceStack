using System;
using System.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Firebird.Converters
{
    public class FirebirdStringConverter : StringConverter
    {
        public FirebirdStringConverter() : base(128) {}

        public override string MaxColumnDefinition
        {
            get
            {
                return "BLOB SUB_TYPE 1 SEGMENT SIZE 8192"; 
            } 
        }

        public override string GetColumnDefinition(int? stringLength)
        {
            if (stringLength.GetValueOrDefault() == StringLengthAttribute.MaxText)
                return MaxColumnDefinition;

            return $"VARCHAR({stringLength.GetValueOrDefault(StringLength)})";
        }

        // StringLength (128) is the default VARCHAR length for CREATE TABLE, not a limit on values.
        // StringConverter also uses it as the Size of any string parameter that has none, and
        // FirebirdClient truncates a parameter value to its Size without an error. Leave Size unset
        // so FirebirdClient sizes the parameter from its value; an explicitly set Size is kept.
        public override void InitDbParam(IDbDataParameter p, Type fieldType)
        {
            var sizeWasSet = p.Size != default;
            base.InitDbParam(p, fieldType);

            if (!sizeWasSet && fieldType == typeof(string))
                p.Size = default;
        }
    }

    public class FirebirdCharArrayConverter : CharArrayConverter
    {
        public override string MaxColumnDefinition => DialectProvider.GetStringConverter().MaxColumnDefinition;

        public override string GetColumnDefinition(int? stringLength)
        {
            return MaxColumnDefinition;
        }
    }

}