using System;
using System.Data;
using System.Reflection;

namespace ServiceStack.OrmLite.Oracle
{
    public class OracleTimestampConverter
    {
        private readonly Lazy<ConstructorInfo> _oracleTimeStampTzConstructor;

        private readonly Type _factoryType;
        private readonly string _clientProviderName;
        private MethodInfo _oracleTypeParameterSetMethod;
        private object _oracleDbTypeTimeStampTzValue;

        public OracleTimestampConverter(Type factoryType, string clientProviderName)
        {
            _factoryType = factoryType;
            _clientProviderName = clientProviderName;
            _oracleTimeStampTzConstructor = new Lazy<ConstructorInfo>(() => InitConstructor(_factoryType));
            InitSetDbType(_factoryType);
        }

        private ConstructorInfo InitConstructor(Type factoryType)
        {
            var oracleAssembly = factoryType.Assembly;

            var clientProviderNamePrefix = _clientProviderName.Substring(0, _clientProviderName.LastIndexOf('.'));
            var oracleTimeStampTzType = oracleAssembly.GetType(clientProviderNamePrefix + ".Types.OracleTimeStampTZ");
            if (oracleTimeStampTzType == null) return null;

            return oracleTimeStampTzType.GetConstructor(new[] { typeof(DateTime), typeof(string) });
        }

        private ConstructorInfo OracleTimeStampTzConstructor
        {
            get
            {
                return _oracleTimeStampTzConstructor != null 
                    ? _oracleTimeStampTzConstructor.Value 
                    : null;
            }
        }

        public object ConvertToOracleTimeStampTz(DateTimeOffset timestamp)
        {
            if (OracleTimeStampTzConstructor != null)
            {
                return OracleTimeStampTzConstructor
                    .Invoke(new object[] { timestamp.DateTime, timestamp.Offset.ToString()});
            }

            return timestamp;
        }

        private void InitSetDbType(Type factoryType)
        {
            var oracleAssembly = factoryType.Assembly;

            var oracleParameterType = oracleAssembly.GetType(_clientProviderName + ".OracleParameter");
            if (oracleParameterType == null) return;
            var oracleTypeParameter = oracleParameterType.GetProperty("OracleDbType");
            if (oracleTypeParameter == null) return;
            _oracleTypeParameterSetMethod = oracleTypeParameter.GetSetMethod();
            if (_oracleTypeParameterSetMethod == null) return;

            var oracleDbType = oracleAssembly.GetType(_clientProviderName + ".OracleDbType");
            if (oracleDbType == null) return;
            _oracleDbTypeTimeStampTzValue = Enum.Parse(oracleDbType, "TimeStampTZ");
        }

        public void SetParameterTimeStampTzType(IDbDataParameter p)
        {
            _oracleTypeParameterSetMethod.Invoke(p, new [] {_oracleDbTypeTimeStampTzValue});
        }
    }
}
