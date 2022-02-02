using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace ServiceStack.OrmLite
{
    public class IndexFieldsCacheKey
    {
        int hashCode;

        public ModelDefinition ModelDefinition { get; private set; }

        public IOrmLiteDialectProvider Dialect { get; private set; } 

        public List<string> Fields { get; private set; }

        public IndexFieldsCacheKey(IDataReader reader, ModelDefinition modelDefinition, IOrmLiteDialectProvider dialect)
        {
            ModelDefinition = modelDefinition;
            Dialect = dialect;

            int startPos = 0;
            int endPos = reader.FieldCount;

            Fields = new List<string>(endPos - startPos);

            for (int i = startPos; i < endPos; i++)
                Fields.Add(reader.GetName(i));

            unchecked 
            {
                hashCode = 17;
                hashCode = hashCode * 23 + ModelDefinition.GetHashCode();
                hashCode = hashCode * 23 + Dialect.GetHashCode();
                hashCode = hashCode * 23 + Fields.Count;
                for (int i = 0; i < Fields.Count; i++)
                    hashCode = hashCode * 23 + Fields[i].Length;
            }
        }

        public override bool Equals (object obj)
        {
            var that = obj as IndexFieldsCacheKey;
            
            if (obj == null) return false;
            
            return this.ModelDefinition == that.ModelDefinition
                && this.Dialect == that.Dialect
                && this.Fields.Count == that.Fields.Count
                && this.Fields.SequenceEqual(that.Fields);
        }
        
        public override int GetHashCode()
        {
            return hashCode;
        }
    }
}