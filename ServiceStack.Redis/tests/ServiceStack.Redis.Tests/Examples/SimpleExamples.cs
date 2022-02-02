using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Examples
{
    [TestFixture, Ignore("Integration"), Category("Integration")]
    public class SimpleExamples
    {
        readonly RedisClient redis = new RedisClient(TestConfig.SingleHost);

        [SetUp]
        public void OnBeforeEachTest()
        {
            redis.FlushAll();
        }

        [Test]
        public void Store_and_retrieve_users()
        {
            var redisUsers = redis.As<User>();
            redisUsers.Store(new User { Id = redisUsers.GetNextSequence(), Name = "ayende" });
            redisUsers.Store(new User { Id = redisUsers.GetNextSequence(), Name = "mythz" });

            var allUsers = redisUsers.GetAll();
            allUsers.PrintDump();
            /*Output
            [
                {
                    Id: 1,
                    Name: ayende,
                    BlogIds: []
                },
                {
                    Id: 2,
                    Name: mythz,
                    BlogIds: []
                }
            ]
            */
        }

        [Test]
        public void Store_and_retrieve_some_blogs()
        {
            //Retrieve strongly-typed Redis clients that let's you natively persist POCO's
            var redisUsers = redis.As<User>();
            var redisBlogs = redis.As<Blog>();
            //Create the user, getting a unique User Id from the User sequence.
            var mythz = new User { Id = redisUsers.GetNextSequence(), Name = "Demis Bellot" };

            //create some blogs using unique Ids from the Blog sequence. Also adding references
            var mythzBlogs = new List<Blog>
            {
                new Blog
                {
                    Id = redisBlogs.GetNextSequence(),
                    UserId = mythz.Id,
                    UserName = mythz.Name,
                    Tags = new List<string> { "Architecture", ".NET", "Redis" },
                },
                new Blog
                {
                    Id = redisBlogs.GetNextSequence(),
                    UserId = mythz.Id,
                    UserName = mythz.Name,
                    Tags = new List<string> { "Music", "Twitter", "Life" },
                },
            };
            //Add the blog references
            mythzBlogs.ForEach(x => mythz.BlogIds.Add(x.Id));

            //Store the user and their blogs
            redisUsers.Store(mythz);
            redisBlogs.StoreAll(mythzBlogs);

            //retrieve all blogs
            var blogs = redisBlogs.GetAll();

            //Recursively print the values of the POCO (For T.Dump() Extension method see: http://mono.servicestack.net/mythz_blog/?p=202)
            blogs.PrintDump();

            /*Output
            [
                {
                    Id: 1,
                    UserId: 1,
                    UserName: Demis Bellot,
                    Tags: 
                    [
                        Architecture,
                        .NET,
                        Redis
                    ],
                    BlogPostIds: []
                },
                {
                    Id: 2,
                    UserId: 1,
                    UserName: Demis Bellot,
                    Tags: 
                    [
                        Music,
                        Twitter,
                        Life
                    ],
                    BlogPostIds: []
                }
            ]
             */
        }

    }
}
