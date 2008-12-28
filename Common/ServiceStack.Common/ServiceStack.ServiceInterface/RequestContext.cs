using System;

namespace ServiceStack.ServiceInterface
{
	public class RequestContext : IDisposable
	{
		public RequestContext(object requestDto, IDisposable facade)
		{
			this.Dto = requestDto;
			this.Facade = facade;
		}

		public object Dto { get; set; }
		public IDisposable Facade { get; set; }

		public TFacade GetFacade<TFacade>()
		{
			return (TFacade)this.Facade;
		}

		public TRequest GetDto<TRequest>()
		{
			return (TRequest)this.Dto;
		}

		public void Dispose()
		{
			if (this.Facade != null)
			{
				this.Facade.Dispose();
			}
		}
	}
}