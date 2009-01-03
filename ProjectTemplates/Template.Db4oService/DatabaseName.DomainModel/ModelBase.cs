using ServiceStack.Validation;

namespace @DomainModelNamespace@
{
	public abstract class ModelBase
	{
		public virtual ValidationResult Validate()
		{
			return ModelValidator.ValidateObject(this);
		}
	}
}