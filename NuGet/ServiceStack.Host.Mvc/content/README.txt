You *MUST* register ServiceStacks '/api' path by adding the line below to MvcApplication.RegisterRoutes(RouteCollection) in the Global.asax:

	routes.IgnoreRoute("api/{*pathInfo}"); 
	routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" }); //Prevent exceptions for favicon


To enable the Mini Profiler add the following lines in to MvcApplication in Global.asax.cs:


	protected void Application_BeginRequest(object src, EventArgs e)
	{
		if (Request.IsLocal)
			ServiceStack.MiniProfiler.Profiler.Start();
	}

	protected void Application_EndRequest(object src, EventArgs e)
	{
		ServiceStack.MiniProfiler.Profiler.Stop();
	}


For more info on the MiniProfiler see v3.09 of the https://github.com/ServiceStack/ServiceStack/wiki/Release-Notes


The Urls for metadata page and included Services:

  * /api/metadata - Auto generated metadata pages
  * /api/hello - Simple Hello World Service see: http://www.servicestack.net/ServiceStack.Hello/
  * /api/todos - Simple REST Service see: http://www.servicestack.net/Backbone.Todos/

  * /default.htm - Backbone.js TODO application talking to the TODO REST service at /api/todos



For more info about ServiceStack please visit: http://www.servicestack.net

Feel free to ask questions about ServiceStack on:
http://stackoverflow.com/

or on the mailing Group at:
http://groups.google.com/group/servicestack

Enjoy!