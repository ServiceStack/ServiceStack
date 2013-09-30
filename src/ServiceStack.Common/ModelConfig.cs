using System;

namespace ServiceStack
{
    public class ModelConfig<T>
    {
        public static void Id(Func<T, object> getIdFn)
        {
            IdUtils<T>.CanGetId = getIdFn;
        }
    }
}