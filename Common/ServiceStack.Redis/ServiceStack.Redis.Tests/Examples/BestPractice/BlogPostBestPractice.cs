using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common;
using ServiceStack.Common.Extensions;
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
			this.BlogIds = new List<int>();
		}

		public int Id { get; set; }
		public string Name { get; set; }
		public List<int> BlogIds { get; set; }

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
			this.BlogPostIds = new List<int>();
		}

		public int Id { get; set; }
		public int UserId { get; set; }
		public string UserName { get; set; }
		public List<string> Tags { get; set; }
		public List<int> BlogPostIds { get; set; }

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
	#endregion


	#region Blog Repository
	public interface IHasBlogRepository
	{
		IBlogRepository Repository { set; }
	}

	public interface IBlogRepository
	{
		void StoreUsers(params User[] users);
		List<User> GetAllUsers();

		void StoreBlogs(User user, params Blog[] users);
		List<Blog> GetBlogs(IEnumerable<int> blogIds);
		List<Blog> GetAllBlogs();

		List<BlogPost> GetBlogPosts(IEnumerable<int> blogPostIds);
		void StoreNewBlogPosts(Blog blog, params BlogPost[] blogPosts);

		List<BlogPost> GetRecentBlogPosts();
		List<BlogPostComment> GetRecentBlogPostComments();
		IDictionary<string, double> GetTopTags(int take);
		HashSet<string> GetAllCategories();

		void StoreBlogPost(BlogPost blogPost);
		BlogPost GetBlogPost(int postId);
		List<BlogPost> GetBlogPostsByCategory(string categoryName);
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
			this.redisClient = client;
		}

		private readonly IRedisClient redisClient;

		public void StoreUsers(params User[] users)
		{
			using (var userClient = redisClient.GetTypedClient<User>())
			{
				Inject(users);
				users.Where(x => x.Id == default(int))
					.ForEach(x => x.Id = userClient.GetNextSequence());

				userClient.StoreAll(users);
			}
		}

		public List<User> GetAllUsers()
		{
			using (var userClient = redisClient.GetTypedClient<User>())
			{
				return Inject(userClient.GetAll());
			}
		}

		public void StoreBlogs(User user, params Blog[] blogs)
		{
			using (var redisBlogs = redisClient.GetTypedClient<Blog>())
			{
				foreach (var blog in blogs)
				{
					blog.Id = blog.Id != default(int) ? blog.Id : redisBlogs.GetNextSequence();
					blog.UserId = user.Id;
					blog.UserName = user.Name;

					user.BlogIds.AddIfNotExists(blog.Id);
				}

				using (var trans = redisClient.CreateTransaction())
				{
					trans.QueueCommand(x => x.Store(user));
					trans.QueueCommand(x => x.StoreAll(blogs));

					trans.Commit();
				}

				Inject(blogs);
			}
		}

		public List<Blog> GetBlogs(IEnumerable<int> blogIds)
		{
			using (var redisBlogs = redisClient.GetTypedClient<Blog>())
			{
				return Inject(
					redisBlogs.GetByIds(blogIds.ConvertAll(x => x.ToString())));
			}
		}

		public List<Blog> GetAllBlogs()
		{
			using (var redisBlogs = redisClient.GetTypedClient<Blog>())
			{
				return Inject(redisBlogs.GetAll());
			}
		}

		public List<BlogPost> GetBlogPosts(IEnumerable<int> blogPostIds)
		{
			using (var redisBlogPosts = redisClient.GetTypedClient<BlogPost>())
			{
				return redisBlogPosts.GetByIds(blogPostIds.ConvertAll(x => x.ToString())).ToList();
			}
		}

		public void StoreNewBlogPosts(Blog blog, params BlogPost[] blogPosts)
		{
			using (var redisBlogPosts = redisClient.GetTypedClient<BlogPost>())
			using (var redisComments = redisClient.GetTypedClient<BlogPostComment>())
			{
				var recentPosts = redisBlogPosts.Lists[RecentBlogPostsKey];
				var recentComments = redisComments.Lists[RecentBlogPostCommentsKey];

				foreach (var blogPost in blogPosts)
				{
					blogPost.Id = blogPost.Id != default(int) ? blogPost.Id : redisBlogPosts.GetNextSequence();
					blogPost.BlogId = blog.Id;
					blog.BlogPostIds.AddIfNotExists(blogPost.Id);

					recentPosts.Prepend(blogPost);
					blogPost.Comments.ForEach(recentComments.Prepend);
					blogPost.Tags.ForEach(x =>
						redisClient.IncrementItemInSortedSet(TagCloudKey, x, 1));
					blogPost.Categories.ForEach(x =>
						  redisClient.AddToSet(AllCategoriesKey, x));
					blogPost.Categories.ForEach(x =>
						  redisClient.AddToSet(UrnId.Create(CategoryTypeName, x), blogPost.Id.ToString()));
				}

				//Rolling list only keep the last 5
				recentPosts.Trim(0, 4);
				recentComments.Trim(0, 4);

				using (var trans = redisClient.CreateTransaction())
				{
					trans.QueueCommand(x => x.Store(blog));
					trans.QueueCommand(x => x.StoreAll(blogPosts));

					trans.Commit();
				}
			}
		}

		public List<BlogPost> GetRecentBlogPosts()
		{
			using (var redisBlogPosts = redisClient.GetTypedClient<BlogPost>())
			{
				return redisBlogPosts.Lists[RecentBlogPostsKey].GetAll();
			}
		}

		public List<BlogPostComment> GetRecentBlogPostComments()
		{
			using (var redisComments = redisClient.GetTypedClient<BlogPostComment>())
			{
				return redisComments.Lists[RecentBlogPostCommentsKey].GetAll();
			}
		}

		public IDictionary<string, double> GetTopTags(int take)
		{
			return redisClient.GetRangeWithScoresFromSortedSetDesc(TagCloudKey, 0, take - 1);
		}

		public HashSet<string> GetAllCategories()
		{
			return redisClient.GetAllFromSet(AllCategoriesKey);
		}

		public void StoreBlogPost(BlogPost blogPost)
		{
			redisClient.Store(blogPost);
		}

		public BlogPost GetBlogPost(int postId)
		{
			return redisClient.GetById<BlogPost>(postId);
		}

		public List<BlogPost> GetBlogPostsByCategory(string categoryName)
		{
			var categoryUrn = UrnId.Create(CategoryTypeName, categoryName);
			var documentDbPostIds = redisClient.GetAllFromSet(categoryUrn);

			return redisClient.GetByIds<BlogPost>(documentDbPostIds.ToArray()).ToList();
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


	[TestFixture]
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

			var mythzBlog = mythz.CreateNewBlog(new Blog { Tags = { "Architecture", ".NET", "Databases" }});

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

			Console.WriteLine(mythzBlogPosts.Dump());
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
			Console.WriteLine(blogs.Dump());
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

			Console.WriteLine("Recent Posts:\n" + recentPosts.Dump());
			Console.WriteLine("Recent Comments:\n" + recentComments.Dump());
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
				]
			]
			 */
		}

		[Test]
		public void Show_all_Categories()
		{
			//Categories are maintained in the repository
			var allCategories = repository.GetAllCategories();
			Console.WriteLine(allCategories.Dump());
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