using System;

namespace ServiceStack.Sakila.Logic.LogicInterface
{
	public class CustomerAlreadyExistsException : Exception
	{
		public CustomerAlreadyExistsException(string message) : base(message) {}
	}
}