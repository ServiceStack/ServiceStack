using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common.Extensions;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Examples.BestPractice
{

	/// <summary>
	/// A complete, self-contained example showing how to create a basic blog application using Redis.
	/// </summary>

	public class User
		: IHasBlogRepository
	{
		public User()
		{
			this.BlogIds = new List<int>();
		}

		public IBlogRepository Repository { private get; set; }

		public int Id { get; set; }
		public string Name { get; set; }
		public List<int> BlogIds { get; set; }

		public List<Blog> GetBlogs()
		{
			return this.Repository.GetBlogs(this.BlogIds);
		}

		public Blog CreateNewBlog(IEnumerable<string> tags)
		{
			var newBlog = new Blog { UserId = this.Id, UserName = this.Name, Tags = tags.ToList() };
			this.Repository.StoreBlogs(newBlog);
			this.BlogIds.Add(newBlog.Id);
			this.Repository.StoreUsers(this);

			return newBlog;
		}
	}

	public class Blog
		: IHasBlogRepository
	{
		public Blog()
		{
			this.Tags = new List<string>();
			this.BlogPostIds = new List<int>();
		}

		public IBlogRepository Repository { private get; set; }

		public int Id { get; set; }
		public int UserId { get; set; }
		public string UserName { get; set; }
		public List<string> Tags { get; set; }
		public List<int> BlogPostIds { get; set; }

		public List<BlogPost> GetBlogPosts()
		{
			return this.Repository.GetBlogPosts(this.BlogPostIds);
		}

		public BlogPost CreateNewBlogPost(BlogPost newPost)
		{
			newPost.BlogId = this.Id;

			this.Repository.StoreBlogPosts(newPost);

			this.BlogPostIds.Add(newPost.Id);
			this.Repository.StoreBlogPosts(newPost);
			this.Repository.StoreBlogs(this);

			return newPost;
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

		public int Id { get; set; }
		public int BlogId { get; set; }
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

	public interface IHasBlogRepository
	{
		IBlogRepository Repository { set; }
	}

	public interface IBlogRepository
	{
		void StoreUsers(params User[] users);
		List<User> GetAllUsers();

		void StoreBlogs(params Blog[] users);
		List<Blog> GetBlogs(IEnumerable<int> blogIds);

		List<BlogPost> GetBlogPosts(IEnumerable<int> blogPostIds);
		void StoreBlogPosts(params BlogPost[] blogPosts);
	}

	public class BlogRepository
		: IBlogRepository
	{
		public BlogRepository(IRedisClient client)
		{
			this.client = client;
		}

		private readonly IRedisClient client;

		public void StoreUsers(params User[] users)
		{
			using (var userClient = client.GetTypedClient<User>())
			{
				Inject(users);
				users.Where(x => x.Id == default(int))
					.ForEach(x => x.Id = userClient.GetNextSequence());

				userClient.StoreAll(users);
			}
		}

		public List<User> GetAllUsers()
		{
			using (var userClient = client.GetTypedClient<User>())
			{
				return Inject(userClient.GetAll());
			}
		}

		public void StoreBlogs(params Blog[] blogs)
		{
			using (var blogsClient = client.GetTypedClient<Blog>())
			{
				Inject(blogs);
				blogs.Where(x => x.Id == default(int))
					.ForEach(x => x.Id = blogsClient.GetNextSequence());

				blogsClient.StoreAll(blogs);
			}
		}

		public List<Blog> GetBlogs(IEnumerable<int> blogIds)
		{
			using (var blogClient = client.GetTypedClient<Blog>())
			{
				return Inject(
					blogClient.GetByIds(blogIds.ConvertAll(x => x.ToString())));
			}
		}

		public List<BlogPost> GetBlogPosts(IEnumerable<int> blogPostIds)
		{
			using (var blogPostClient = client.GetTypedClient<BlogPost>())
			{
				return blogPostClient.GetByIds(blogPostIds.ConvertAll(x => x.ToString())).ToList();
			}
		}

		public void StoreBlogPosts(params BlogPost[] blogPosts)
		{
			using (var blogPostsClient = client.GetTypedClient<BlogPost>())
			{
				blogPosts.Where(x => x.Id == default(int))
					.ForEach(x => x.Id = blogPostsClient.GetNextSequence());

				blogPostsClient.StoreAll(blogPosts);
			}
		}

		public List<T> Inject<T>(IEnumerable<T> entities)
			where T : IHasBlogRepository
		{
			var entitiesList = entities.ToList();
			entitiesList.ForEach(x => x.Repository = this);
			return entitiesList;
		}

	}

	[TestFixture]
	public class BlogPostExample
	{
		readonly RedisClient redisClient = new RedisClient(TestConfig.SingleHost);
		private IBlogRepository repository;

		[SetUp]
		public void OnBeforeEachTest()
		{
			redisClient.FlushAll();
			repository = new BlogRepository(redisClient);

			InsertTestData();
		}

		public void InsertTestData()
		{
			var ayende = new User { Name = "Oren Eini" };
			var mythz = new User { Name = "Demis Bellot" };

			repository.StoreUsers(ayende, mythz);

			var ayendeBlog = ayende.CreateNewBlog(new[] { "Architecture", ".NET", "Databases" });

			var mythzBlog = mythz.CreateNewBlog(new[] { "Architecture", ".NET", "Databases" });

			ayendeBlog.CreateNewBlogPost(new BlogPost
				{
					Title = "RavenDB",
					Categories = new List<string> { "NoSQL", "DocumentDB" },
					Tags = new List<string> { "Raven", "NoSQL", "JSON", ".NET" },
					Comments = new List<BlogPostComment>
					{
						new BlogPostComment { Content = "First Comment!", CreatedDate = DateTime.UtcNow,},
						new BlogPostComment { Content = "Second Comment!", CreatedDate = DateTime.UtcNow,},
					}
				});

			mythzBlog.CreateNewBlogPost(new BlogPost
				{
					Title = "Redis",
					Categories = new List<string> { "NoSQL", "Cache" },
					Tags = new List<string> { "Redis", "NoSQL", "Scalability", "Performance" },
					Comments = new List<BlogPostComment>
					{
						new BlogPostComment { Content = "First Comment!", CreatedDate = DateTime.UtcNow,}
					}
				});

			ayendeBlog.CreateNewBlogPost(new BlogPost
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

			mythzBlog.CreateNewBlogPost(new BlogPost
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
			var ayende = repository.GetAllUsers().First(x => x.Name == "Oren Eini");
			var ayendeBlogPostIds = ayende.GetBlogs().SelectMany(x => x.BlogPostIds);
			var ayendeBlogPosts = repository.GetBlogPosts(ayendeBlogPostIds);
			
			Console.WriteLine(ayendeBlogPosts.Dump());			
		}

		[Test]
		public void Show_a_list_of_blogs()
		{
			using (var redisBlogs = redisClient.GetTypedClient<Blog>())
			{
				var blogs = redisBlogs.GetAll();
				Console.WriteLine(blogs.Dump());
				/* Output: 
				[
					{
						Id: 1,
						UserId: 1,
						UserName: Ayende,
						Tags: 
						[
							Architecture,
							.NET,
							Databases
						],
						BlogPostIds: 
						[
							1,
							3
						]
					},
					{
						Id: 2,
						UserId: 2,
						UserName: Demis,
						Tags: 
						[
							Architecture,
							.NET,
							Databases
						],
						BlogPostIds: 
						[
							2,
							4
						]
					}
				]
				 */
			}
		}

		[Test]
		public void Show_a_list_of_recent_posts_and_comments()
		{
			using (var redisBlogPosts = redisClient.GetTypedClient<BlogPost>())
			using (var redisComments = redisClient.GetTypedClient<BlogPostComment>())
			{
				var blogPosts = redisBlogPosts.GetAll();

				var recentPosts = redisBlogPosts.Lists["urn:BlogPosts:RecentPosts"];
				var recentComments = redisComments.Lists["urn:BlogPostComment:RecentComments"];

				foreach (var blogPost in blogPosts)
				{
					recentPosts.Prepend(blogPost);

					blogPost.Comments.ForEach(recentComments.Prepend);
				}
				//Rolling list only keep the last 3
				recentPosts.Trim(0, 2);
				recentComments.Trim(0, 2);

				//Print out the last 3 posts:
				Console.WriteLine(recentPosts.GetAll().Dump());
				/* Output: 
				[
					{
						Id: 2,
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
								CreatedDate: 2010-04-20T22:14:02.755878Z
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
								CreatedDate: 2010-04-20T22:14:02.755878Z
							},
							{
								Content: Second Comment!,
								CreatedDate: 2010-04-20T22:14:02.755878Z
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
								CreatedDate: 2010-04-20T22:14:02.755878Z
							}
						]
					}
				]
				*/

				Console.WriteLine(recentComments.GetAll().Dump());
				/* Output:
				[
					{
						Content: First Comment!,
						CreatedDate: 2010-04-20T20:32:42.2970956Z
					},
					{
						Content: First Comment!,
						CreatedDate: 2010-04-20T20:32:42.2970956Z
					},
					{
						Content: First Comment!,
						CreatedDate: 2010-04-20T20:32:42.2970956Z
					}
				]
				 */
			}
		}

		[Test]
		public void Show_a_TagCloud()
		{
			using (var redisBlogPosts = redisClient.GetTypedClient<BlogPost>())
			{
				var blogPosts = redisBlogPosts.GetAll();

				foreach (var blogPost in blogPosts)
				{
					blogPost.Tags.ForEach(x =>
						redisClient.IncrementItemInSortedSet("urn:TagCloud", x, 1));
				}

				//Show top 5 most popular tags with their scores
				var tagCloud = redisClient.GetRangeWithScoresFromSortedSetDesc("urn:TagCloud", 0, 4);
				Console.WriteLine(tagCloud.Dump());
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
					],
				]
				 */
			}
		}

		[Test]
		public void Show_all_Categories()
		{
			using (var redisBlogPosts = redisClient.GetTypedClient<BlogPost>())
			{
				var blogPosts = redisBlogPosts.GetAll();

				foreach (var blogPost in blogPosts)
				{
					blogPost.Categories.ForEach(x =>
						  redisClient.AddToSet("urn:Categories", x));
				}

				var uniqueCategories = redisClient.GetAllFromSet("urn:Categories");
				Console.WriteLine(uniqueCategories.Dump());
				/* Output:
				[
					DocumentDB,
					NoSQL,
					Cluster,
					Cache
				]
				 */
			}
		}

		[Test]
		public void Show_post_and_all_comments()
		{
			var postId = 1;
			using (var redisBlogPosts = redisClient.GetTypedClient<BlogPost>())
			{
				var blogPost = redisBlogPosts.GetById(postId.ToString());

				Console.WriteLine(blogPost.Dump());
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
							CreatedDate: 2010-04-20T21:26:31.9918236Z
						},
						{
							Content: Second Comment!,
							CreatedDate: 2010-04-20T21:26:31.9918236Z
						}
					]
				}
				*/
			}
		}

		[Test]
		public void Add_comment_to_existing_post()
		{
			var postId = 1;
			using (var redisBlogPosts = redisClient.GetTypedClient<BlogPost>())
			{
				var blogPost = redisBlogPosts.GetById(postId.ToString());
				blogPost.Comments.Add(
					new BlogPostComment { Content = "Third Post!", CreatedDate = DateTime.UtcNow });
				redisBlogPosts.Store(blogPost);

				var refreshBlogPost = redisBlogPosts.GetById(postId.ToString());
				Console.WriteLine(refreshBlogPost.Dump());
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
							CreatedDate: 2010-04-20T21:32:39.9688707Z
						},
						{
							Content: Second Comment!,
							CreatedDate: 2010-04-20T21:32:39.9688707Z
						},
						{
							Content: Third Post!,
							CreatedDate: 2010-04-20T21:32:40.2688879Z
						}
					]
				}
				*/
			}
		}

		[Test]
		public void Show_all_Posts_for_a_Category()
		{
			using (var redisBlogPosts = redisClient.GetTypedClient<BlogPost>())
			{
				var blogPosts = redisBlogPosts.GetAll();

				foreach (var blogPost in blogPosts)
				{
					blogPost.Categories.ForEach(x =>
						  redisClient.AddToSet("urn:Category:" + x, blogPost.Id.ToString()));
				}
				var documentDbPostIds = redisClient.GetAllFromSet("urn:Category:DocumentDB");

				var documentDbPosts = redisBlogPosts.GetByIds(documentDbPostIds);

				Console.WriteLine(documentDbPosts.Dump());
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
								CreatedDate: 2010-04-20T21:38:24.6305842Z
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
								CreatedDate: 2010-04-20T21:38:24.6295842Z
							},
							{
								Content: Second Comment!,
								CreatedDate: 2010-04-20T21:38:24.6295842Z
							}
						]
					}
				]
				 */
			}
		}

	}
}