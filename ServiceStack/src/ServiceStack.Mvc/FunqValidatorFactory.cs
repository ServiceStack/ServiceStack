using System;
using Funq;
using ServiceStack.FluentValidation;
using ServiceStack.Host;

namespace ServiceStack.Mvc
{
	public class FunqValidatorFactory : ValidatorFactoryBase
	{
		private readonly ContainerResolveCache funqBuilder;

		public FunqValidatorFactory(Container container=null)
		{
            this.funqBuilder = new ContainerResolveCache(container ?? HostContext.Container);
		}

		public override IValidator CreateInstance(Type validatorType)
		{
			return funqBuilder.CreateInstance(HostContext.Container, validatorType, true) as IValidator;
		}
	}
}