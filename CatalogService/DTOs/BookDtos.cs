namespace CatalogService.DTOs;
using System.ComponentModel.DataAnnotations;

public class BookSummaryDto
{
    public Guid BookId { get; set; }
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int? PublicationYear { get; set; }
    public string? Description { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class BookDetailDto
{
    public Guid BookId { get; set; }
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int? PublicationYear { get; set; }
    public string? Description { get; set; }
    public string? Publisher { get; set; }
    public int? PageCount { get; set; }
    public string? Language { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateBookRequestDto
{
    [Required, MaxLength(20)]
    public string Isbn { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Author { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Genre { get; set; } = string.Empty;

    public int? PublicationYear { get; set; }
    public string? Description { get; set; }

    [MaxLength(255)]
    public string? Publisher { get; set; }

    public int? PageCount { get; set; }

    [MaxLength(50)]
    public string? Language { get; set; }

    [Range(0, int.MaxValue)]
    public int TotalCopies { get; set; }
}

public class UpdateAvailabilityRequestDto
{
    public int Delta { get; set; }
}