using CatalogService.Data;
using CatalogService.DTOs;
using CatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Repositories;

public interface IBookRepository
{
    Task<(List<BookSummaryDto> Items, int TotalCount)> SearchAsync(BookQueryParameters parameters);
    Task<BookDetailDto?> GetByIdAsync(Guid bookId);
    Task<BookDetailDto?> UpdateAvailabilityAsync(Guid bookId, int delta);
    
    Task<BookDetailDto> CreateAsync(CreateBookRequestDto request);
}

public class BookRepository : IBookRepository
{
    private readonly CatalogServiceContext _context;

    public BookRepository(CatalogServiceContext context)
    {
        _context = context;
    }

    public async Task<(List<BookSummaryDto> Items, int TotalCount)> SearchAsync(BookQueryParameters parameters)
    {
        var query = _context.Books.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Query))
        {
            var term = parameters.Query.Trim().ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(term) ||
                b.Author.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Genre))
        {
            query = query.Where(b => b.Genre == parameters.Genre);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Isbn))
        {
            query = query.Where(b => b.Isbn == parameters.Isbn);
        }

        if (parameters.AvailableOnly)
        {
            query = query.Where(b => b.AvailableCopies > 0);
        }

        var totalCount = await query.CountAsync();

        query = ApplySorting(query, parameters.SortBy, parameters.SortOrder);

        var items = await query
            .Skip(parameters.Page * parameters.Size)
            .Take(parameters.Size)
            .Select(b => new BookSummaryDto
            {
                BookId = b.BookId,
                Isbn = b.Isbn,
                Title = b.Title,
                Author = b.Author,
                Genre = b.Genre,
                PublicationYear = b.PublicationYear,
                Description = b.Description,
                TotalCopies = b.TotalCopies,
                AvailableCopies = b.AvailableCopies,
                Status = b.AvailableCopies > 0 ? "AVAILABLE" : "CHECKED_OUT"
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<BookDetailDto?> GetByIdAsync(Guid bookId) =>
        _context.Books
            .AsNoTracking()
            .Where(b => b.BookId == bookId)
            .Select(b => new BookDetailDto
            {
                BookId = b.BookId,
                Isbn = b.Isbn,
                Title = b.Title,
                Author = b.Author,
                Genre = b.Genre,
                PublicationYear = b.PublicationYear,
                Description = b.Description,
                Publisher = b.Publisher,
                PageCount = b.PageCount,
                Language = b.Language,
                TotalCopies = b.TotalCopies,
                AvailableCopies = b.AvailableCopies,
                Status = b.AvailableCopies > 0 ? "AVAILABLE" : "CHECKED_OUT",
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            })
            .FirstOrDefaultAsync();
    
    
    public async Task<BookDetailDto?> UpdateAvailabilityAsync(Guid bookId, int delta)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.BookId == bookId);
        if (book is null) return null;

        var newAvailable = book.AvailableCopies + delta;
        if (newAvailable < 0 || newAvailable > book.TotalCopies)
            throw new InvalidOperationException(
                $"Cannot adjust availability by {delta}: would result in {newAvailable}, valid range is 0-{book.TotalCopies}");

        book.AvailableCopies = newAvailable;
        await _context.SaveChangesAsync();

        return new BookDetailDto
        {
            BookId = book.BookId, Isbn = book.Isbn, Title = book.Title, Author = book.Author,
            Genre = book.Genre, PublicationYear = book.PublicationYear, Description = book.Description,
            Publisher = book.Publisher, PageCount = book.PageCount, Language = book.Language,
            TotalCopies = book.TotalCopies, AvailableCopies = book.AvailableCopies,
            Status = book.AvailableCopies > 0 ? "AVAILABLE" : "CHECKED_OUT",
            CreatedAt = book.CreatedAt, UpdatedAt = book.UpdatedAt
        };
    }
    
    public async Task<BookDetailDto> CreateAsync(CreateBookRequestDto request)
    {
        var book = new Book
        {
            Isbn = request.Isbn, Title = request.Title, Author = request.Author, Genre = request.Genre,
            PublicationYear = request.PublicationYear, Description = request.Description,
            Publisher = request.Publisher, PageCount = request.PageCount, Language = request.Language,
            TotalCopies = request.TotalCopies, AvailableCopies = request.TotalCopies
        };

        await _context.Books.AddAsync(book);
        await _context.SaveChangesAsync();

        return new BookDetailDto
        {
            BookId = book.BookId, Isbn = book.Isbn, Title = book.Title, Author = book.Author, Genre = book.Genre,
            PublicationYear = book.PublicationYear, Description = book.Description, Publisher = book.Publisher,
            PageCount = book.PageCount, Language = book.Language, TotalCopies = book.TotalCopies,
            AvailableCopies = book.AvailableCopies,
            Status = book.AvailableCopies > 0 ? "AVAILABLE" : "CHECKED_OUT",
            CreatedAt = book.CreatedAt, UpdatedAt = book.UpdatedAt
        };
    }

    private static IQueryable<Book> ApplySorting(IQueryable<Book> query, string sortBy, string sortOrder)
    {
        var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "author" => descending ? query.OrderByDescending(b => b.Author) : query.OrderBy(b => b.Author),
            "publicationyear" => descending ? query.OrderByDescending(b => b.PublicationYear) : query.OrderBy(b => b.PublicationYear),
            _ => descending ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title)
        };
    }
}