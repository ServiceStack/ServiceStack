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
      <!--To use the in-role flavor of Windows Azure Cache, set identifier to be the cache cluster role name -->
      <!--To use the Windows Azure Cache Service, set identifier to be the endpoint of the cache cluster -->
      <autoDiscover isEnabled="true" identifier="yourdomainname.cache.windows.net" />

      <!--<localCache isEnabled="true" sync="TimeoutBased" objectCount="100000" ttlValue="300" />-->

      <!--Use this section to specify security settings for connecting to your cache. This section is not required if your cache is hosted on a role that is a part of your cloud service. -->
      <securityProperties mode="Message" sslEnabled="false">
        <messageSecurity authorizationInfo="yourauthinfo" />
      </securityProperties>
    </dataCacheClient>
  </dataCacheClients>
