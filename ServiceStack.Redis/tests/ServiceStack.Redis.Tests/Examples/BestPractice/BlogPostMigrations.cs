using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Text;

//Shows how to easily migrated data from an old version of the schema to a new one

//New schema types used in this example
namespace ServiceStack.Redis.Tests.Examples.BestPractice.New
{
    public class BlogPost
    {
        public BlogPost()
        {
            this.Labels = new List<string>();
            this.Tags = new HashSet<string>();
            this.Comments = new List<Dictionary<string, string>>();
        }

        //Changed int types to both a long and a double type
        public long Id { get; set; }
        public double BlogId { get; set; }

        //Added new field
        public BlogPostType PostType { get; set; }

        public string Title { get; set; }
        public string Content { get; set; }

        //Renamed from 'Categories' to 'Labels'
        public List<string> Labels { get; set; }

        //Changed from List to a HashSet
        public HashSet<string> Tags { get; set; }

        //Changed from List of strongly-typed 'BlogPostComment' to loosely-typed string map
        public List<Dictionary<string, string>> Comments { get; set; }

        //Added pointless calculated field
        public int? NoOfComments { get; set; }
    }

    public enum BlogPostType
    {
        None,
        Article,
        Summary,
    }
}

namespace ServiceStack.Redis.Tests.Examples.BestPractice
{
    [TestFixture, Ignore("Integration"), Category("Integration")]
    public class BlogPostMigrations
    {
        readonly RedisClient redisClient = new RedisClient(TestConfig.SingleHost);

        [SetUp]
        public void OnBeforeEachTest()
        {
            redisClient.FlushAll();
        }

        [Test]
        public void Automatically_migrate_to_new_Schema()
        {
            var repository = new BlogRepository(redisClient);

            //Populate the datastore with the old schema from the 'BlogPostBestPractice'
            BlogPostBestPractice.InsertTestData(repository);

            //Create a typed-client based on the new schema
            var redisBlogPosts = redisClient.As<New.BlogPost>();
            //Automatically retrieve blog posts
            IList<New.BlogPost> allBlogPosts = redisBlogPosts.GetAll();

            //Print out the data in the list of 'New.BlogPost' populated from old 'BlogPost' type
            //Note: renamed fields are lost 
            allBlogPosts.PrintDump();
            /*Output:
            [
                {
                    Id: 3,
                    BlogId: 2,
                    PostType: None,
                    Title: Redis,
                    Labels: [],
                    Tags: 
                    [
                        Redis,
                        NoSQL,
                        Scalability,
                        Performance
                    ],
                    Comments: 
                    [
                        {
                            Content: First Comment!,
                            CreatedDate: 2010-04-28T21:42:03.9484725Z
                        }
                    ]
                },
                {
                    Id: 4,
                    BlogId: 2,
                    PostType: None,
                    Title: Couch Db,
                    Labels: [],
                    Tags: 
                    [
                        CouchDb,
                        NoSQL,
                        JSON
                    ],
                    Comments: 
                    [
                        {
                            Content: First Comment!,
                            CreatedDate: 2010-04-28T21:42:03.9484725Z
                        }
                    ]
                },
                {
                    Id: 1,
                    BlogId: 1,
                    PostType: None,
                    Title: RavenDB,
                    Labels: [],
                    Tags: 
                    [
                        Raven,
                        NoSQL,
                        JSON,
                        .NET
                    ],
                    Comments: 
                    [
                        {
                            Content: First Comment!,
                            CreatedDate: 2010-04-28T21:42:03.9004697Z
                        },
                        {
                            Content: Second Comment!,
                            CreatedDate: 2010-04-28T21:42:03.9004697Z
                        }
                    ]
                },
                {
                    Id: 2,
                    BlogId: 1,
                    PostType: None,
                    Title: Cassandra,
                    Labels: [],
                    Tags: 
                    [
                        Cassandra,
                        NoSQL,
                        Scalability,
                        Hashing
                    ],
                    Comments: 
                    [
                        {
                            Content: First Comment!,
                            CreatedDate: 2010-04-28T21:42:03.9004697Z
                        }
                    ]
                }
            ]

             */
        }

        [Test]
        public void Manually_migrate_to_new_Schema_using_a_custom_tranlation()
        {
            var repository = new BlogRepository(redisClient);

            //Populate the datastore with the old schema from the 'BlogPostBestPractice'
            BlogPostBestPractice.InsertTestData(repository);

            //Create a typed-client based on the new schema
            var redisBlogPosts = redisClient.As<BlogPost>();
            var redisNewBlogPosts = redisClient.As<New.BlogPost>();
            //Automatically retrieve blog posts
            IList<BlogPost> oldBlogPosts = redisBlogPosts.GetAll();

            //Write a custom translation layer to migrate to the new schema
            var migratedBlogPosts = oldBlogPosts.Map(old => new New.BlogPost
            {
                Id = old.Id,
                BlogId = old.BlogId,
                Title = old.Title,
                Content = old.Content,
                Labels = old.Categories, //populate with data from renamed field
                PostType = New.BlogPostType.Article, //select non-default enum value
                Tags = new HashSet<string>(old.Tags),
                Comments = old.Comments.ConvertAll(x => new Dictionary<string, string> { { "Content", x.Content }, { "CreatedDate", x.CreatedDate.ToString() }, }),
                NoOfComments = old.Comments.Count, //populate using logic from old data
            });

            //Persist the new migrated blogposts 
            redisNewBlogPosts.StoreAll(migratedBlogPosts);

            //Read out the newly stored blogposts
            var refreshedNewBlogPosts = redisNewBlogPosts.GetAll();
            //Note: data renamed fields are successfully migrated to the new schema
            refreshedNewBlogPosts.PrintDump();
            /*
            [
                {
                    Id: 3,
                    BlogId: 2,
                    PostType: Article,
                    Title: Redis,
                    Labels: 
                    [
                        NoSQL,
                        Cache
                    ],
                    Tags: 
                    [
                        Redis,
                        NoSQL,
                        Scalability,
                        Performance
                    ],
                    Comments: 
                    [
                        {
                            Content: First Comment!,
                            CreatedDate: 28/04/2010 22:58:35
                        }
                    ],
                    NoOfComments: 1
                },
                {
                    Id: 4,
                    BlogId: 2,
                    PostType: Article,
                    Title: Couch Db,
                    Labels: 
                    [
                        NoSQL,
                        DocumentDB
                    ],
                    Tags: 
                    [
                        CouchDb,
                        NoSQL,
                        JSON
                    ],
                    Comments: 
                    [
                        {
                            Content: First Comment!,
                            CreatedDate: 28/04/2010 22:58:35
                        }
                    ],
                    NoOfComments: 1
                },
                {
                    Id: 1,
                    BlogId: 1,
                    PostType: Article,
                    Title: RavenDB,
                    Labels: 
                    [
                        NoSQL,
                        DocumentDB
                    ],
                    Tags: 
                    [
                        Raven,
                        NoSQL,
                        JSON,
                        .NET
                    ],
                    Comments: 
                    [
                        {
                            Content: First Comment!,
                            CreatedDate: 28/04/2010 22:58:35
                        },
                        {
                            Content: Second Comment!,
                            CreatedDate: 28/04/2010 22:58:35
                        }
                    ],
                    NoOfComments: 2
                },
                {
                    Id: 2,
                    BlogId: 1,
                    PostType: Article,
                    Title: Cassandra,
                    Labels: 
                    [
                        NoSQL,
                        Cluster
                    ],
                    Tags: 
                    [
                        Cassandra,
                        NoSQL,
                        Scalability,
                        Hashing
                    ],
                    Comments: 
                    [
                        {
                            Content: First Comment!,
                            CreatedDate: 28/04/2010 22:58:35
                        }
                    ],
                    NoOfComments: 1
                }
            ]
            */
        }

    }

}
