using System;
using System.Text;

namespace ServiceStack.Text
{
	public interface ITextFormatter
	{
		string SerializeString(string value);
		string SerializeNumber<T>(T value) where T : struct;
		string SerializeDateTime(DateTime value);
		string SerializeDateTime(DateTime? value);
		string SerializeBoolean(bool value);
		string SerializeValueType(object value);
	}

	public class JsvTextFormatter 
		: ITextFormatter
	{
		public ITextFormatter Instance = new JsvTextFormatter();

		public string SerializeString(string value)
		{
			throw new NotImplementedException();
		}

		public string SerializeNumber<T>(T value) where T : struct
		{
			throw new NotImplementedException();
		}

		public string SerializeDateTime(DateTime value)
		{
			throw new NotImplementedException();
		}

		public string SerializeDateTime(DateTime? value)
		{
			throw new NotImplementedException();
		}

		public string SerializeBoolean(bool value)
		{
			throw new NotImplementedException();
		}

		public string SerializeValueType(object value)
		{
			throw new NotImplementedException();
		}
	}
}