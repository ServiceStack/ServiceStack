#nullable enable
using System;
using Funq;
using ServiceStack.FluentValidation;
using ServiceStack.Host;

namespace ServiceStack.Mvc;

public class FunqValidatorFactory(Container? container = null) : ValidatorFactoryBase
{
	private readonly ContainerResolveCache funqBuilder = new();

	public override IValidator? CreateInstance(Type validatorType)
	{
		return funqBuilder.CreateInstance(container ?? HostContext.Container, validatorType, true) as IValidator;
	}
}
