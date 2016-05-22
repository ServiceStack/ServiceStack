using ServiceStack.ServiceHost.Tests.UseCase;

namespace ServiceStack.ServiceHost.Tests.Support
{
    public class CustomerUseCaseConfig
    {
        public CustomerUseCaseConfig()
        {
            this.UseCache = CustomerUseCase.UseCache;
        }

        public bool UseCache { get; set; }
    }
}