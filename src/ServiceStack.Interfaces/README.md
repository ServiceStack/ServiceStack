# ServiceStack.NET's implementation-free Interfaces project

Most client-facing implementations and providers in ServiceStack generally adhere to interfaces defined in this project.
This good practices approach encourages clients binding to **Interfaces** and not **implementations** making it easy to test/mock, version and change the behaviour of your services.