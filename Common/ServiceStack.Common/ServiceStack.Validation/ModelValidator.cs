using System;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Validation.Validators;

namespace ServiceStack.Validation
{
	public class ModelValidator
	{
		private static readonly IDictionary<Type, ModelValidator> validators = new Dictionary<Type, ModelValidator>();
		private readonly List<ValidationAttributeProperty> typeValidationAttributeProperties = new List<ValidationAttributeProperty>();

		/// <summary>
		/// Private class to cache the types properties that have validation attributes
		/// </summary>
		private class ValidationAttributeProperty
		{
			public ValidationAttributeProperty(PropertyInfo propertyInfo, IEnumerable<ValidationAttributeBase> ValidationAttributes)
			{
				this.PropertyInfo = propertyInfo;
				this.ValidationAttributes = new List<ValidationAttributeBase>(ValidationAttributes);
			}

			internal PropertyInfo PropertyInfo { get; private set; }
			internal List<ValidationAttributeBase> ValidationAttributes { get; private set; }
		}

		public Type SupportedType
		{
			get;
			private set;
		}

		public ModelValidator(Type type)
		{
			this.SupportedType = type;

			Parse();
		}

		private void Parse()
		{
			var typeProperties = this.SupportedType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			
			foreach (var propertyInfo in typeProperties)
			{
				var validationAttributes = new List<ValidationAttributeBase>();
				var propertyAttrs = propertyInfo.GetCustomAttributes(true);
				foreach (var attr in propertyAttrs)
				{
					//we only care about our validation attributes
					var validationAttr = attr as ValidationAttributeBase;
					if (validationAttr != null)
					{
						validationAttributes.Add(validationAttr);
					}
				}

				//if the property have validation attributes hold the property and validation attribtues
				if (validationAttributes.Count > 0)
				{
					typeValidationAttributeProperties.Add(
						new ValidationAttributeProperty(propertyInfo, validationAttributes));
				}
			}
		}

		/// <summary>
		/// Gets an ModelValidator for a specific type of object
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static ModelValidator GetObjectValidator(Type type)
		{
			ModelValidator retval;

			if (!validators.TryGetValue(type, out retval))
			{
				retval = new ModelValidator(type);
			}

			return retval;
		}

		/// <summary>
		/// Gets the validation errors for the supplied entity.
		/// No validation errors will return an empty list.
		/// </summary>
		/// <param name="entity">The entity.  Must of a supported type.</param>
		/// <returns></returns>
		public virtual ValidationResult Validate(object entity)
		{
			var validationErrors = new List<ValidationError>();

			if (!this.SupportedType.IsAssignableFrom(entity.GetType()))
			{
				throw new NotSupportedException("Type not supported");
			}

			foreach (var validationAttributeProperty in this.typeValidationAttributeProperties)
			{
				//get the current entity value for the property
				var propertyValue = validationAttributeProperty.PropertyInfo.GetValue(entity, null);
				foreach (var validationAttribute in validationAttributeProperty.ValidationAttributes)
				{
					string errorCode;
					try
					{
						errorCode = validationAttribute.Validate(propertyValue);
					}
					catch (Exception ex)
					{						
						throw new Exception(
							string.Format("Error trying to validate property '{0}' with value '{1}' using validator '{2}'",
							validationAttributeProperty.PropertyInfo.Name, propertyValue, validationAttribute.GetType().Name), ex);
					}
					//Test each validation attribute with the property value
					var isValid = errorCode == null;
					if (isValid) continue;

					var error = new ValidationError(
						errorCode,
						validationAttributeProperty.PropertyInfo.Name,
						validationAttribute.ErrorMessage);

					//store any errors, including the property name
					validationErrors.Add(error);
				}
			}

			return new ValidationResult(validationErrors);
		}

		public static ValidationResult ValidateObject(object entity)
		{
			return GetObjectValidator(entity.GetType()).Validate(entity);
		}

		public static void ThrowIfNotValid(object entity)
		{
			var validationResult = ValidateObject(entity);
			if (!validationResult.IsValid)
			{
				throw new ValidationException(validationResult);
			}
		}
	}
}