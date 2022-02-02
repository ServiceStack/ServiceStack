using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    public class AutoMappingCustomConverterTests
    {
        public class PersonWithWrappedDateOfBirth : User
        {
            public WrappedDateTimeOffset DateOfBirth { get; set; }
        }

        public class PersonWithDateOfBirth : User
        {
            public DateTimeOffset DateOfBirth { get; set; }
        }

        public class WrappedDateTimeOffset
        {
            private readonly DateTimeOffset dateTimeOffset;

            public WrappedDateTimeOffset(DateTimeOffset dateTimeOffset)
            {
                this.dateTimeOffset = dateTimeOffset;
            }

            public DateTimeOffset ToDateTimeOffset()
            {
                return dateTimeOffset;
            }
        }

        [Test]
        public void Can_convert_prop_with_CustomTypeConverter()
        {
            AutoMapping.RegisterConverter((WrappedDateTimeOffset from) => from.ToDateTimeOffset());

            var map = new Dictionary<string, object>
            {
                { "FirstName", "Foo" },
                { "LastName", "Bar" },
                { "DateOfBirth", new WrappedDateTimeOffset(
                    new DateTimeOffset(1971, 3, 23, 4, 30, 0, TimeSpan.Zero)) }
            };

            var personWithDoB = map.FromObjectDictionary<PersonWithDateOfBirth>();

            Assert.That(personWithDoB.FirstName, Is.EqualTo("Foo"));
            Assert.That(personWithDoB.LastName, Is.EqualTo("Bar"));
            Assert.That(personWithDoB.DateOfBirth, Is.Not.Null);
            Assert.That(personWithDoB.DateOfBirth.Year, Is.EqualTo(1971));
            Assert.That(personWithDoB.DateOfBirth.Month, Is.EqualTo(3));
            Assert.That(personWithDoB.DateOfBirth.Day, Is.EqualTo(23));
            Assert.That(personWithDoB.DateOfBirth.Hour, Is.EqualTo(4));
            Assert.That(personWithDoB.DateOfBirth.Minute, Is.EqualTo(30));
            Assert.That(personWithDoB.DateOfBirth.Second, Is.EqualTo(0));
            
            AutoMappingUtils.Reset();
        }

        [Test]
        public void Can_Convert_Props_With_CustomTypeConverter()
        {
            AutoMapping.RegisterConverter((WrappedDateTimeOffset from) => from.ToDateTimeOffset());

            var personWithWrappedDateOfBirth = new PersonWithWrappedDateOfBirth
            {
                FirstName = "Foo",
                LastName = "Bar",
                DateOfBirth = new WrappedDateTimeOffset(
                    new DateTimeOffset(1971, 3, 23, 4, 30, 0, TimeSpan.Zero))
            };

            var personWithDoB = personWithWrappedDateOfBirth.ConvertTo<PersonWithDateOfBirth>();

            Assert.That(personWithDoB.FirstName, Is.EqualTo("Foo"));
            Assert.That(personWithDoB.LastName, Is.EqualTo("Bar"));
            Assert.That(personWithDoB.DateOfBirth, Is.Not.Null);
            Assert.That(personWithDoB.DateOfBirth.Year, Is.EqualTo(1971));
            Assert.That(personWithDoB.DateOfBirth.Month, Is.EqualTo(3));
            Assert.That(personWithDoB.DateOfBirth.Day, Is.EqualTo(23));
            Assert.That(personWithDoB.DateOfBirth.Hour, Is.EqualTo(4));
            Assert.That(personWithDoB.DateOfBirth.Minute, Is.EqualTo(30));
            Assert.That(personWithDoB.DateOfBirth.Second, Is.EqualTo(0));

            AutoMappingUtils.Reset();
        }

        [Test]
        public void Can_Convert_Anonymous_Types_With_CustomTypeConverter()
        {
            AutoMapping.RegisterConverter((DateTimeOffset from) => new WrappedDateTimeOffset(from));

            var personWithDateOfBirth = new
            {
                FirstName = "Foo",
                LastName = "Bar",
                DateOfBirth = new DateTimeOffset(1971, 3, 23, 4, 30, 0, TimeSpan.Zero)
            };

            var personWithWrappedDoB = personWithDateOfBirth.ConvertTo<PersonWithWrappedDateOfBirth>();

            Assert.That(personWithWrappedDoB.FirstName, Is.EqualTo("Foo"));
            Assert.That(personWithWrappedDoB.LastName, Is.EqualTo("Bar"));
            Assert.That(personWithWrappedDoB.DateOfBirth, Is.Not.Null);
            var dto = personWithWrappedDoB.DateOfBirth.ToDateTimeOffset();
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto.Year, Is.EqualTo(1971));
            Assert.That(dto.Month, Is.EqualTo(3));
            Assert.That(dto.Day, Is.EqualTo(23));
            Assert.That(dto.Hour, Is.EqualTo(4));
            Assert.That(dto.Minute, Is.EqualTo(30));
            Assert.That(dto.Second, Is.EqualTo(0));

            AutoMappingUtils.Reset();
        }

        // TODO: Work out a way we can capture and monitor the Trace log in the test, as exceptions are caught in Populate method
        [Test]
        public void Should_Not_Throw_Exception_When_Multiple_Same_Type_CustomTypeConverters_Found()
        {
            AutoMapping.RegisterConverter((DateTimeOffset from) => new WrappedDateTimeOffset(from));

            var personWithWrappedDateOfBirth = new PersonWithWrappedDateOfBirth
            {
                DateOfBirth = new WrappedDateTimeOffset(
                    new DateTimeOffset(1971, 3, 23, 4, 30, 0, TimeSpan.Zero))
            };

            var personWithDoB = personWithWrappedDateOfBirth.ConvertTo<PersonWithDateOfBirth>();

            // Object returned but mapping failed
            Assert.That(personWithDoB.FirstName, Is.Null);
            Assert.That(personWithDoB.LastName, Is.Null);
            Assert.That(personWithDoB.DateOfBirth, Is.EqualTo(DateTimeOffset.MinValue));
             
            AutoMappingUtils.Reset();
        }

        [Test]
        public void Can_Convert_POCO_collections_with_custom_Converter()
        {
            AutoMapping.RegisterConverter((User from) => {
                var to = from.ConvertTo<UserDto>(skipConverters:true); // avoid infinite recursion
                to.FirstName += "!";
                to.LastName += "!";
                return to;
            });
            AutoMapping.RegisterConverter((Car from) => $"{from.Name} ({from.Age})");

            var user = new User {
                FirstName = "John",
                LastName = "Doe",
                Car = new Car { Name = "BMW X6", Age = 3 }
            };
            var users = new UsersData {
                Id = 1,
                User = user,
                UsersList = { user },
                UsersMap = { {1,user} }
            };

            var dtoUsers = users.ConvertTo<UsersDto>();
            Assert.That(dtoUsers.Id, Is.EqualTo(users.Id));

            void AssertUser(UserDto userDto)
            {
                Assert.That(userDto.FirstName, Is.EqualTo(user.FirstName + "!"));
                Assert.That(userDto.LastName, Is.EqualTo(user.LastName + "!"));
                Assert.That(userDto.Car, Is.EqualTo($"{user.Car.Name} ({user.Car.Age})"));
            }
            AssertUser(user.ConvertTo<UserDto>());
            AssertUser(dtoUsers.User);
            AssertUser(dtoUsers.UsersList[0]);
            AssertUser(dtoUsers.UsersMap[1]);

            AutoMappingUtils.Reset();
        }

        [Test]
        public void Does_ignore_POCO_mappings()
        {
            AutoMapping.IgnoreMapping<User, UserDto>();

            var user = new User {
                FirstName = "John",
                LastName = "Doe",
                Car = new Car { Name = "BMW X6", Age = 3 }
            };
            var users = new UsersData {
                Id = 1,
                User = user,
                UsersList = { user },
                UsersMap = {{1,user}}
            };

            var dtoUsers = users.ConvertTo<UsersDto>();
            Assert.That(dtoUsers.Id, Is.EqualTo(users.Id));

            Assert.That(user.ConvertTo<UserDto>(), Is.Null);
            Assert.That(dtoUsers.User, Is.Null);
            Assert.That(dtoUsers.UsersList, Is.Empty);
            Assert.That(dtoUsers.UsersMap, Is.Empty);
            
            AutoMappingUtils.Reset();
        }

        [Test]
        public void Does_ignore_collection_mappings()
        {
            AutoMapping.IgnoreMapping<List<User>, List<UserDto>>();
            AutoMapping.IgnoreMapping<Dictionary<int, User>, Dictionary<int, UserDto>>();
            
            var users = new UsersData {
                Id = 1,
                UsersList = new List<User> {
                    new User {
                        FirstName = "John",
                        LastName = "Doe",
                        Car = new Car { Name = "BMW X6", Age = 3 }
                    }
                }
            };

            var dtoUsers = users.ConvertTo<UsersDto>();
            dtoUsers.PrintDump();

            Assert.That(dtoUsers.Id, Is.EqualTo(users.Id));
            Assert.That(dtoUsers.UsersList, Is.Empty);
            Assert.That(dtoUsers.UsersMap, Is.Empty);
           
            AutoMappingUtils.Reset();
        }

    }
}