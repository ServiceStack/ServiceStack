using System;

namespace ServiceStack.Aws.Support
{
    public static class Guard
    {
        public static void Against(bool assert, string message)
        {
            if (!assert)
                return;

            throw new InvalidOperationException(message);
        }

        public static void Against<TException>(bool assert, string message) where TException : Exception
        {
            if (!assert)
                return;

            throw (TException)Activator.CreateInstance(typeof(TException), message);
        }

        public static void AgainstNullArgument<T>(T source, string argumentName)
            where T : class
        {
            if (source != null)
                return;

            throw new ArgumentNullException(argumentName);
        }

        public static void AgainstArgumentOutOfRange(bool assert, string argumentName)
        {
            if (!assert)
                return;

            throw new ArgumentOutOfRangeException(argumentName);
        }

    }
}