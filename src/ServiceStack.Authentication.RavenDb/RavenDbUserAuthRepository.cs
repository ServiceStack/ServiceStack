using System;
using System.Reflection;
using ServiceStack.Auth;
using ServiceStack.DataAnnotations;
using Raven.Client.Documents;

namespace ServiceStack.Authentication.RavenDb
{
    public class RavenDbUserAuthRepository : RavenDbUserAuthRepository<RavenUserAuth, RavenUserAuthDetails>, IUserAuthRepository
    {
        public RavenDbUserAuthRepository(IDocumentStore documentStore) : base(documentStore) { }

        public static Func<MemberInfo, bool> FindIdentityProperty { get; set; } = DefaultFindIdentityProperty;

        public static bool DefaultFindIdentityProperty(MemberInfo p) =>
            p.Name == (p.DeclaringType.FirstAttribute<IndexAttribute>()?.Name ?? "Id");
    }
}