# Useful high-level App and use-case specific features and utils

The ServiceBase and RestServiceBase classes provide use-ful base classes for your web services to inherit from.

### ServiceBase - base class for RPC services
  
  * Handles C# exceptions and serializes them into your Response DTO's so your clients can programatically access them
  * If you have a IRedisClient installed, rolling error logs will be maintained so you can easily see the latest errors
  * **base.ResolveService()** - let's you access a pre-configured instance of another web service so you can delegate required functionality
  * **base.AppHost** - Accesses the underlying AppHost letting you inspect its configuration, etc

### RestServiceBase - base class for REST Services (extends ServiceBase)

  * Reduces the boiler-plate by already implementing all REST operations so you don't have to e.g. IRestGetService<TRequest>

### ServiceModel
Generic DTO types useful for all web services. e.g. **ResponseStatus** is where C# exceptions get injected into

### Fluent Validation

Includes @JeremySkinner's Fluent Validation (https://github.com/JeremySkinner/FluentValidation)
Which is licensed under the Apache License 2.0 
http://www.apache.org/licenses/LICENSE-2.0.html

### Session
Existing classes