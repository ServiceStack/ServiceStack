using ServiceStack.Auth;
using ServiceStack.Caching;

namespace ServiceStack
{
    public static class RequiresSchemaProviders
    {
        public static void InitSchema(this ICacheClient cache)
        {
            var requiresSchema = cache as IRequiresSchema;
            requiresSchema?.InitSchema();
        }

        public static void InitSchema(this IAuthRepository authRepo)
        {
            var requiresSchema = authRepo as IRequiresSchema;
            requiresSchema?.InitSchema();
        }
    }
}