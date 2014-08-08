using Check.ServiceModel.Operations;
using Check.ServiceModel.Types;
using ServiceStack;

namespace Check.ServiceInterface
{
    public class NativeTypesTestService : Service
    {
        public object Any(Hello request)
        {
            return new HelloResponse { Result = request.Name };
        }

        public object Any(HelloAnnotated request)
        {
            return new HelloAnnotatedResponse { Result = request.Name };
        }

        public object Any(HelloAllTypes request)
        {
            return new HelloAllTypesResponse { AllTypes = request.AllTypes, Result = request.Name };
        }

        public object Any(HelloString request)
        {
            return request.Name;
        }

        public void Any(HelloVoid request)
        {
        }

        public object Any(HelloWithDataContract request)
        {
            return new HelloWithDataContractResponse { Result = request.Name };
        }

        public object Any(HelloWithDescription request)
        {
            return new HelloWithDescriptionResponse { Result = request.Name };
        }

        public object Any(HelloWithInheritance request)
        {
            return new HelloWithInheritanceResponse { Result = request.Name };
        }

        public object Any(HelloWithReturn request)
        {
            return new HelloWithAlternateReturnResponse { Result = request.Name };
        }

        public object Any(HelloWithRoute request)
        {
            return new HelloWithRouteResponse { Result = request.Name };
        }

        public object Any(HelloWithType request)
        {
            return new HelloWithTypeResponse
                {
                    Result = new HelloType { Result = request.Name }
                };
        }
    }
}