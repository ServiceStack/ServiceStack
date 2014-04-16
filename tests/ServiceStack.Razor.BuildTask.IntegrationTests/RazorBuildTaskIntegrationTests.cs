namespace ServiceStack.Razor.BuildTask.Tests
{
    using System;

    using NUnit.Framework;

    [TestFixture]
    public class Tests
    {
        [Test]
        public void All_Views_Exist_In_Assembly()
        {
            Assert.NotNull(Type.GetType("RazorRockstars.__default, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.__Login, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.__NoModelNoController, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.__NotFound, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.__TypedModelNoController, RazorRockstars.BuildTask"));

            Assert.NotNull(Type.GetType("RazorRockstars.stars.alive.__Grohl, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.stars.alive.___Layout, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.stars.alive.Love.__default, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.stars.alive.Springsteen.__default, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.stars.alive.Vedder.__default, RazorRockstars.BuildTask"));

            Assert.NotNull(Type.GetType("RazorRockstars.stars.dead.___Layout, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.stars.dead.Cobain.__default, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.stars.dead.Hendrix.__default, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.stars.dead.Jackson.__default, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.stars.dead.Joplin.__default, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.stars.dead.Presley.__default, RazorRockstars.BuildTask"));

            Assert.NotNull(Type.GetType("RazorRockstars.Views.__AngularJS, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.Views.___Layout, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.Views.__Rockstars, RazorRockstars.BuildTask"));

            Assert.NotNull(Type.GetType("RazorRockstars.Views.Shared.__Empty, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.Views.Shared.__HtmlReport, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.Views.Shared.__MenuAlive, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.Views.Shared.__MenuDead, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.Views.Shared.__OtherPages, RazorRockstars.BuildTask"));
            Assert.NotNull(Type.GetType("RazorRockstars.Views.Shared.__SimpleLayout, RazorRockstars.BuildTask"));

        }
    }
}
