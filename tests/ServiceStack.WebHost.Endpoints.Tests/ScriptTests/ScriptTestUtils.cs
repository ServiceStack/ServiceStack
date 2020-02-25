namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public Person() { }
        public Person(string name, int age)
        {
            Name = name;
            Age = age;
        }

        protected bool Equals(Person other) => string.Equals(Name, other.Name) && Age == other.Age;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((Person) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ Age;
            }
        }
    }

}