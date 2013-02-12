using System;
using System.Collections.Generic;

namespace ServiceStack.Api.Swagger
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SwaggerAllowableValuesAttribute : Attribute
    {
		public SwaggerAllowableValuesAttribute(string name)
		{
			this.Name = name;
		}
		public SwaggerAllowableValuesAttribute(string name, int min, int max) : this(name)
		{
			Type = "RANGE";
			Min = min;
			Max = max;
		}

		public SwaggerAllowableValuesAttribute(string name, params string[] values)
			: this(name)
		{
			Type = "LIST";
			Values = values;
		}

		public SwaggerAllowableValuesAttribute(string name, Type enumType)
			: this(name)
		{
			if (enumType.IsEnum)
			{
				Type = "LIST";
				Values = System.Enum.GetNames(enumType);
			}
		}

		public SwaggerAllowableValuesAttribute(string name, Func<string[]> listAction)
			: this(name)
		{
			if (listAction != null)
			{
				Type = "LIST";
				Values = listAction();
			}
		}
        /// <summary>
        /// Gets or sets parameter name with which allowable values will be associated.
        /// </summary>
        public string Name { get; set; }

		public string Type { get; set; }

		public int? Min { get; set; }

		public int? Max { get; set; }

		public String[] Values { get; set; }

        //TODO: should be implemented according to:
        //https://github.com/wordnik/swagger-core/wiki/datatypes
    }
}
