using System;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Firebird
{
    public class FirebirdNamingStrategy : OrmLiteNamingStrategyBase
    {
        private static int MaxNameLength { get; set; }

        public FirebirdNamingStrategy() : this(31) { }

        public FirebirdNamingStrategy(int maxNameLength)
        {
            MaxNameLength = maxNameLength;
        }

        public override string GetSchemaName(string name)
        {
            return name != null 
                ? ApplyNameRestrictions(name).ToUpper() 
                : null;
        }

        public override string GetTableName(string name)
        {
            return ApplyNameRestrictions(name).ToUpper();
        }

        public override string GetColumnName(string name)
        {
            return ApplyNameRestrictions(name).ToUpper();
        }

        public override string GetSequenceName(string modelName, string fieldName)
        {
            var seqName = ApplyNameRestrictions($"GEN_{modelName}_{fieldName}").ToUpper();
            return seqName;
        }

        public override string ApplyNameRestrictions(string name)
        {
            name = name.Replace(" ", "_");
            if (name.Length > MaxNameLength)
                name = Squash(name);

            return name.TrimStart('_');
        }

        public override string GetTableName(ModelDefinition modelDef)
        {
            return GetTableName(modelDef.ModelName);
        }

        private static string Squash(string name)
        {
            name.Print();

            // First try squashing out the vowels
            var removeVowels = new[] {"a", "A", "e", "E", "i", "I", "o", "O", "u", "U", "y", "Y"};
            var squashed = name;
            foreach (var removeVowel in removeVowels)
            {
                squashed = squashed.Replace(removeVowel, "");
                if (squashed.Length <= MaxNameLength)
                    return squashed;
            }

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