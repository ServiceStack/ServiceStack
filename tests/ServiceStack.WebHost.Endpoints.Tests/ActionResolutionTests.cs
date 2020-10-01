using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Admin;
using ServiceStack.Auth;
using ServiceStack.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class ActionResolutionTests
    {
        List<string> ActionVerbs(List<ActionMethod> actions) =>
            actions.Map(x => StripAsync(x.Name)).Distinct().ToList();
        string StripAsync(string name) => 
            name.Replace("Async", "");

        List<string> StripAsync(IEnumerable<string> names) =>
            names.Map(StripAsync);
        
        [Test]
        public void Does_resolve_all_AuthenticateService_methods()
        {
            var actions = typeof(AuthenticateService).GetActions();
            Assert.That(actions.Count, Is.EqualTo(4));
            var verbs = ActionVerbs(actions);
            var expected = StripAsync(new[] {
                nameof(AuthenticateService.Options),
                nameof(AuthenticateService.GetAsync),
                nameof(AuthenticateService.PostAsync),
                nameof(AuthenticateService.DeleteAsync),
            });
            Assert.That(verbs, Is.EquivalentTo(expected));
        }
        
        [Test]
        public void Does_resolve_all_RegisterService_methods()
        {
            var actions = typeof(RegisterService).GetActions();
            Assert.That(actions.Count, Is.EqualTo(2));
            var verbs = ActionVerbs(actions);
            var expected = StripAsync(new[] {
                nameof(RegisterService.PutAsync),
                nameof(RegisterService.PostAsync),
            });
            Assert.That(verbs, Is.EquivalentTo(expected));
        }
         
        [Test]
        public void Does_resolve_all_AdminUsersService_methods()
        {
            var actions = typeof(AdminUsersService).GetActions();
            Assert.That(actions.Count, Is.EqualTo(5));
            var verbs = ActionVerbs(actions);
            var expected = StripAsync(new[] {
                nameof(AdminUsersService.Get),
                nameof(AdminUsersService.Post),
                nameof(AdminUsersService.Put),
                nameof(AdminUsersService.Delete),
            });
            Assert.That(verbs, Is.EquivalentTo(expected));
        }
    }
}