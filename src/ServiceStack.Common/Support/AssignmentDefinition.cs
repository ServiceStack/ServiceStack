using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Common.Utils;
using ServiceStack.Logging;
using ServiceStack.Net30.Collections.Concurrent;
using ServiceStack.Text;

namespace ServiceStack.Common.Support
{
    public class AssignmentDefinition
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AssignmentDefinition));

        private ConcurrentDictionary<string, PropertySetterDelegate> PropertySetters = 
            new ConcurrentDictionary<string, PropertySetterDelegate>();

        private ConcurrentDictionary<string, PropertyGetterDelegate> PropertyGetters = 
            new ConcurrentDictionary<string, PropertyGetterDelegate>();

        public AssignmentDefinition()
        {
            this.PropertyInfoMap = new Dictionary<PropertyInfo, PropertyInfo>();
            this.FieldInfoMap = new Dictionary<FieldInfo, FieldInfo>();
        }

        public Type FromType { get; set; }
        public Type ToType { get; set; }

        //from => to
        public Dictionary<PropertyInfo, PropertyInfo> PropertyInfoMap { get; set; }
        public Dictionary<FieldInfo, FieldInfo> FieldInfoMap { get; set; }

        public void AddMatch(PropertyInfo fromPropertyInfo, PropertyInfo toPropertyInfo)
        {
            this.PropertyInfoMap[fromPropertyInfo] = toPropertyInfo;
        }

        public void AddMatch(FieldInfo fromFieldInfo, FieldInfo toFieldInfo)
        {
            this.FieldInfoMap[fromFieldInfo] = toFieldInfo;
        }

        public void PopulateFromPropertiesWithAttribute(object to, object from, Type attributeType)
        {
            var hasAttributePredicate = (Func<PropertyInfo, bool>)
                (x => x.GetCustomAttributes(attributeType, true).Length > 0);

            Populate(to, from, hasAttributePredicate, null);
        }

        public void PopulateWithNonDefaultValues(object to, object from)
        {
            var nonDefaultPredicate = (Func<object, bool>) (x => 
                    x != null && !Equals( x, ReflectionUtils.GetDefaultValue(x.GetType()) )
                );
    
            Populate(to, from, null, nonDefaultPredicate);
        }

        public void Populate(object to, object from)
        {
            Populate(to, from, null, null);
        }

        public void Populate(object to, object from,
            Func<PropertyInfo, bool> propertyInfoPredicate,
            Func<object, bool> valuePredicate)
        {
            foreach (var propertyEntry in PropertyInfoMap)
            {
                var fromPropertyInfo = propertyEntry.Key;
                var toPropertyInfo = propertyEntry.Value;

                if (propertyInfoPredicate != null)
                {
                    if (!propertyInfoPredicate(fromPropertyInfo)) continue;
                }

                try
                {
                    var getterFn = PropertyGetters.GetOrAdd(fromPropertyInfo.Name,
                        fromPropertyInfo.GetPropertyGetterFn());
                    var fromValue = getterFn(from);

                    if (valuePredicate != null)
                    {
                        if (!valuePredicate(fromValue)) continue;
                    }

                    if (fromPropertyInfo.PropertyType != toPropertyInfo.PropertyType)
                    {
                        if (fromPropertyInfo.PropertyType == typeof(string))
                        {
                            fromValue = TypeSerializer.DeserializeFromString((string)fromValue,
                                toPropertyInfo.PropertyType);
                        }
                        else if (toPropertyInfo.PropertyType == typeof(string))
                        {
                            fromValue = TypeSerializer.SerializeToString(fromValue);
                        }
                        else
                        {
                            var listResult = TranslateListWithElements.TryTranslateToGenericICollection(
                                fromPropertyInfo.PropertyType, toPropertyInfo.PropertyType, fromValue);

                            if (listResult != null)
                            {
                                fromValue = listResult;
                            }
                        }
                    }

                    var setterFn = PropertySetters.GetOrAdd(toPropertyInfo.Name,
                        toPropertyInfo.GetPropertySetterFn());

                    setterFn(to, fromValue);
                }
                catch (Exception ex)
                {
                    Log.Warn(string.Format("Error trying to set properties {0}.{1} > {2}.{3}",
                        FromType.FullName, fromPropertyInfo.Name,
                        ToType.FullName, toPropertyInfo.Name), ex);
                }
            }

            foreach (var fieldEntry in FieldInfoMap)
            {
                var fromFieldInfo = fieldEntry.Key;
                var toFieldInfo = fieldEntry.Value;

                try
                {
                    var fromValue = fromFieldInfo.GetValue(from);
                    toFieldInfo.SetValue(to, fromValue);
                }
                catch (Exception ex)
                {
                    Log.Warn(string.Format("Error trying to set fields {0}.{1} > {2}.{3}",
                        FromType.FullName, fromFieldInfo.Name,
                        ToType.FullName, toFieldInfo.Name), ex);
                }
            }
        }
    }
}