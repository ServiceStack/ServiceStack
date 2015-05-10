using System;
using System.Collections.Generic;

namespace ServiceStack
{
    public static class TypeExtensions
    {
        public static Type[] GetReferencedTypes(this Type type)
        {
            var refTypes = new HashSet<Type> { type };

            AddReferencedTypes(type, refTypes);

            return refTypes.ToArray();
        }

        public static void AddReferencedTypes(Type type, HashSet<Type> refTypes)
        {
            if (type.BaseType != null)
            {
                if (!refTypes.Contains(type.BaseType))
                {
                    refTypes.Add(type.BaseType);
                    AddReferencedTypes(type.BaseType, refTypes);
                }

                if (!type.BaseType.GetGenericArguments().IsEmpty())
                {
                    foreach (var arg in type.BaseType.GetGenericArguments())
                    {
                        if (!refTypes.Contains(arg))
                        {
                            refTypes.Add(arg);
                            AddReferencedTypes(arg, refTypes);
                        }
                    }
                }
            }

            var properties = type.GetProperties();
            if (!properties.IsEmpty())
            {
                foreach (var p in properties)
                {
                    if (!refTypes.Contains(p.PropertyType))
                    {
                        refTypes.Add(p.PropertyType);
                        AddReferencedTypes(type, refTypes);
                    }

                    var args = p.PropertyType.GetGenericArguments();
                    if (!args.IsEmpty())
                    {
                        foreach (var arg in args)
                        {
                            if (!refTypes.Contains(arg))
                            {
                                refTypes.Add(arg);
                                AddReferencedTypes(arg, refTypes);
                            }
                        }
                    }
                    else if (p.PropertyType.IsArray)
                    {
                        var elType = p.PropertyType.GetElementType();
                        if (!refTypes.Contains(elType))
                        {
                            refTypes.Add(elType);
                            AddReferencedTypes(elType, refTypes);
                        }
                    }
                }
            }
        }

    }

}