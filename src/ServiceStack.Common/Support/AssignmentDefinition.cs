using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Common.Utils;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Common.Support
{
    public class AssignmentEntry
    {
        public string Name;
        public AssignmentMember From;
        public AssignmentMember To;
        public PropertyGetterDelegate GetValueFn;
        public PropertySetterDelegate SetValueFn;

        public AssignmentEntry(string name, AssignmentMember @from, AssignmentMember to)
        {
            Name = name;
            From = @from;
            To = to;

            GetValueFn = From.GetGetValueFn();
            SetValueFn = To.GetSetValueFn();
        }
    }

    public class AssignmentMember
    {
        public AssignmentMember(Type type, PropertyInfo propertyInfo)
        {
            Type = type;
            PropertyInfo = propertyInfo;
        }

        public AssignmentMember(Type type, FieldInfo fieldInfo)
        {
            Type = type;
            FieldInfo = fieldInfo;
        }

        public AssignmentMember(Type type, MethodInfo methodInfo)
        {
            Type = type;
            MethodInfo = methodInfo;
        }

        public Type Type;
        public PropertyInfo PropertyInfo;
        public FieldInfo FieldInfo;
        public MethodInfo MethodInfo;

        public PropertyGetterDelegate GetGetValueFn()
        {
            if (PropertyInfo != null)
                return PropertyInfo.GetPropertyGetterFn();
            if (FieldInfo != null)
                return o => FieldInfo.GetValue(o);
            if (MethodInfo != null)
#if NETFX_CORE
                return (PropertyGetterDelegate)
                    MethodInfo.CreateDelegate(typeof(PropertyGetterDelegate));
#else
                return (PropertyGetterDelegate)
                    Delegate.CreateDelegate(typeof(PropertyGetterDelegate), MethodInfo);
#endif
            return null;
        }

        public PropertySetterDelegate GetSetValueFn()
        {
            if (PropertyInfo != null)
                return PropertyInfo.GetPropertySetterFn();
            if (FieldInfo != null)
                return (o, v) => FieldInfo.SetValue(o, v);
            if (MethodInfo != null)
                return (PropertySetterDelegate)MethodInfo.MakeDelegate(typeof(PropertySetterDelegate));

            return null;
        }
    }

    public class AssignmentDefinition
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AssignmentDefinition));

        public AssignmentDefinition()
        {
            this.AssignmentMemberMap = new Dictionary<string, AssignmentEntry>();
        }

        public Type FromType { get; set; }
        public Type ToType { get; set; }

        public Dictionary<string, AssignmentEntry> AssignmentMemberMap { get; set; }

        public void AddMatch(string name, AssignmentMember readMember, AssignmentMember writeMember)
        {
            this.AssignmentMemberMap[name] = new AssignmentEntry(name, readMember, writeMember);
        }

        public void PopulateFromPropertiesWithAttribute(object to, object from, Type attributeType)
        {
            var hasAttributePredicate = (Func<PropertyInfo, bool>)
                (x => x.CustomAttributes(attributeType).Length > 0);
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
            foreach (var assignmentEntry in AssignmentMemberMap)
            {
                var assignmentMember = assignmentEntry.Value;
                var fromMember = assignmentEntry.Value.From;
                var toMember = assignmentEntry.Value.To;

                if (fromMember.PropertyInfo != null && propertyInfoPredicate != null)
                {
                    if (!propertyInfoPredicate(fromMember.PropertyInfo)) continue;
                }

                try
                {
                    var fromValue = assignmentMember.GetValueFn(from);

                    if (valuePredicate != null)
                    {
                        if (!valuePredicate(fromValue)) continue;
                    }

                    if (fromMember.Type != toMember.Type)
                    {
                        if (fromMember.Type == typeof(string))
                        {
                            fromValue = TypeSerializer.DeserializeFromString((string)fromValue, toMember.Type);
                        }
                        else if (toMember.Type == typeof(string))
                        {
                            fromValue = TypeSerializer.SerializeToString(fromValue);
                        }
                        else if (toMember.Type.IsGeneric()
                            && toMember.Type.GenericTypeDefinition() == typeof(Nullable<>))
                        {
                            Type genericArg = toMember.Type.GenericTypeArguments()[0];
                            if (genericArg.IsEnum())
                            {
								fromValue = Enum.ToObject(genericArg, fromValue);
							}
						}
                        else
                        {
                            var listResult = TranslateListWithElements.TryTranslateToGenericICollection(
                                fromMember.Type, toMember.Type, fromValue);

                            if (listResult != null)
                            {
                                fromValue = listResult;
                            }
                        }
                    }

                    var setterFn = assignmentMember.SetValueFn;
                    setterFn(to, fromValue);
                }
                catch (Exception ex)
                {
                    Log.Warn(string.Format("Error trying to set properties {0}.{1} > {2}.{3}",
                        FromType.FullName, fromMember.Type.Name,
                        ToType.FullName, toMember.Type.Name), ex);
                }
            }
        }
    }
}