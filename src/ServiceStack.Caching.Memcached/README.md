#Memcached ICacheClient provider

This project provides an adapter implementation around Enyim.Caching c# memcached client

## Usage

As this is a just a drop-in replacement for ICacheClient you can configure it via the normal route using ServiceStack's IOC in your AppHost:

		public class AppHost : AppHostBase
		{
			public override void Configure(Container container)
			{
				container.Register<ICacheClient>(new MemcachedClientCache());
			}
		}
