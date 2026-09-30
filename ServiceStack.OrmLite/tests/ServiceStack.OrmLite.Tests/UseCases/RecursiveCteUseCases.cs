using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// A hierarchy of subjects, where each subject can have a parent subject
/// </summary>
public class Subject
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; }
    public bool Active { get; set; }
}

public static class Subjects
{
    public static void Seed(IDbConnection db)
    {
        db.DropAndCreateTable<Subject>();
        db.InsertAll(new List<Subject> {
            new() { Id = 1,  ParentId = null, Name = "Books",           Active = true },
            new() { Id = 2,  ParentId = 1,    Name = "Fiction",         Active = true },
            new() { Id = 3,  ParentId = 2,    Name = "Fantasy",         Active = true },
            new() { Id = 4,  ParentId = 3,    Name = "Epic Fantasy",    Active = true },
            new() { Id = 5,  ParentId = 2,    Name = "Science Fiction", Active = false },
            new() { Id = 6,  ParentId = 1,    Name = "Non-Fiction",     Active = true },
            new() { Id = 7,  ParentId = 6,    Name = "History",         Active = true },
            new() { Id = 8,  ParentId = 7,    Name = "Ancient History", Active = true },
            new() { Id = 9,  ParentId = 6,    Name = "Science",         Active = true },
            new() { Id = 10, ParentId = null, Name = "Archive",         Active = false },
            new() { Id = 11, ParentId = 10,   Name = "Old Catalogs",    Active = false },
        });
    }
}

/// <summary>
/// WithRecursive() queries hierarchical data, e.g. a subject and all its descendants, by reading from a recursive
/// common table expression that starts with the rows of a seed query and repeatedly adds the matching rows.
/// </summary>
[TestFixtureOrmLite]
public class RecursiveCteUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Select_a_node_and_all_its_descendants()
    {
        using var db = OpenDbConnection();
        Subjects.Seed(db);

        // Fiction and every subject under it
        var q = db.From<Subject>()
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.Name == "Fiction"),
                recurse: (parent, child) => child.ParentId == parent.Id);

        var subjects = db.Select(q);
        Assert.That(subjects.Map(x => x.Name),
            Is.EquivalentTo(new[] { "Fiction", "Fantasy", "Epic Fantasy", "Science Fiction" }));
    }

    [Test]
    public void Select_all_ancestors_of_a_node()
    {
        using var db = OpenDbConnection();
        Subjects.Seed(db);

        // Walk up the hierarchy by swapping the relationship: the next row is the found row's parent
        var q = db.From<Subject>()
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.Id == 4),
                recurse: (child, parent) => parent.Id == child.ParentId);

        var path = db.Select(q);
        Assert.That(path.Map(x => x.Name), Is.EquivalentTo(new[] { "Epic Fantasy", "Fantasy", "Fiction", "Books" }));
    }

    [Test]
    public void Stop_walking_at_rows_that_dont_match()
    {
        using var db = OpenDbConnection();
        Subjects.Seed(db);

        // Conditions in recurse stop the walk, e.g. the inactive Science Fiction subject and anything under it
        var q = db.From<Subject>()
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.Name == "Fiction"),
                recurse: (parent, child) => child.ParentId == parent.Id && child.Active);

        Assert.That(db.Select(q).Map(x => x.Name), Is.EquivalentTo(new[] { "Fiction", "Fantasy", "Epic Fantasy" }));
    }

    [Test]
    public void Filter_order_and_project_the_results()
    {
        using var db = OpenDbConnection();
        Subjects.Seed(db);

        // Names of active subjects under Books, excluding Books itself
        var q = db.From<Subject>()
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.Id == 1),
                recurse: (parent, child) => child.ParentId == parent.Id)
            .Where(x => x.Active && x.Id != 1)
            .OrderBy(x => x.Name)
            .Select(x => x.Name);

        Assert.That(db.Column<string>(q), Is.EqualTo(new[] {
            "Ancient History", "Epic Fantasy", "Fantasy", "Fiction", "History", "Non-Fiction", "Science" }));
    }

    [Test]
    public void Count_and_page_the_results()
    {
        using var db = OpenDbConnection();
        Subjects.Seed(db);

        SqlExpression<Subject> Descendants() => db.From<Subject>()
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.Name == "Non-Fiction"),
                recurse: (parent, child) => child.ParentId == parent.Id);

        Assert.That(db.Count(Descendants()), Is.EqualTo(4));
        Assert.That(db.Exists(Descendants()));

        var page = db.Select(Descendants().OrderBy(x => x.Id).Skip(1).Take(2));
        Assert.That(page.Map(x => x.Name), Is.EqualTo(new[] { "History", "Ancient History" }));

        // Keyset pagination also works on the results
        var next = db.Select(Descendants().OrderBy(x => x.Id).SeekAfter(page.Last()));
        Assert.That(next.Map(x => x.Name), Is.EqualTo(new[] { "Science" }));
    }

    [Test]
    public void Seed_multiple_roots_with_params()
    {
        using var db = OpenDbConnection();
        Subjects.Seed(db);

        // Everything under every root, except inactive subjects, with params in both the seed and outer query
        var q = db.From<Subject>()
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.ParentId == null && x.Name != "Nothing"),
                recurse: (parent, child) => child.ParentId == parent.Id)
            .Where(x => x.Active == false && x.Name != "Archive");

        Assert.That(db.Column<string>(q.Select(x => x.Name)),
            Is.EquivalentTo(new[] { "Science Fiction", "Old Catalogs" }));
    }

    [Test]
    public void Combine_with_other_queries()
    {
        using var db = OpenDbConnection();
        Subjects.Seed(db);

        // Fantasy subjects plus all root subjects
        var q = db.From<Subject>()
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.Name == "Fantasy"),
                recurse: (parent, child) => child.ParentId == parent.Id)
            .Select(x => x.Name)
            .Union(db.From<Subject>().Where(x => x.ParentId == null).Select(x => x.Name));

        Assert.That(db.Column<string>(q), Is.EquivalentTo(new[] { "Fantasy", "Epic Fantasy", "Books", "Archive" }));
    }

    [Test]
    public async Task Async_APIs()
    {
        using var db = await OpenDbConnectionAsync();
        Subjects.Seed(db);

        var q = db.From<Subject>()
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.Id == 6),
                recurse: (parent, child) => child.ParentId == parent.Id);

        Assert.That((await db.SelectAsync(q)).Count, Is.EqualTo(4));
        Assert.That(await db.CountAsync(q), Is.EqualTo(4));

        var names = new List<string>();
        await foreach (var subject in db.SelectLazyAsync(q))
            names.Add(subject.Name);
        Assert.That(names.Count, Is.EqualTo(4));
    }

    [Test]
    public void Invalid_usage_throws()
    {
        using var db = OpenDbConnection();
        var seed = db.From<Subject>().Where(x => x.Id == 1);

        var q = db.From<Subject>();
        Assert.Throws<ArgumentException>(() => q.WithRecursive(q, (p, c) => c.ParentId == p.Id));

        q.WithRecursive(seed, (p, c) => c.ParentId == p.Id);
        Assert.Throws<NotSupportedException>(() => q.WithRecursive(seed, (p, c) => c.ParentId == p.Id));

        // Can only be the first query of a set operation
        var union = db.From<Subject>().Union(q);
        Assert.Throws<NotSupportedException>(() => union.ToSelectStatement());
    }
}
