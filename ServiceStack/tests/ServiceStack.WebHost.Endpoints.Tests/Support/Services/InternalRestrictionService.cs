namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    [Restrict(AccessTo = RequestAttributes.InternalNetworkAccess)]
    public class InternalRestriction { }

    [Restrict(RequestAttributes.Localhost)]
    public class LocalhostRestriction { }

    [Restrict(RequestAttributes.LocalSubnet)]
    public class LocalSubnetRestriction { }

    [Restrict(RequestAttributes.InProcess)]
    public class InProcessRestriction { }

    [Restrict(AccessTo = RequestAttributes.None)]
    public class AccessToNoneRestriction { }

    public class NetworkRestrictionServices : Service
    {
        public object Any(InternalRestriction request) => request;
        public object Any(InProcessRestriction request) => request;
        public object Any(LocalhostRestriction request) => request;
        public object Any(LocalSubnetRestriction request) => request;
        public object Any(AccessToNoneRestriction request) => request;
    }

    public class LocalhostRestrictionOnService : IReturn<Response> { }

    [Restrict(LocalhostOnly = true)]
    public class LocalHostOnService : Service
    {
        public object Any(LocalhostRestrictionOnService request) => request;
    }
}