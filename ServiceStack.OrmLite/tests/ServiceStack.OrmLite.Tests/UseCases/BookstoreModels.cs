using System.Collections.Generic;
using System.Data;
using System.Linq;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

// Tests in UseCases/ double as reference docs, showing popular ways to use OrmLite features against a Bookstore domain

public enum Genre
{
    Fiction,
    Fantasy,
    History,
    Science,
}

public class Book
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public Genre Genre { get; set; }
    public decimal Price { get; set; }
    public int Year { get; set; }
    public bool Available { get; set; }
}

public class BookReview
{
    [AutoIncrement]
    public int Id { get; set; }
    public int BookId { get; set; }
    public string Reviewer { get; set; }
    public int Rating { get; set; }
}

public static class Bookstore
{
    public static readonly Book[] Books =
    [
        new() { Title = "The Hobbit",            Author = "J.R.R. Tolkien",  Genre = Genre.Fantasy, Price = 12.99m, Year = 1937, Available = true },
        new() { Title = "The Silmarillion",      Author = "J.R.R. Tolkien",  Genre = Genre.Fantasy, Price = 15.50m, Year = 1977, Available = false },
        new() { Title = "Dune",                  Author = "Frank Herbert",   Genre = Genre.Fiction, Price = 9.99m,  Year = 1965, Available = true },
        new() { Title = "Neuromancer",           Author = "William Gibson",  Genre = Genre.Fiction, Price = 8.50m,  Year = 1984, Available = true },
        new() { Title = "SPQR",                  Author = "Mary Beard",      Genre = Genre.History, Price = 18.00m, Year = 2015, Available = true },
        new() { Title = "The Guns of August",    Author = "Barbara Tuchman", Genre = Genre.History, Price = 16.25m, Year = 1962, Available = false },
        new() { Title = "A Brief History of Time", Author = "Stephen Hawking", Genre = Genre.Science, Price = 14.00m, Year = 1988, Available = true },
        new() { Title = "Cosmos",                Author = "Carl Sagan",      Genre = Genre.Science, Price = 13.75m, Year = 1980, Available = true },
    ];

    /// <summary>
    /// Recreates the Book + BookReview tables populated with the sample data
    /// </summary>
    public static void Seed(IDbConnection db)
    {
        db.DropTable<BookReview>();
        db.DropAndCreateTable<Book>();
        db.CreateTable<BookReview>();

        db.InsertAll(Books.Map(x => x.CreateCopy()));

        var books = db.Select<Book>();
        int IdOf(string title) => books.First(x => x.Title == title).Id;
        db.InsertAll(new List<BookReview> {
            new() { BookId = IdOf("The Hobbit"), Reviewer = "Alice", Rating = 5 },
            new() { BookId = IdOf("The Hobbit"), Reviewer = "Bob",   Rating = 4 },
            new() { BookId = IdOf("Dune"),       Reviewer = "Alice", Rating = 5 },
            new() { BookId = IdOf("Cosmos"),     Reviewer = "Carol", Rating = 3 },
            new() { BookId = IdOf("SPQR"),       Reviewer = "Bob",   Rating = 2 },
        });
    }

    /// <summary>
    /// Recreates the Book table populated with count generated books
    /// </summary>
    public static List<Book> SeedMany(IDbConnection db, int count)
    {
        db.DropTable<BookReview>();
        db.DropAndCreateTable<Book>();
        var books = Enumerable.Range(1, count).Map(i => new Book {
            Title = "Book " + i,
            Author = "Author " + (i % 50),
            Genre = (Genre)(i % 4),
            Price = i % 100,
            Year = 1900 + i % 120,
            Available = i % 2 == 0,
        });
        db.InsertAll(books);
        return db.Select<Book>();
    }
}
