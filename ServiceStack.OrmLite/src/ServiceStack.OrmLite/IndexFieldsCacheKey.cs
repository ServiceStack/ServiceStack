using System.Linq;

namespace ServiceStack.OrmLite;

public class IndexFieldsCacheKey
{
    readonly int hashCode;

    public ModelDefinition ModelDefinition { get; }

    public IOrmLiteDialectProvider Dialect { get; } 

    public string[] Fields { get; }

    public IndexFieldsCacheKey(string[] fields, ModelDefinition modelDefinition, IOrmLiteDialectProvider dialect)
    {
        Fields = fields;
        ModelDefinition = modelDefinition;
        Dialect = dialect;

        unchecked 
        {
            hashCode = 17;
            hashCode = hashCode * 23 + ModelDefinition.GetHashCode();
            hashCode = hashCode * 23 + Dialect.GetHashCode();
            hashCode = hashCode * 23 + Fields.Length;
            foreach (var field in Fields)
            {
                hashCode = hashCode * 23 + field.Length;
            }
        }
    }

    public override bool Equals (object obj)
    {
        var that = obj as IndexFieldsCacheKey;
            
        if (obj == null) return false;
            
        return this.ModelDefinition == that.ModelDefinition
               && this.Dialect == that.Dialect
               && this.Fields.Length == that.Fields.Length
               && this.Fields.SequenceEqual(that.Fields);
    }
        
    public override int GetHashCode() => hashCode;
}
