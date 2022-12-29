using System;
using System.Collections.Generic;
using System.Data;

namespace ServiceStack.OrmLite.Tests
{
    public static class TestHelpers
    {
        public static string NormalizeSql(this string sql)
        {
            return sql.ToLower()
                .Replace("\"", "")
                .Replace("`", "")
                .Replace("_", "")
                .Replace(":", "@")   //postgresql
                .Replace("\n", " ")
                .TrimEnd(); 
        }

        public static string NormalizeQuotes(this string sql)
        {
            return sql.ToLower()
                .Replace("\"", "'")
                .Replace("`", "'")
                .Replace("[", "'")
                .Replace("]", "'")
                .TrimEnd(); 
        }

        public static string PreNormalizeSql(this string sql, IDbConnection db)
        {
            var paramString = db.GetDialectProvider().ParamString;
            if (paramString.Equals("@"))
                return sql;
            return sql.Replace("@", paramString);
        }

        public static List<int> AdjustIds(this IEnumerable<int> ids, int initialId)
        {
            var result = new List<int>();
            foreach (var id in ids)
                result.Add(id + initialId);
            return result;
        }
    }
}