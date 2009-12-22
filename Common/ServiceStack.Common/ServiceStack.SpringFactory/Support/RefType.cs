using System;

namespace ServiceStack.SpringFactory.Support
{
	public class RefType
	{
		public RefType(string name, Type type)
		{
			this.Name = name;
			this.Type = type;
		}

		public string Name { get; private set; }

		public Type Type { get; set; }
	}
}