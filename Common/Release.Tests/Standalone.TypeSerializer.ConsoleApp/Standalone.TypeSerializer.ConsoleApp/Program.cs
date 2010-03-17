using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Standalone.TypeSerializer.ConsoleApp
{
	class Program
	{
		public class Person
		{
			public string Name { get; set; }

			public int Age { get; set; }

			public override string ToString()
			{
				return "Person.ToString(): " 
					+ "Name=" + Name
					+ ", Age = " + Age;
			}
		}

		static void Main(string[] args)
		{
			var person = new Person {Age = 30, Name = "Demis"};

			var personString = ServiceStack.Text.TypeSerializer.SerializeToString(person);
			Console.WriteLine("personString: " + personString);

			var fromPersonString = ServiceStack.Text.TypeSerializer.DeserializeFromString<Person>(personString);
			Console.WriteLine(fromPersonString);

			Console.ReadKey();
		}
	}
}
