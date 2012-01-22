using System;
using Funq;
using ServiceStack.FluentValidation;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Mvc
{
	public class FunqValidatorFactory : ValidatorFactoryBase
	{
		private readonly AutoWireContainer funqBuilder;

		public FunqValidatorFactory(Container container=null)
		{
			this.funqBuilder = new AutoWireContainer(container ?? AppHostBase.Instance.Container) {
				Scope = ReuseScope.None //don't re-use instances
			};
		}

		public override IValidator CreateInstance(Type validatorType)
		{
			return funqBuilder.CreateInstance(validatorType) as IValidator;
		}
	}
}