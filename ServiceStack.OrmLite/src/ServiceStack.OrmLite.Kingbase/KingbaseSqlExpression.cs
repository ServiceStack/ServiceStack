using System.Linq;

// ReSharper disable ConvertToPrimaryConstructor

namespace ServiceStack.OrmLite.Kingbase;

public class KingbaseSqlExpression<T> : SqlExpression<T>
{
    public KingbaseSqlExpression(IOrmLiteDialectProvider dialectProvider)
        : base(dialectProvider)
    {
    }

    protected override string GetQuotedColumnName(ModelDefinition tableDef, string memberName)
    {
        if (useFieldName)
        {
            var fieldDef = tableDef.FieldDefinitions.FirstOrDefault(x => x.Name == memberName);
            if (fieldDef is { IsRowVersion: true } && !PrefixFieldWithTableName)
                return KingbaseDialectProvider.RowVersionFieldComparer;

            return base.GetQuotedColumnName(tableDef, memberName);
        }

        return memberName;
    }
}