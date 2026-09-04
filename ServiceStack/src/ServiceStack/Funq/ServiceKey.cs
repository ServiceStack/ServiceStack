using System;

namespace Funq
{
    internal sealed class ServiceKey
    {
        int hash;

        public ServiceKey(Type factoryType, string serviceName)
        {
            FactoryType = factoryType;
            Name = serviceName;

            hash = (factoryType?.GetHashCode() ?? 0) ^ (serviceName?.GetHashCode() ?? 0);
        }

        public Type FactoryType;
        public string Name;

        #region Equality

        public bool Equals(ServiceKey other)
        {
            return ServiceKey.Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            return ServiceKey.Equals(this, obj as ServiceKey);
        }

        public static bool Equals(ServiceKey obj1, ServiceKey obj2)
        {
            if (ReferenceEquals(obj1, obj2))
                return true;
            if (obj1 is null || obj2 is null)
                return false;

            return obj1.FactoryType == obj2.FactoryType &&
                string.Equals(obj1.Name, obj2.Name, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return hash;
        }

        #endregion
    }
}