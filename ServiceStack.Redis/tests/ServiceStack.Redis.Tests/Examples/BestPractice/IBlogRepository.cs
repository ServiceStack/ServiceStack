using System.Collections.Generic;

namespace ServiceStack.Redis.Tests.Examples.BestPractice
{
    public interface IBlogRepository
    {
        void StoreUsers(params User[] users);
        List<User> GetAllUsers();

        void StoreBlogs(User user, params Blog[] users);
        List<Blog> GetBlogs(IEnumerable<long> blogIds);
        List<Blog> GetAllBlogs();

        List<BlogPost> GetBlogPosts(IEnumerable<long> blogPostIds);
        void StoreNewBlogPosts(Blog blog, params BlogPost[] blogPosts);

        List<BlogPost> GetRecentBlogPosts();
        List<BlogPostComment> GetRecentBlogPostComments();
        IDictionary<string, double> GetTopTags(int take);
        HashSet<string> GetAllCategories();

        void StoreBlogPost(BlogPost blogPost);
        BlogPost GetBlogPost(int postId);
        List<BlogPost> GetBlogPostsByCategory(string categoryName);
    }
}