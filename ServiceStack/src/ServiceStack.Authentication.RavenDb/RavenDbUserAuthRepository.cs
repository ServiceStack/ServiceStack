using System;
using System.Reflection;
using ServiceStack.Auth;
using ServiceStack.DataAnnotations;
using Raven.Client.Documents;

namespace ServiceStack.Authentication.RavenDb
{
    public class RavenDbUserAuthRepository : RavenDbUserAuthRepository<RavenUserAuth, RavenUserAuthDetails>, IUserAuthRepository
    {
        public RavenDbUserAuthRepository(IDocumentStore documentStore, bool createIndexes = true) : base(documentStore, createIndexes) { }

        public static Func<MemberInfo, bool> FindIdentityProperty { get; set; } = DefaultFindIdentityProperty;

        public static bool DefaultFindIdentityProperty(MemberInfo p) =>
            p != null && p.Name == ((p.ReflectedType ?? p.DeclaringType)?.FirstAttribute<IndexAttribute>()?.Name ?? "Id");
    }
}