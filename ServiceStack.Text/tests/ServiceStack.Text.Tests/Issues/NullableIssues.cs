using NUnit.Framework;

namespace ServiceStack.Text.Tests.Issues
{
    public class NullableIssues
    {
        public class NBoolTest
        {
            public bool? IsOk {get; set;}

            protected bool Equals(NBoolTest other) => IsOk == other.IsOk;
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((NBoolTest) obj);
            }
            public override int GetHashCode() => IsOk.GetHashCode();
        }
        
        [Test]
        public void Does_deserialize_nullable_bools()
        {
            Assert.That("{\"IsOk\": true}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = true }));
            Assert.That("{\"IsOk\": false}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = false }));
            Assert.That("{\"IsOk\": \"true\"}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = true }));
            Assert.That("{\"IsOk\": \"false\"}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = false }));
            Assert.That("{\"IsOk\": \"True\"}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = true }));
            Assert.That("{\"IsOk\": \"False\"}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = false }));
            
            Assert.That("{\"IsOk\": null}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = null }));
        }
        
        [Test]
        public void Does_deserialize_nullable_bools_conventions()
        {
            Assert.That("{\"IsOk\": \"t\"}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = true }));
            Assert.That("{\"IsOk\": \"f\"}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = false }));
            Assert.That("{\"IsOk\": \"Y\"}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = true }));
            Assert.That("{\"IsOk\": \"N\"}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = false }));
            Assert.That("{\"IsOk\": \"on\"}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = true }));
            Assert.That("{\"IsOk\": \"off\"}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = false }));
        }
        
        [Test]
        public void Deserialize_nullable_bools_results_in_error()
        {
            Assert.That("{\"IsOk\": \"tt\"}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = null }));
            Assert.That("{\"IsOk\": \"fu\"}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = null }));
            Assert.That("{\"IsOk\": \"eee\"}".FromJson<NBoolTest>(), Is.EqualTo(new NBoolTest { IsOk = null }));
        }
    }
}