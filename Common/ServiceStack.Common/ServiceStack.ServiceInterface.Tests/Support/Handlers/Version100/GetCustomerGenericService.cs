using System;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface.Tests.Support.Version100.Operations;

namespace ServiceStack.ServiceInterface.Tests.Support.Handlers.Version100
{
	public class GetCustomerGenericService 
		: IService<GetCustomerGeneric>
	{
		public object Execute(GetCustomerGeneric requestDto)
		{
			return new GetCustomerGenericResponse();
		}
	}
}