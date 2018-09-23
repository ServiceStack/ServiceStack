namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateTestUtils
    {
        
    }
    
    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public Person() { }
        public Person(string name, int age)
        {
            Name = name;
            Age = age;
        }
    }

}