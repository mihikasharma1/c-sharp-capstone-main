using CatalogService.Data;
using CatalogService.Models;
using CatalogService.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CatalogService.DTOs;

namespace CatalogService.Tests.Repositories;

public class BookRepositoryTests
{
    private static CatalogServiceContext CreateContext() =>
        new(new DbContextOptionsBuilder<CatalogServiceContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task SeedAsync(CatalogServiceContext context)
    {
        context.Books.AddRange(
            new Book { Title = "Clean Code", Author = "Robert Martin", Genre = "Technology", PublicationYear = 2008, TotalCopies = 5, AvailableCopies = 2, Isbn = "111" },
            new Book { Title = "Refactoring", Author = "Martin Fowler", Genre = "Technology", PublicationYear = 2018, TotalCopies = 3, AvailableCopies = 0, Isbn = "222" },
            new Book { Title = "1984", Author = "George Orwell", Genre = "Fiction", PublicationYear = 1949, TotalCopies = 8, AvailableCopies = 3, Isbn = "333" }
        );
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task SearchAsync_ReturnsAllBooks_WhenNoFiltersApplied()
    {
        var context = CreateContext();
        await SeedAsync(context);
        var (items, total) = await new BookRepository(context).SearchAsync(new BookQueryParameters());
        Assert.Equal(3, total);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task SearchAsync_FiltersByGenre()
    {
        var context = CreateContext();
        await SeedAsync(context);
        var (items, total) = await new BookRepository(context).SearchAsync(new BookQueryParameters { Genre = "Fiction" });
        Assert.Equal(1, total);
        Assert.Equal("1984", items[0].Title);
    }

    [Fact]
    public async Task SearchAsync_FiltersByAvailableOnly()
    {
        var context = CreateContext();
        await SeedAsync(context);
        var (items, total) = await new BookRepository(context).SearchAsync(new BookQueryParameters { AvailableOnly = true });
        Assert.Equal(2, total);
        Assert.All(items, i => Assert.True(i.AvailableCopies > 0));
    }

    [Fact]
    public async Task SearchAsync_SearchesTitleAndAuthorCaseInsensitively()
    {
        var context = CreateContext();
        await SeedAsync(context);
        var (_, total) = await new BookRepository(context).SearchAsync(new BookQueryParameters { Query = "MARTIN" });
        Assert.Equal(2, total);
    }

    [Fact]
    public async Task SearchAsync_SortsByPublicationYearDescending()
    {
        var context = CreateContext();
        await SeedAsync(context);
        var (items, _) = await new BookRepository(context).SearchAsync(new BookQueryParameters { SortBy = "publicationyear", SortOrder = "desc" });
        Assert.Equal("Refactoring", items[0].Title);
    }

    [Fact]
    public async Task SearchAsync_ComputesStatusCorrectly()
    {
        var context = CreateContext();
        await SeedAsync(context);
        var (items, _) = await new BookRepository(context).SearchAsync(new BookQueryParameters { Isbn = "222" });
        Assert.Equal("CHECKED_OUT", items[0].Status);
    }

    [Fact]
    public async Task SearchAsync_Paginates()
    {
        var context = CreateContext();
        await SeedAsync(context);
        var (items, total) = await new BookRepository(context).SearchAsync(new BookQueryParameters { Page = 1, Size = 2 });
        Assert.Equal(3, total);
        Assert.Single(items);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenBookDoesNotExist()
    {
        var context = CreateContext();
        Assert.Null(await new BookRepository(context).GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_IncrementsWithinBounds()
    {
        var context = CreateContext();
        await SeedAsync(context);
        var book = await context.Books.FirstAsync(b => b.Isbn == "222");
        var result = await new BookRepository(context).UpdateAvailabilityAsync(book.BookId, 1);
        Assert.Equal(1, result!.AvailableCopies);
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_ThrowsWhenExceedingTotalCopies()
    {
        var context = CreateContext();
        await SeedAsync(context);
        var book = await context.Books.FirstAsync(b => b.Isbn == "111");
        await Assert.ThrowsAsync<InvalidOperationException>(() => new BookRepository(context).UpdateAvailabilityAsync(book.BookId, 10));
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_ThrowsWhenGoingNegative()
    {
        var context = CreateContext();
        await SeedAsync(context);
        var book = await context.Books.FirstAsync(b => b.Isbn == "222");
        await Assert.ThrowsAsync<InvalidOperationException>(() => new BookRepository(context).UpdateAvailabilityAsync(book.BookId, -1));
    }
    
    [Fact]
    public async Task CreateAsync_CreatesBookWithAvailableCopiesEqualToTotalCopies()
    {
        var context = CreateContext();
        var repo = new BookRepository(context);

        var result = await repo.CreateAsync(new CreateBookRequestDto
        {
            Isbn = "978-1-111-11111-1",
            Title = "Test Book",
            Author = "Test Author",
            Genre = "Fiction",
            TotalCopies = 3
        });

        Assert.Equal(3, result.TotalCopies);
        Assert.Equal(3, result.AvailableCopies);
        Assert.Equal("AVAILABLE", result.Status);
        Assert.Equal(1, await context.Books.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_SetsCheckedOutStatus_WhenTotalCopiesIsZero()
    {
        var context = CreateContext();
        var repo = new BookRepository(context);

        var result = await repo.CreateAsync(new CreateBookRequestDto
        {
            Isbn = "978-2-222-22222-2",
            Title = "No Copies",
            Author = "Author",
            Genre = "Fiction",
            TotalCopies = 0
        });

        Assert.Equal("CHECKED_OUT", result.Status);
    }
}