using System;

namespace ServiceStack.Translators.Generator.Support
{
	/// <summary>Implements a basic command-line switch by taking the
	/// switching name and the associated description.</summary>
	/// <remark>Only currently is implemented for properties, so all
	/// auto-switching variables should have a get/set method supplied.</remark>
	[AttributeUsage(AttributeTargets.Property)]
	public class CommandLineSwitchAttribute : System.Attribute
	{
		#region Private Variables
		private string m_name = "";
		private string m_description = "";
		#endregion

		#region Public Properties
		/// <summary>Accessor for retrieving the switch-name for an associated
		/// property.</summary>
		public string Name { get { return m_name; } }

		/// <summary>Accessor for retrieving the description for a switch of
		/// an associated property.</summary>
		public string Description { get { return m_description; } }

		#endregion

		#region Constructors
		/// <summary>Attribute constructor.</summary>
		public CommandLineSwitchAttribute(string name,
													  string description)
		{
			m_name = name;
			m_description = description;
		}
		#endregion
	}
}
