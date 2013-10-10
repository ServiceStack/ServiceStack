#Azure ICacheClient provider

This project provides an adapter implementation around Microsoft Azure's Cache client

## Usage

As this is a just a drop-in replacement for ICacheClient you can configure it via the normal route using ServiceStack's IOC in your AppHost:

		public class AppHost : AppHostBase
		{
			public override void Configure(Container container)
			{
				container.Register<ICacheClient>(new AzureCacheClient());
			}
		}

Don't forget to setup your Azure connection in your web or app config, i.e.
  <dataCacheClients>
    <dataCacheClient name="default">
      <hosts>
        <host name="yourdomainname.cache.windows.net" cachePort="22233" />
      </hosts>
      <securityProperties mode="Message">
        <messageSecurity authorizationInfo="yourauthinfo"></messageSecurity>
      </securityProperties>
    </dataCacheClient>
    <dataCacheClient name="SslEndpoint">
      <hosts>
        <host name="yourdomainname.cache.windows.net" cachePort="22243" />
      </hosts>
      <securityProperties mode="Message" sslEnabled="true">
        <messageSecurity authorizationInfo="yourauthinfo"></messageSecurity>
      </securityProperties>
    </dataCacheClient>
  </dataCacheClients>