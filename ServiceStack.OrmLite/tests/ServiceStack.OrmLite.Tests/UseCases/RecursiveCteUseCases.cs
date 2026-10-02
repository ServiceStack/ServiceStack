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

    public class SubjectLevel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Depth { get; set; }
    }

    [Test]
    public void Limit_how_many_levels_are_selected()
    {
        using var db = OpenDbConnection();
        Subjects.Seed(db);

        SqlExpression<Subject> Under(string name, int maxDepth) => db.From<Subject>()
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.Name == name),
                recurse: (parent, child) => child.ParentId == parent.Id,
                maxDepth: maxDepth)
            .OrderBy(x => x.Id);

        // Books and its children
        Assert.That(db.Select(Under("Books", maxDepth: 1)).Map(x => x.Name),
            Is.EqualTo(new[] { "Books", "Fiction", "Non-Fiction" }));

        // And their children
        Assert.That(db.Select(Under("Books", maxDepth: 2)).Map(x => x.Name), Is.EqualTo(new[] {
            "Books", "Fiction", "Fantasy", "Science Fiction", "Non-Fiction", "History", "Science" }));

        // Only the rows it starts with
        Assert.That(db.Select(Under("Books", maxDepth: 0)).Map(x => x.Name), Is.EqualTo(new[] { "Books" }));

        Assert.That(db.Count(Under("Books", maxDepth: 1)), Is.EqualTo(3));
    }

    [Test]
    public void Select_the_level_of_each_row()
    {
        using var db = OpenDbConnection();
        Subjects.Seed(db);

        // Sql.RecursiveDepth() is how many levels a row is from the rows the query started with
        var q = db.From<Subject>()
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.Name == "Fiction"),
                recurse: (parent, child) => child.ParentId == parent.Id)
            .OrderBy(x => Sql.RecursiveDepth())
            .ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.Name, Depth = Sql.RecursiveDepth() });

        var levels = db.Select<SubjectLevel>(q);

        Assert.That(levels.Map(x => x.Name), Is.EqualTo(new[] { "Fiction", "Fantasy", "Science Fiction", "Epic Fantasy" }));
        Assert.That(levels.Map(x => x.Depth), Is.EqualTo(new[] { 0, 1, 1, 2 }));
    }

    [Test]
    public void Filter_by_the_level_of_each_row()
    {
        using var db = OpenDbConnection();
        Subjects.Seed(db);

        // The grandchildren of Books, which also stops looking further than them
        var q = db.From<Subject>()
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.Name == "Books"),
                recurse: (parent, child) => child.ParentId == parent.Id,
                maxDepth: 2)
            .Where(x => Sql.RecursiveDepth() == 2 && x.Active)
            .OrderBy(x => x.Id);

        Assert.That(db.Select(q).Map(x => x.Name), Is.EqualTo(new[] { "Fantasy", "History", "Science" }));
    }

    [Test]
    public void Select_the_nearest_ancestors_of_a_node()
    {
        using var db = OpenDbConnection();
        Subjects.Seed(db);

        // Epic Fantasy, its parent and grandparent, nearest first
        var q = db.From<Subject>()
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.Name == "Epic Fantasy"),
                recurse: (child, parent) => parent.Id == child.ParentId,
                maxDepth: 2)
            .OrderBy(x => Sql.RecursiveDepth());

        Assert.That(db.Select(q).Map(x => x.Name), Is.EqualTo(new[] { "Epic Fantasy", "Fantasy", "Fiction" }));
    }

    [Test]
    public void Stop_at_rows_that_were_already_visited()
    {
        using var db = OpenDbConnection();
        Subjects.Seed(db);

        // A loop in the data: Books is now under Epic Fantasy, which is under Books
        db.UpdateOnly(() => new Subject { ParentId = 4 }, where: x => x.Name == "Books");

        // Without detectCycles this would recurse until the RDBMS stops it
        var q = db.From<Subject>()
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.Name == "Fiction"),
                recurse: (parent, child) => child.ParentId == parent.Id,
                detectCycles: true)
            .OrderBy(x => x.Id);

        // Every subject that can be reached, once
        Assert.That(db.Select(q).Map(x => x.Id), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));

        // With the level each was reached at
        var levels = db.Select<SubjectLevel>(db.From<Subject>()
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.Name == "Fiction"),
                recurse: (parent, child) => child.ParentId == parent.Id,
                maxDepth: 3,
                detectCycles: true)
            .OrderBy(x => Sql.RecursiveDepth()).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.Name, Depth = Sql.RecursiveDepth() }));

        Assert.That(levels.Map(x => x.Name), Is.EqualTo(new[] {
            "Fiction", "Fantasy", "Science Fiction", "Epic Fantasy", "Books" }));
        Assert.That(levels.Map(x => x.Depth), Is.EqualTo(new[] { 0, 1, 1, 2, 3 }));
    }

    public class Page
    {
        public string Id { get; set; }
        public string LinksTo { get; set; }
    }

    [Test]
    public void Detect_cycles_with_text_primary_keys()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Page>();
        db.InsertAll(new[] {
            new Page { Id = "home", LinksTo = "about" },
            new Page { Id = "about", LinksTo = "team" },
            new Page { Id = "team", LinksTo = "home" },
            new Page { Id = "orphan", LinksTo = null },
        });

        // The pages that can be reached from home, by following where each links to
        var q = db.From<Page>()
            .WithRecursive(
                seed: db.From<Page>().Where(x => x.Id == "home"),
                recurse: (page, next) => next.Id == page.LinksTo,
                detectCycles: true);

        Assert.That(db.Select(q).Map(x => x.Id), Is.EquivalentTo(new[] { "home", "about", "team" }));
    }

    [Test]
    public async Task Limit_levels_async()
    {
        using var db = await OpenDbConnectionAsync();
        Subjects.Seed(db);

        var q = db.From<Subject>()
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.Name == "Books"),
                recurse: (parent, child) => child.ParentId == parent.Id,
                maxDepth: 1,
                detectCycles: true)
            .Select(x => new { x.Id, x.Name, Depth = Sql.RecursiveDepth() });

        var levels = await db.SelectAsync<SubjectLevel>(q);
        Assert.That(levels.Count, Is.EqualTo(3));
        Assert.That(levels.Map(x => x.Depth).Max(), Is.EqualTo(1));
        Assert.That(await db.CountAsync(q), Is.EqualTo(3));
    }

    [Test]
    public void The_depth_is_only_available_in_recursive_queries()
    {
        using var db = OpenDbConnection();

        var q = db.From<Subject>().Where(x => Sql.RecursiveDepth() == 1);
        Assert.Throws<InvalidOperationException>(() => q.ToSelectStatement());

        var seed = db.From<Subject>().Where(x => x.Id == 1);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.From<Subject>().WithRecursive(seed, (p, c) => c.ParentId == p.Id, maxDepth: -1));

        // Queries that don't use the depth don't select it
        var plain = db.From<Subject>().WithRecursive(seed, (p, c) => c.ParentId == p.Id).ToSelectStatement();
        Assert.That(plain, Does.Not.Contain("cte_depth").And.Not.Contain("cte_path"));
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
