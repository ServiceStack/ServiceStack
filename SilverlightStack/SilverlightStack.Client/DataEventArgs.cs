using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SilverlightStack.Client
{
    
	public class DataEventArgs : EventArgs
	{
		public object Data { get; private set; }
	
		public Type Type { get; private set; }

		public Exception Exception { get; private set; }

		public DataEventArgs(object data)
		{
			Data = data;
		}

		public DataEventArgs(object data, Type type)
			: this(data)
		{
			Type = type;
		}

		public DataEventArgs(object data, Exception exception)
			: this(data)
		{
			Exception = exception;
		}

		public bool IsSuccess
		{
			get { return this.Exception == null; }
		}

		public bool IsSuccessAndOfType<T>()
		{
			return this.IsSuccess && this.Data.GetType() == typeof(T);
		}

		public T GetData<T>()
			where T : class 
		{
			return this.Data as T;
		}

	}

}
