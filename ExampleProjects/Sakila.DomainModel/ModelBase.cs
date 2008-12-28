using ServiceStack.Validation;

namespace Sakila.DomainModel
{
	public abstract class ModelBase
	{
		public virtual ValidationResult Validate()
		{
			return ModelValidator.ValidateObject(this);
		}
	}
}