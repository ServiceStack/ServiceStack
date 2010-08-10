using System;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.WebHost.Endpoints.Metadata;

namespace ServiceStack.Examples.Tests.MonoTests
{
	public class Soap12WsdlProgram
	{
		public static void Main()
		{
			var xsd = new XsdGenerator
			{
				OperationTypes = new[] {
					typeof(GetUsers), typeof(DeleteAllUsers), typeof(StoreNewUser), 
					typeof(GetFactorial), typeof(GetFibonacciNumbers)
				},
				OptimizeForFlash = false,
				IncludeAllTypesInAssembly = false,
			}.ToString();

			Console.WriteLine("xsd: " + xsd);
		}
	}
}