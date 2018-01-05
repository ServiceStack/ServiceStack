using System;

namespace ServiceStack
{
    public class ModelConfig<T>
    {
        public static void Id(GetMemberDelegate<T> getIdFn)
        {
            IdUtils<T>.CanGetId = getIdFn;
        }
    }
}