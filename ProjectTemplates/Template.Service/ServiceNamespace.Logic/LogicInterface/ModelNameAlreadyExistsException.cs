using System;

namespace @ServiceNamespace@.Logic.LogicInterface
{
	public class @ModelName@AlreadyExistsException : Exception
	{
		public @ModelName@AlreadyExistsException(string message) : base(message) {}
	}
}