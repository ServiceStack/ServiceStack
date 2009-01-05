using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
	public class ServiceOperations
	{
		public ServiceOperations(Assembly serviceModelAssembly, string operationNamespace)
		{
			try
			{
				var dtoTypes = new List<Type>();
				foreach (var type in serviceModelAssembly.GetTypes())
				{
					if (type.Namespace != operationNamespace) continue;
					
					var baseTypeWithSameName = GetBaseTypeWithTheSameName(type);
					dtoTypes.Add(baseTypeWithSameName);
				}
				AllOperations = new Operations(dtoTypes);
			}
			catch (ReflectionTypeLoadException ex)
			{
				throw;
			}
			ReplyOperations = AllOperations.ReplyOperations;
			OneWayOperations = AllOperations.OneWayOperations;
		}

		/// <summary>
		/// Gets the name of the base most type in the heirachy tree with the same.
		/// 
		/// We get an exception when trying to create a schema with multiple types of the same name
		/// like when inheriting from a DataContract with the same name.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public static Type GetBaseTypeWithTheSameName(Type type)
		{
			var typesWithSameName = new Stack<Type>();
			var baseType = type;
			do
			{
				if (baseType.Name == type.Name) 
					typesWithSameName.Push(baseType);
			}
			while ((baseType = baseType.BaseType) != null);
						
			return typesWithSameName.Pop();
		}

		public Operations ReplyOperations { get; private set; }
		public Operations OneWayOperations { get; private set; }
		public Operations AllOperations { get; private set; }

	}
}