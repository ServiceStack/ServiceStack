using System;

namespace ServiceStack.Aws.Support
{
    public abstract class AwsConnectionFactory<T>
    {
        private readonly Func<T> clientFactory;

        protected AwsConnectionFactory(Func<T> clientFactory)
        {
            Guard.AgainstNullArgument(clientFactory, "clientFactory");
            this.clientFactory = clientFactory;
        }

        public T GetClient()
        {
            return clientFactory();
        }
    }
}