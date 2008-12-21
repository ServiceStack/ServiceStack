using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using ServiceStack.Common.Services.Support.Config;

namespace ServiceStack.Common.Services.Serialization
{
    public class RestDataContractDeserializer 
    {
        public static RestDataContractDeserializer Instance = new RestDataContractDeserializer();

        public object Parse(IDictionary<string,string> keyValuePairs, Type returnType)
        {
            try
            {
                return null;
            }
            catch (Exception ex)
            {
                throw new SerializationException("RestDataContractDeserializer: Error converting to type: " + ex.Message, ex);
            }
        }

        public To Parse<To>(IDictionary<string, string> keyValuePairs)
        {
            return (To)Parse(keyValuePairs, typeof(To));
        }
    }
}