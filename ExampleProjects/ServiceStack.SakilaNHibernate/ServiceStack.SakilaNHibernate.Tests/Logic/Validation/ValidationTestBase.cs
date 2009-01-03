using System.Collections.Generic;
using ServiceStack.Validation;

namespace ServiceStack.SakilaNHibernate.Tests.Logic.Validation
{
	public class ValidationTestBase
	{
		protected static Dictionary<string, IList<ValidationError>> CreateErrorMap(IList<ValidationError> errors)
		{
			var errorMap = new Dictionary<string, IList<ValidationError>>();
			foreach (var error in errors)
			{
				if (!errorMap.ContainsKey(error.ErrorCode))
				{
					errorMap[error.ErrorCode] = new List<ValidationError>();
				}
				errorMap[error.ErrorCode].Add(error);
			}
			return errorMap;
		}
	}
}