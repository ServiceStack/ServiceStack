using System;

namespace ServiceStack.OrmLite.Oracle
{
    public class OracleNamingStrategy : OrmLiteNamingStrategyBase
    {
        private static int MaxNameLength { get; set; }

        public OracleNamingStrategy(int maxNameLength)
        {
            MaxNameLength = maxNameLength;
        }

        public OracleNamingStrategy()
        {
            if (MaxNameLength <= 0) throw new InvalidOperationException("Can't create OracleNamingStrategy first time using default constructor");
        }

        public override string GetTableName(string name)
        {
            return ApplyNameRestrictions(name);
        }

        public override string GetColumnName(string name)
        {
            return ApplyNameRestrictions(name);
        }

        public override string GetSequenceName(string modelName, string fieldName)
        {
            var seqName = ApplyNameRestrictions(modelName + "_" + fieldName + "_GEN");
            return seqName;
        }

        public override string ApplyNameRestrictions(string name)
        {
            if (name.Length > MaxNameLength) name = Squash(name);
            return name.TrimStart('_');
        }

        public override string GetTableName(ModelDefinition modelDef)
        {
            return modelDef.IsInSchema
                       ? ApplyNameRestrictions(modelDef.Schema)
                            + "." + ApplyNameRestrictions(GetTableName(modelDef.ModelName))
                       : GetTableName(modelDef.ModelName);
        }

        private static string Squash(string name)
        {
            // First try squashing out the vowels
            var squashed = name.Replace("a", "").Replace("e", "").Replace("i", "").Replace("o", "").Replace("u", "").Replace("y", "");
            squashed = squashed.Replace("A", "").Replace("E", "").Replace("I", "").Replace("O", "").Replace("U", "").Replace("Y", "");
            if (squashed.Length > MaxNameLength)
            {   // Still too long, squash out every 4th letter, starting at the 3rd
                for (var i = 2; i < squashed.Length - 1; i += 4)
                    squashed = squashed.Substring(0, i) + squashed.Substring(i + 1);
            }
            if (squashed.Length > MaxNameLength)
            {   // Still too long, truncate
                squashed = squashed.Substring(0, MaxNameLength);
            }
            return squashed;
        }
    }
}
