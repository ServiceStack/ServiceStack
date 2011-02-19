using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceStack.WebHost.Endpoints.Metadata;

namespace ServiceStack.ServiceHost
{
	public class ServiceOperations
	{
		IDictionary<string, Type> OperationTypesMap { get; set; }

		public ServiceOperations(IList<Type> dtoTypes)
		{
			AllOperations = new Operations(dtoTypes);
			ReplyOperations = AllOperations.ReplyOperations;
			OneWayOperations = AllOperations.OneWayOperations;
			LoadOperationTypes(dtoTypes);
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

		/// <summary>
		/// Loads the operation types into a dictionary.
		/// If there are multiple operation types with the same name,
		/// the operation type that is last will be the one 'discoverable' via the service.
		/// </summary>
		/// <param name="operationTypes">The operation types.</param>
		private void LoadOperationTypes(IEnumerable<Type> operationTypes)
		{
			OperationTypesMap = new Dictionary<string, Type>();
			foreach (var operationType in operationTypes)
			{
				OperationTypesMap[operationType.Name.ToLower()] = operationType;
			}
		}

		public Type GetOperationType(string operationTypeName)
		{
			Type operationType;
			OperationTypesMap.TryGetValue(operationTypeName.ToLower(), out operationType);
			return operationType;
		}

		public Operations ReplyOperations { get; private set; }
		public Operations OneWayOperations { get; private set; }
		public Operations AllOperations { get; private set; }

	}
}