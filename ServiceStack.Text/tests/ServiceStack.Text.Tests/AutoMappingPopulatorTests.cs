using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    public class AutoMappingPopulatorTests
    {
        public class UserData
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public Car Car { get; set; }
        }
        
        private static UserData CreateUser() =>
            new UserData {
                FirstName = "John",
                LastName = "Doe",
                Car = new Car {Name = "BMW X6", Age = 3}
            };

        [Test]
        public void Does_call_populator_for_PopulateWith()
        {
            AutoMapping.RegisterPopulator((UserDto target, UserData source) => 
                target.LastName += "?!");

            var user = CreateUser();
            var dtoUser = new UserDto().PopulateWith(user);
            
            Assert.That(dtoUser.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(dtoUser.LastName, Is.EqualTo(user.LastName + "?!"));
        }

        [Test]
        public void Does_call_populator_for_PopulateWithNonDefaultValues()
        {
            AutoMapping.RegisterPopulator((UserDto target, UserData source) => 
                target.LastName += "?!");

            var user = CreateUser();
            var dtoUser = new UserDto().PopulateWithNonDefaultValues(user);
            
            Assert.That(dtoUser.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(dtoUser.LastName, Is.EqualTo(user.LastName + "?!"));
        }

        [Test]
        public void Does_call_populator_for_PopulateFromPropertiesWithoutAttribute()
        {
            AutoMapping.RegisterPopulator((UserDto target, UserData source) => 
                target.LastName += "?!");

            var user = CreateUser();
            var dtoUser = new UserDto().PopulateFromPropertiesWithoutAttribute(user, typeof(IgnoreAttribute));
            
            Assert.That(dtoUser.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(dtoUser.LastName, Is.EqualTo(user.LastName + "?!"));
        }

        [Test]
        public void Does_call_populator_for_ConvertTo()
        {
            AutoMapping.RegisterPopulator((UserDto target, UserData source) => 
                target.LastName += "?!");

            var user = CreateUser();
            var dtoUser = user.ConvertTo<UserDto>();
            
            Assert.That(dtoUser.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(dtoUser.LastName, Is.EqualTo(user.LastName + "?!"));
        }
    }
}