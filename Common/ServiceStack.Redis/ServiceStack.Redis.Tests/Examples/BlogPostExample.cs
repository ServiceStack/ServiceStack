using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common.Extensions;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Examples
{

	/// <summary>
	/// A complete, self-contained example showing how to create a basic blog application using Redis.
	/// </summary>
	public class User
	{
		public User()
		{
			this.BlogIds = new List<int>();
		}

		public int Id { get; set; }
		public string Name { get; set; }
		public List<int> BlogIds { get; set; }
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


	[TestFixture]
	public class BlogPostExample
	{
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
				var ayende = new User { Id = redisUsers.GetNextSequence(), Name = "Oren Eini" };
				var mythz = new User { Id = redisUsers.GetNextSequence(), Name = "Demis Bellot" };

				var ayendeBlog = new Blog
					{
						Id = redisBlogs.GetNextSequence(),
						UserId = ayende.Id,
						UserName = ayende.Name,
						Tags = new List<string> { "Architecture", ".NET", "Databases" },
					};

				var mythzBlog = new Blog
					{
						Id = redisBlogs.GetNextSequence(),
						UserId = mythz.Id,
						UserName = mythz.Name,
						Tags = new List<string> { "Architecture", ".NET", "Databases" },
					};

				var blogPosts = new List<BlogPost>
				{
					new BlogPost
					{
						Id = redisBlogPosts.GetNextSequence(),
						BlogId = ayendeBlog.Id,
						Title = "RavenDB",
						Categories = new List<string> { "NoSQL", "DocumentDB" },
						Tags = new List<string> {"Raven", "NoSQL", "JSON", ".NET"} ,
						Comments = new List<BlogPostComment>
						{
							new BlogPostComment { Content = "First Comment!", CreatedDate = DateTime.UtcNow,},
							new BlogPostComment { Content = "Second Comment!", CreatedDate = DateTime.UtcNow,},
						}
					},
					new BlogPost
					{
						Id = redisBlogPosts.GetNextSequence(),
						BlogId = mythzBlog.Id,
						Title = "Redis",
						Categories = new List<string> { "NoSQL", "Cache" },
						Tags = new List<string> {"Redis", "NoSQL", "Scalability", "Performance"},
						Comments = new List<BlogPostComment>
						{
							new BlogPostComment { Content = "First Comment!", CreatedDate = DateTime.UtcNow,}
						}
					},
					new BlogPost
					{
						Id = redisBlogPosts.GetNextSequence(),
						BlogId = ayendeBlog.Id,
						Title = "Cassandra",
						Categories = new List<string> { "NoSQL", "Cluster" },
						Tags = new List<string> {"Cassandra", "NoSQL", "Scalability", "Hashing"},
						Comments = new List<BlogPostComment>
						{
							new BlogPostComment { Content = "First Comment!", CreatedDate = DateTime.UtcNow,}
						}
					},
					new BlogPost
					{
						Id = redisBlogPosts.GetNextSequence(),
						BlogId = mythzBlog.Id,
						Title = "Couch Db",
						Categories = new List<string> { "NoSQL", "DocumentDB" },
						Tags = new List<string> {"CouchDb", "NoSQL", "JSON"},
						Comments = new List<BlogPostComment>
						{
							new BlogPostComment {Content = "First Comment!", CreatedDate = DateTime.UtcNow,}
						}
					},
				};

				ayende.BlogIds.Add(ayendeBlog.Id);
				ayendeBlog.BlogPostIds.AddRange(blogPosts.Where(x => x.BlogId == ayendeBlog.Id).ConvertAll(x => x.Id));

				mythz.BlogIds.Add(mythzBlog.Id);
				mythzBlog.BlogPostIds.AddRange(blogPosts.Where(x => x.BlogId == mythzBlog.Id).ConvertAll(x => x.Id));

				redisUsers.Store(ayende);
				redisUsers.Store(mythz);
				redisBlogs.StoreAll(new[] { ayendeBlog, mythzBlog });
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
			}
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

		[Test]
		public void Show_a_list_of_recent_posts_and_comments()
		{
			//Get strongly-typed clients
			using (var redisBlogPosts = redisClient.GetTypedClient<BlogPost>())
			using (var redisComments = redisClient.GetTypedClient<BlogPostComment>())
			{
				//To keep this example let's pretend this is a new list of blog posts
				var newIncomingBlogPosts = redisBlogPosts.GetAll();

				//Let's get back an IList<BlogPost> wrapper around a Redis server-side List.
				var recentPosts = redisBlogPosts.Lists["urn:BlogPost:RecentPosts"];
				var recentComments = redisComments.Lists["urn:BlogPostComment:RecentComments"];

				foreach (var newBlogPost in newIncomingBlogPosts)
				{
					//Prepend the new blog posts to the start of the 'RecentPosts' list
					recentPosts.Prepend(newBlogPost);

					//Prepend all the new blog post comments to the start of the 'RecentComments' list
					newBlogPost.Comments.ForEach(recentComments.Prepend);
				}

				//Make this a Rolling list by only keep the latest 3 posts and comments
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
			//Get strongly-typed clients
			using (var redisBlogPosts = redisClient.GetTypedClient<BlogPost>())
			{
				var newIncomingBlogPosts = redisBlogPosts.GetAll();

				foreach (var newBlogPost in newIncomingBlogPosts)
				{
					//For every tag in each new blog post, increment the number of times each Tag has occurred 
					newBlogPost.Tags.ForEach(x =>
						redisClient.IncrementItemInSortedSet("urn:TagCloud", x, 1));
				}

				//Show top 5 most popular tags with their scores
				var tagCloud = redisClient.GetRangeWithScoresFromSortedSetDesc("urn:TagCloud", 0, 4);
				Console.WriteLine(tagCloud.Dump());
			}
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
			//There is nothing special required here as since comments are Key Value Objects 
			//they are stored and retrieved with the post
			var postId = 1;
			using (var redisBlogPosts = redisClient.GetTypedClient<BlogPost>())
			{
				var selectedBlogPost = redisBlogPosts.GetById(postId.ToString());

				Console.WriteLine(selectedBlogPost.Dump());
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
		public void Show_all_Posts_for_the_DocumentDB_Category()
		{
			using (var redisBlogPosts = redisClient.GetTypedClient<BlogPost>())
			{
				var newIncomingBlogPosts = redisBlogPosts.GetAll();

				foreach (var newBlogPost in newIncomingBlogPosts)
				{
					//For each post add it's Id into each of it's 'Cateogry > Posts' index
					newBlogPost.Categories.ForEach(x =>
						  redisClient.AddToSet("urn:Category:" + x, newBlogPost.Id.ToString()));
				}

				//Retrieve all the post ids for the category you want to view
				var documentDbPostIds = redisClient.GetAllFromSet("urn:Category:DocumentDB");

				//Make a batch call to retrieve all the posts containing the matching ids 
				//(i.e. the DocumentDB Category posts)
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