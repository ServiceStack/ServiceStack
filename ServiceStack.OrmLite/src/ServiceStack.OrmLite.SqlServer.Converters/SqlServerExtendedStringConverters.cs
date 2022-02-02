using System;
using System.Data;
using System.Data.SqlClient;
using ServiceStack.DataAnnotations;
using Microsoft.SqlServer.Types;

namespace ServiceStack.OrmLite.SqlServer.Converters
{
    public class SqlServerExtendedStringConverter : SqlServerStringConverter
    {
        public override object FromDbValue(Type fieldType, object value)
        {
            if (value is SqlHierarchyId)
            {
                var hierarchyId = (SqlHierarchyId)value;
                return (hierarchyId.IsNull) ? null : hierarchyId.ToString();
            }

            if (value is SqlGeography)
            {
                var geography = (SqlGeography)value;
                return (geography.IsNull) ? null : geography.ToString();
            }

            if (value is SqlGeometry)
            {
                var geometry = (SqlGeometry)value;
                return (geometry.IsNull) ? null : geometry.ToString();
            }

            return base.FromDbValue(fieldType, value);
        }

        //public override object ToDbValue(Type fieldType, object value)
        //{
        //    var str = value?.ToString();

        //    if (fieldType == typeof(SqlHierarchyId))
        //    {
        //        return (str == null) ? SqlHierarchyId.Null : SqlHierarchyId.Parse(str);
        //    }

        //    if (fieldType == typeof(SqlGeography))
        //    {
        //        var geography = (SqlGeography)value;
        //        var srid = geography.STSrid.Value;
        //        return (str == null) ? SqlGeography.Null : SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars(str), srid);
        //    }

        //    if (fieldType == typeof(SqlGeometry))
        //    {
        //        var geometry = (SqlGeometry)value;
        //        var srid = geometry.STSrid.Value;
        //        return (str == null) ? SqlGeometry.Null : SqlGeometry.STGeomFromText(new System.Data.SqlTypes.SqlChars(str), srid);
        //    }

        //    return base.ToDbValue(fieldType, value);
        //}
    }
}