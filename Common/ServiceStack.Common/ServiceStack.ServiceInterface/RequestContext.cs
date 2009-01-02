using System;
using ServiceStack.Configuration;

namespace ServiceStack.ServiceInterface
{
	public class RequestContext : IDisposable
	{
		public RequestContext(object requestDto, IFactoryProvider factory)
		{
			this.Dto = requestDto;
			this.Factory = factory;
		}

		public object Dto { get; set; }

		public T Get<T>() where T : class
		{
			var isDto = this.Dto as T;
			return isDto ?? this.Factory.Resolve<T>();
		}

		public IFactoryProvider Factory { get; set; }

		~RequestContext()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				GC.SuppressFinalize(this);

			if (this.Factory != null)
			{
				this.Factory.Dispose();
			}
		}
	}
}