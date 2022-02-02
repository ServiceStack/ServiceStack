using System;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Oracle.Converters
{
    public class OracleStringConverter : StringConverter
    {
        public OracleStringConverter() : base(4000) {}

        protected OracleStringConverter(int stringLength) : base(stringLength)
        {
        }

        public override string MaxColumnDefinition => UseUnicode ? "NVARCHAR2(2000)" : "NVARCHAR2(4000)";

        public override int MaxVarCharLength => UseUnicode ? 2000 : 4000;

        public override string GetColumnDefinition(int? stringLength)
        {
            if (stringLength.GetValueOrDefault() == StringLengthAttribute.MaxText)
                return MaxColumnDefinition;

            var safeLength = Math.Min(stringLength.GetValueOrDefault(StringLength), UseUnicode ? 2000 : 4000);

            return UseUnicode
                ? $"NVARCHAR({safeLength})"
                : $"VARCHAR({safeLength})";
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Oracle12StringConverter : StringConverter
    {
        public Oracle12StringConverter() : base(32767) {}

        public override string MaxColumnDefinition => UseUnicode ? "NVARCHAR2(16383)" : "NVARCHAR2(32767)";

        public override int MaxVarCharLength => UseUnicode ? 16383 : 32767;

        public override string GetColumnDefinition(int? stringLength)
        {
            if (stringLength.GetValueOrDefault() == StringLengthAttribute.MaxText)
                return MaxColumnDefinition;

            var safeLength = Math.Min(stringLength.GetValueOrDefault(StringLength), UseUnicode ? 16383 : 32767);

            return UseUnicode
                ? $"NVARCHAR({safeLength})"
                : $"VARCHAR({safeLength})";
        }
    }
}