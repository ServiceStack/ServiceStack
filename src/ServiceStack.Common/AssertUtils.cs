using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack
{
    public static class AssertUtils
    {
        public static void AreNotNull<T>(params T[] fields)
        {
            if (fields.Contains(default(T)))
            {
                throw new ArgumentNullException(typeof(T).Name);
            }
        }

        /// <summary>
        /// Asserts that the supplied arguments are not null.
        /// 
        /// AssertUtils.AreNotNull(new Dictionary<string,object>{ {"name",null} });
        ///   will throw new ArgumentNullException("name");
        /// </summary>
        /// <param name="fieldMap">The field map.</param>
        public static void AreNotNull(IDictionary<string, object> fieldMap)
        {
            foreach (var pair in fieldMap)
            {
                if (pair.Value == null)
                {
                    throw new ArgumentNullException(pair.Key);
                }
            }
        }
    }
}