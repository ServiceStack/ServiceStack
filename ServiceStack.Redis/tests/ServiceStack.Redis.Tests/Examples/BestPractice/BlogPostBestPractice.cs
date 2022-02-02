using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Examples.BestPractice
{

    /// <summary>
    /// A complete, self-contained example showing how to create a basic blog application using Redis.
    /// </summary>

    #region Blog Models	

    public class User
        : IHasBlogRepository
    {
        public IBlogRepository Repository { private get; set; }

        public User()
        {
            this.BlogIds = new List<long>();
        }

        public long Id { get; set; }
        public string Name { get; set; }
        public List<long> BlogIds { get; set; }

        public List<Blog> GetBlogs()
        {
            return this.Repository.GetBlogs(this.BlogIds);
        }

        public Blog CreateNewBlog(Blog blog)
        {
            this.Repository.StoreBlogs(this, blog);

            return blog;
        }
    }

    public class Blog
        : IHasBlogRepository
    {
        public IBlogRepository Repository { private get; set; }

        public Blog()
        {
            this.Tags = new List<string>();
            this.BlogPostIds = new List<long>();
        }

        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public List<string> Tags { get; set; }
        public List<long> BlogPostIds { get; set; }

        public List<BlogPost> GetBlogPosts()
        {
            return this.Repository.GetBlogPosts(this.BlogPostIds);
        }

        public void StoreNewBlogPosts(params BlogPost[] blogPosts)
        {
            this.Repository.StoreNewBlogPosts(this, blogPosts);
        }
    }

    public class BlogPost
    {
        public BlogPost()
        {
            this.Categories = new List<string>();
            this.Tags = new List<string>();
            this.Comments = new List<BlogPostComment>();
        }

        public long Id { get; set; }
        public long BlogId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public List<string> Categories { get; set; }
        public List<string> Tags { get; set; }
        public List<BlogPostComment> Comments { get; set; }
    }

    public class BlogPostComment
    {
        public string Content { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    #endregion


    #region Blog Repository
    public interface IHasBlogRepository
    {
        IBlogRepository Repository { set; }
    }

    public class BlogRepository
        : IBlogRepository
    {
        const string CategoryTypeName = "Category";
        const string TagCloudKey = "urn:TagCloud";
        const string AllCategoriesKey = "urn:Categories";
        const string RecentBlogPostsKey = "urn:BlogPosts:RecentPosts";
        const string RecentBlogPostCommentsKey = "urn:BlogPostComment:RecentComments";

        public BlogRepository(IRedisClient client)
        {
            this.redis = client;
        }

        private readonly IRedisClient redis;

        public void StoreUsers(params User[] users)
        {
            var redisUsers = redis.As<User>();
            Inject(users);
            users.Where(x => x.Id == default(int))
                .Each(x => x.Id = redisUsers.GetNextSequence());

            redisUsers.StoreAll(users);
        }

        public List<User> GetAllUsers()
        {
            var redisUsers = redis.As<User>();
            return Inject(redisUsers.GetAll());
        }

        public void StoreBlogs(User user, params Blog[] blogs)
        {
            var redisBlogs = redis.As<Blog>();
            foreach (var blog in blogs)
            {
                blog.Id = blog.Id != default(int) ? blog.Id : redisBlogs.GetNextSequence();
                blog.UserId = user.Id;
                blog.UserName = user.Name;

                user.BlogIds.AddIfNotExists(blog.Id);
            }

            using (var trans = redis.CreateTransaction())
            {
                trans.QueueCommand(x => x.Store(user));
                trans.QueueCommand(x => x.StoreAll(blogs));

                trans.Commit();
            }

            Inject(blogs);
        }

        public List<Blog> GetBlogs(IEnumerable<long> blogIds)
        {
            var redisBlogs = redis.As<Blog>();
            return Inject(
                redisBlogs.GetByIds(blogIds.Map(x => x.ToString())));
        }

        public List<Blog> GetAllBlogs()
        {
            var redisBlogs = redis.As<Blog>();
            return Inject(redisBlogs.GetAll());
        }

        public List<BlogPost> GetBlogPosts(IEnumerable<long> blogPostIds)
        {
            var redisBlogPosts = redis.As<BlogPost>();
            return redisBlogPosts.GetByIds(blogPostIds.Map(x => x.ToString())).ToList();
        }

        public void StoreNewBlogPosts(Blog blog, params BlogPost[] blogPosts)
        {
            var redisBlogPosts = redis.As<BlogPost>();
            var redisComments = redis.As<BlogPostComment>();

            //Get wrapper around a strongly-typed Redis server-side List
            var recentPosts = redisBlogPosts.Lists[RecentBlogPostsKey];
            var recentComments = redisComments.Lists[RecentBlogPostCommentsKey];

            foreach (var blogPost in blogPosts)
            {
                blogPost.Id = blogPost.Id != default(int) ? blogPost.Id : redisBlogPosts.GetNextSequence();
                blogPost.BlogId = blog.Id;
                blog.BlogPostIds.AddIfNotExists(blogPost.Id);

                //List of Recent Posts and comments
                recentPosts.Prepend(blogPost);
                blogPost.Comments.ForEach(recentComments.Prepend);

                //Tag Cloud
                blogPost.Tags.ForEach(x =>
                    redis.IncrementItemInSortedSet(TagCloudKey, x, 1));

                //List of all post categories
                blogPost.Categories.ForEach(x =>
                      redis.AddItemToSet(AllCategoriesKey, x));

                //Map of Categories to BlogPost Ids
                blogPost.Categories.ForEach(x =>
                      redis.AddItemToSet(UrnId.Create(CategoryTypeName, x), blogPost.Id.ToString()));
            }

            //Rolling list of recent items, only keep the last 5
            recentPosts.Trim(0, 4);
            recentComments.Trim(0, 4);

            using (var trans = redis.CreateTransaction())
            {
                trans.QueueCommand(x => x.Store(blog));
                trans.QueueCommand(x => x.StoreAll(blogPosts));

                trans.Commit();
            }
        }

        public List<BlogPost> GetRecentBlogPosts()
        {
            var redisBlogPosts = redis.As<BlogPost>();
            return redisBlogPosts.Lists[RecentBlogPostsKey].GetAll();
        }

        public List<BlogPostComment> GetRecentBlogPostComments()
        {
            var redisComments = redis.As<BlogPostComment>();
            return redisComments.Lists[RecentBlogPostCommentsKey].GetAll();
        }

        public IDictionary<string, double> GetTopTags(int take)
        {
            return redis.GetRangeWithScoresFromSortedSetDesc(TagCloudKey, 0, take - 1);
        }

        public HashSet<string> GetAllCategories()
        {
            return redis.GetAllItemsFromSet(AllCategoriesKey);
        }

        public void StoreBlogPost(BlogPost blogPost)
        {
            redis.Store(blogPost);
        }

        public BlogPost GetBlogPost(int postId)
        {
            return redis.GetById<BlogPost>(postId);
        }

        public List<BlogPost> GetBlogPostsByCategory(string categoryName)
        {
            var categoryUrn = UrnId.Create(CategoryTypeName, categoryName);
            var documentDbPostIds = redis.GetAllItemsFromSet(categoryUrn);

            return redis.GetByIds<BlogPost>(documentDbPostIds.ToArray()).ToList();
        }

        public List<T> Inject<T>(IEnumerable<T> entities)
            where T : IHasBlogRepository
        {
            var entitiesList = entities.ToList();
            entitiesList.ForEach(x => x.Repository = this);
            return entitiesList;
        }

    }
    #endregion


    [TestFixture, Ignore("Integration"), Category("Integration")]
    public class BlogPostBestPractice
    {
        readonly RedisClient redisClient = new RedisClient(TestConfig.SingleHost);
        private IBlogRepository repository;

        [SetUp]
        public void OnBeforeEachTest()
        {
            redisClient.FlushAll();
            repository = new BlogRepository(redisClient);

            InsertTestData(repository);
        }

        public static void InsertTestData(IBlogRepository repository)
        {
            var ayende = new User { Name = "ayende" };
            var mythz = new User { Name = "mythz" };

            repository.StoreUsers(ayende, mythz);

            var ayendeBlog = ayende.CreateNewBlog(new Blog { Tags = { "Architecture", ".NET", "Databases" } });

            var mythzBlog = mythz.CreateNewBlog(new Blog { Tags = { "Architecture", ".NET", "Databases" } });

            ayendeBlog.StoreNewBlogPosts(new BlogPost
            {
                Title = "RavenDB",
                Categories = new List<string> { "NoSQL", "DocumentDB" },
                Tags = new List<string> { "Raven", "NoSQL", "JSON", ".NET" },
                Comments = new List<BlogPostComment>
                    {
                        new BlogPostComment { Content = "First Comment!", CreatedDate = DateTime.UtcNow,},
                        new BlogPostComment { Content = "Second Comment!", CreatedDate = DateTime.UtcNow,},
                    }
            },
                new BlogPost
                {
                    BlogId = ayendeBlog.Id,
                    Title = "Cassandra",
                    Categories = new List<string> { "NoSQL", "Cluster" },
                    Tags = new List<string> { "Cassandra", "NoSQL", "Scalability", "Hashing" },
                    Comments = new List<BlogPostComment>
                    {
                        new BlogPostComment { Content = "First Comment!", CreatedDate = DateTime.UtcNow,}
                    }
                });

            mythzBlog.StoreNewBlogPosts(
                new BlogPost
                {
                    Title = "Redis",
                    Categories = new List<string> { "NoSQL", "Cache" },
                    Tags = new List<string> { "Redis", "NoSQL", "Scalability", "Performance" },
                    Comments = new List<BlogPostComment>
                    {
                        new BlogPostComment { Content = "First Comment!", CreatedDate = DateTime.UtcNow,}
                    }
                },
                new BlogPost
                {
                    Title = "Couch Db",
                    Categories = new List<string> { "NoSQL", "DocumentDB" },
                    Tags = new List<string> { "CouchDb", "NoSQL", "JSON" },
                    Comments = new List<BlogPostComment>
                    {
                        new BlogPostComment {Content = "First Comment!", CreatedDate = DateTime.UtcNow,}
                    }
                });
        }

        [Test]
        public void View_test_data()
        {
            var mythz = repository.GetAllUsers().First(x => x.Name == "mythz");
            var mythzBlogPostIds = mythz.GetBlogs().SelectMany(x => x.BlogPostIds);
            var mythzBlogPosts = repository.GetBlogPosts(mythzBlogPostIds);

            Debug.WriteLine(mythzBlogPosts.Dump());
            /* Output:
			[
				{
					Id: 3,
					BlogId: 2,
					Title: Redis,
					Categories: 
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
							CreatedDate: 2010-04-26T02:24:47.516949Z
						}
					]
				},
				{
					Id: 4,
					BlogId: 2,
					Title: Couch Db,
					Categories: 
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
							CreatedDate: 2010-04-26T02:24:47.516949Z
						}
					]
				}
			]			 
			*/
        }

        [Test]
        public void Show_a_list_of_blogs()
        {
            var blogs = repository.GetAllBlogs();
            Debug.WriteLine(blogs.Dump());
            /* Output: 
			[
				{
					Id: 1,
					UserId: 1,
					UserName: ayende,
					Tags: 
					[
						Architecture,
						.NET,
						Databases
					],
					BlogPostIds: 
					[
						1,
						2
					]
				},
				{
					Id: 2,
					UserId: 2,
					UserName: mythz,
					Tags: 
					[
						Architecture,
						.NET,
						Databases
					],
					BlogPostIds: 
					[
						3,
						4
					]
				}
			]
			*/
        }

        [Test]
        public void Show_a_list_of_recent_posts_and_comments()
        {
            //Recent posts are already maintained in the repository
            var recentPosts = repository.GetRecentBlogPosts();
            var recentComments = repository.GetRecentBlogPostComments();

            Debug.WriteLine("Recent Posts:\n" + recentPosts.Dump());
            Debug.WriteLine("Recent Comments:\n" + recentComments.Dump());
            /* 
			Recent Posts:
			[
				{
					Id: 4,
					BlogId: 2,
					Title: Couch Db,
					Categories: 
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
							CreatedDate: 2010-04-26T02:25:39.7419361Z
						}
					]
				},
				{
					Id: 3,
					BlogId: 2,
					Title: Redis,
					Categories: 
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
							CreatedDate: 2010-04-26T02:25:39.7419361Z
						}
					]
				},
				{
					Id: 2,
					BlogId: 1,
					Title: Cassandra,
					Categories: 
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
							CreatedDate: 2010-04-26T02:25:39.7039339Z
						}
					]
				},
				{
					Id: 1,
					BlogId: 1,
					Title: RavenDB,
					Categories: 
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
							CreatedDate: 2010-04-26T02:25:39.7039339Z
						},
						{
							Content: Second Comment!,
							CreatedDate: 2010-04-26T02:25:39.7039339Z
						}
					]
				}
			]

			Recent Comments:
			[
				{
					Content: First Comment!,
					CreatedDate: 2010-04-26T02:25:39.7419361Z
				},
				{
					Content: First Comment!,
					CreatedDate: 2010-04-26T02:25:39.7419361Z
				},
				{
					Content: First Comment!,
					CreatedDate: 2010-04-26T02:25:39.7039339Z
				},
				{
					Content: Second Comment!,
					CreatedDate: 2010-04-26T02:25:39.7039339Z
				},
				{
					Content: First Comment!,
					CreatedDate: 2010-04-26T02:25:39.7039339Z
				}
			]
			 
			 */
        }

        [Test]
        public void Show_a_TagCloud()
        {
            //Tags are maintained in the repository
            var tagCloud = repository.GetTopTags(5);
            Debug.WriteLine(tagCloud.Dump());
            /* Output:
			[
				[
					NoSQL,
					 4
				],
				[
					Scalability,
					 2
				],
				[
					JSON,
					 2
				],
				[
					Redis,
					 1
				],
				[
					Raven,
					 1
				]
			]
			 */
        }

        [Test]
        public void Show_all_Categories()
        {
            //Categories are maintained in the repository
            var allCategories = repository.GetAllCategories();
            Debug.WriteLine(allCategories.Dump());
            /* Output:
			[
				DocumentDB,
				NoSQL,
				Cluster,
				Cache
			]
			 */
        }

        [Test]
        public void Show_post_and_all_comments()
        {
            var postId = 1;
            var blogPost = repository.GetBlogPost(postId);
            Debug.WriteLine(blogPost.Dump());
            /* Output:
			{
				Id: 1,
				BlogId: 1,
				Title: RavenDB,
				Categories: 
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
						CreatedDate: 2010-04-26T02:00:24.5982749Z
					},
					{
						Content: Second Comment!,
						CreatedDate: 2010-04-26T02:00:24.5982749Z
					}
				]
			}
			*/
        }

        [Test]
        public void Add_comment_to_existing_post()
        {
            var postId = 1;
            var blogPost = repository.GetBlogPost(postId);

            blogPost.Comments.Add(
                new BlogPostComment { Content = "Third Comment!", CreatedDate = DateTime.UtcNow });

            repository.StoreBlogPost(blogPost);

            var refreshBlogPost = repository.GetBlogPost(postId);
            Debug.WriteLine(refreshBlogPost.Dump());
            /* Output:
			{
				Id: 1,
				BlogId: 1,
				Title: RavenDB,
				Categories: 
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
						CreatedDate: 2010-04-26T02:08:13.5580978Z
					},
					{
						Content: Second Comment!,
						CreatedDate: 2010-04-26T02:08:13.5580978Z
					},
					{
						Content: Third Comment!,
						CreatedDate: 2010-04-26T02:08:13.6871052Z
					}
				]
			}
			 */
        }

        [Test]
        public void Show_all_Posts_for_a_Category()
        {
            var documentDbPosts = repository.GetBlogPostsByCategory("DocumentDB");
            Debug.WriteLine(documentDbPosts.Dump());
            /* Output:
			[
				{
					Id: 4,
					BlogId: 2,
					Title: Couch Db,
					Categories: 
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
							CreatedDate: 2010-04-26T02:16:08.0332362Z
						}
					]
				},
				{
					Id: 1,
					BlogId: 1,
					Title: RavenDB,
					Categories: 
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
							CreatedDate: 2010-04-26T02:16:07.9662324Z
						},
						{
							Content: Second Comment!,
							CreatedDate: 2010-04-26T02:16:07.9662324Z
						}
					]
				}
			]
			*/
        }

    }
}