using System.Collections;
using System.Collections.Generic;

namespace ServiceStack.Redis.Support.Queue.Implementation
{
    public class SerializingRedisClient : RedisClient
    {
        private ISerializer serializer = new ObjectSerializer();

        public SerializingRedisClient(string host)
            : base(host) {}

        public SerializingRedisClient(RedisEndpoint config)
            : base(config) {}
   
        public SerializingRedisClient(string host, int port)
            : base(host, port) {}
        
        /// <summary>
        /// customize the client serializer
        /// </summary>
        public ISerializer Serializer
        {
            set{ serializer = value;}
        }

        /// <summary>
        ///  Serialize object to buffer
        /// </summary>
        /// <param name="value">serializable object</param>
        /// <returns></returns>
        public  byte[] Serialize(object value)
        {
            return serializer.Serialize(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="values">array of serializable objects</param>
        /// <returns></returns>
        public List<byte[]> Serialize(object[] values)
        {
            var rc = new List<byte[]>();
            foreach (var value in values)
            {
                var bytes = Serialize(value);
                if (bytes != null)
                    rc.Add(bytes);
            }
            return rc;
        }

        /// <summary>
        ///  Deserialize buffer to object
        /// </summary>
        /// <param name="someBytes">byte array to deserialize</param>
        /// <returns></returns>
        public  object Deserialize(byte[] someBytes)
        {
            return serializer.Deserialize(someBytes);
        }

        /// <summary>
        /// deserialize an array of byte arrays
        /// </summary>
        /// <param name="byteArray"></param>
        public IList Deserialize(byte[][] byteArray)
        {
            IList rc = new ArrayList();
            foreach (var someBytes in byteArray)
            {
                var obj = Deserialize(someBytes);
                if (obj != null)
                    rc.Add(obj);
            }
            return rc;
        }

    }
}