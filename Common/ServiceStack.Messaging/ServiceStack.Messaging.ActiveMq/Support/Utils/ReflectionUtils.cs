using System;
using System.Reflection;

namespace ServiceStack.Messaging.ActiveMq.Support.Utils
{
    public class ReflectionUtils
    {
        public static T GetPropertyValue<T>(object obj, string propertyName)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            PropertyInfo pi = obj.GetType().GetProperty(propertyName, typeof(T));
            return (T)pi.GetValue(obj, null);
        }

        public static void SetPropertyValue<T>(object obj, string propertyName, object propertyValue)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            PropertyInfo pi = obj.GetType().GetProperty(propertyName, typeof(T));
            pi.SetValue(obj, propertyValue, null);
        }
    }
}
