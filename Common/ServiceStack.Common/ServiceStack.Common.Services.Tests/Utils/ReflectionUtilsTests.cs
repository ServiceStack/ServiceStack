using ServiceStack.Common.Services.Serialization;
using ServiceStack.Common.Services.Tests.Support.DataContracts;
using ServiceStack.Common.Services.Utils;
using NUnit.Framework;

namespace ServiceStack.Common.Services.Tests.Utils
{
    public class ReflectionUtilsTests
    {
        [Test]
        public void populate_request_data_contract()
        {
            var type = typeof(GetUsers);
            var result = ReflectionUtils.PopulateType(type);
            //ObjectDumperUtils.Write(result);
            var json = JsonDataContractSerializer.Instance.Parse(result);
            Logging.LogManager.GetLogger(GetType()).Debug(json);
        }

        [Test]
        public void populate_response_data_contract()
        {
            var type = typeof(GetUsersResponse);
            var result = ReflectionUtils.PopulateType(type);
            var json = JsonDataContractSerializer.Instance.Parse(result);
            Logging.LogManager.GetLogger(GetType()).Debug(json);
        }

    }
}