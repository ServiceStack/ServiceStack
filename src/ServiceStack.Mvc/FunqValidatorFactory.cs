using System;
using Funq;
using ServiceStack.FluentValidation;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Mvc
{
	public class FunqValidatorFactory : ValidatorFactoryBase
	{
		private readonly ContainerResolveCache funqBuilder;

		public FunqValidatorFactory(Container container=null)
		{
			this.funqBuilder = new ContainerResolveCache(container ?? AppHostBase.Instance.Container);
		}

		public override IValidator CreateInstance(Type validatorType)
		{
			return funqBuilder.CreateInstance(validatorType, true) as IValidator;
		}
	}
}