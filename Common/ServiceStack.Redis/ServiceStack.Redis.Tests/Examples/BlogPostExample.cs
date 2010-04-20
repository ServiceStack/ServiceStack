using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Examples
{
	[TestFixture]
	public class BlogPostExample
	{

		public class User
		{
			public int Id { get; set; }
			public string Name { get; set; }
		}

		public class Blog
		{
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


		readonly RedisClient redisClient = new RedisClient(TestConfig.SingleHost);

		[SetUp]
		public void OnBeforeEachTest()
		{
			redisClient.FlushAll();
			InsertTestData();
		}


		public void InsertTestData()
		{
			using (var redisUsers = redisClient.GetTypedClient<User>())
			using (var redisBlogs = redisClient.GetTypedClient<Blog>())
			using (var redisBlogPosts = redisClient.GetTypedClient<BlogPost>())
			{
				var ayende = new User { Id = redisUsers.GetNextSequence(), Name = "Ayende" };
				var demis = new User { Id = redisUsers.GetNextSequence(), Name = "Demis" };

				var ayendeBlog = new Blog
					{
						Id = redisBlogs.GetNextSequence(),
						UserId = ayende.Id,
						UserName = ayende.Name,
						Tags = new List<string> { "Architecture", ".NET", "Databases" },
					};

				var demisBlog = new Blog
					{
						Id = redisBlogs.GetNextSequence(),
						UserId = demis.Id,
						UserName = demis.Name,
						Tags = new List<string> { "Architecture", ".NET", "Databases" },
					};

				var blogPosts = new List<BlogPost>
            	{
            		new BlogPost
            			{
            				Id = redisBlogs.GetNextSequence(),
            				BlogId = ayendeBlog.Id,
            				Title = "RavenDB",
            				Categories = new List<string> {"Raven", "NoSQL"},
            				Comments = new List<BlogPostComment>
				           	{
				           		new BlogPostComment { Content = "First Comment!", CreatedDate = DateTime.UtcNow,},
				           		new BlogPostComment { Content = "Second Comment!", CreatedDate = DateTime.UtcNow,},
				           	}
            			},
            		new BlogPost
            			{
            				Id = redisBlogs.GetNextSequence(),
            				BlogId = demisBlog.Id,
            				Title = "Redis",
            				Categories = new List<string> {"Redis", "NoSQL"},
            				Comments = new List<BlogPostComment>
				           	{
				           		new BlogPostComment { Content = "First Comment!", CreatedDate = DateTime.UtcNow,}
				           	}
            			},
            		new BlogPost
            			{
            				Id = redisBlogs.GetNextSequence(),
            				BlogId = ayendeBlog.Id,
            				Title = "Cassandra",
            				Categories = new List<string> {"Cassandra", "NoSQL"},
            				Comments = new List<BlogPostComment>
				           	{
				           		new BlogPostComment { Content = "First Comment!", CreatedDate = DateTime.UtcNow,}
				           	}
            			},
            		new BlogPost
            			{
            				Id = redisBlogs.GetNextSequence(),
            				BlogId = demisBlog.Id,
            				Title = "Couch Db",
            				Categories = new List<string> {"CouchDb", "NoSQL"},
            				Comments = new List<BlogPostComment>
				           	{
				           		new BlogPostComment {Content = "First Comment!", CreatedDate = DateTime.UtcNow,}
				           	}
            			},
            	};

				ayendeBlog.BlogPostIds.AddRange(blogPosts.ConvertAll(x => x.Id));

				redisUsers.Store(ayende);
				redisUsers.Store(demis);
				redisBlogs.StoreAll(new[] { ayendeBlog, demisBlog });
				redisBlogPosts.StoreAll(blogPosts);
			}
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
							3,
							4
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
						BlogPostIds: []
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
				//Console.WriteLine(recentPosts.GetAll().Dump());
				/* Output: 
					[
						{
							Id: 6,
							BlogId: 2,
							Title: Couch Db,
							Categories: 
							[
								CouchDb,
								NoSQL
							],
							Tags: [],
							Comments: 
							[
								{
									Content: First Comment!,
									CreatedDate: 2010-04-20T20:26:28.0676909Z
								}
							]
						},
						{
							Id: 5,
							BlogId: 1,
							Title: Cassandra,
							Categories: 
							[
								Cassandra,
								NoSQL
							],
							Tags: [],
							Comments: 
							[
								{
									Content: First Comment!,
									CreatedDate: 2010-04-20T20:26:28.0666909Z
								}
							]
						},
						{
							Id: 4,
							BlogId: 2,
							Title: Redis,
							Categories: 
							[
								Redis,
								NoSQL
							],
							Tags: [],
							Comments: 
							[
								{
									Content: First Comment!,
									CreatedDate: 2010-04-20T20:26:28.0666909Z
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
		

	}
}