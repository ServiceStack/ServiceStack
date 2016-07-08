using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace ServiceStack
{
    public static class ObjectUtils
    {
        public static bool HasCircularReferences(object value)
        {
            return HasCircularReferences(value, null);
        }

        private static bool HasCircularReferences(object value, Stack<object> parentValues)
        {
            var type = value != null ? value.GetType() : null;

            if (type == null || !type.IsClass || value is string)
                return false;

            if (parentValues == null)
            {
                parentValues = new Stack<object>();
                parentValues.Push(value);
            }

            var valueEnumerable = value as IEnumerable;
            if (valueEnumerable != null)
            {
                foreach (var item in valueEnumerable)
                {
                    if (HasCircularReferences(item, parentValues))
                        return true;
                }
            }
            else
            {
                var props = type.GetSerializableProperties();

                foreach (var pi in props)
                {
                    if (pi.GetIndexParameters().Length > 0)
                        continue;

                    var mi = pi.GetGetMethod();
                    var pValue = mi != null ? mi.Invoke(value, null) : null;
                    if (pValue == null)
                        continue;

                    if (parentValues.Contains(pValue))
                        return true;

                    parentValues.Push(pValue);

                    if (HasCircularReferences(pValue, parentValues))
                        return true;

                    parentValues.Pop();
                }
            }

            return false;
        }
    }
}