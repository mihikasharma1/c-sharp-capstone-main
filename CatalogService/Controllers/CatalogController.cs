using CatalogService.DTOs;
using CatalogService.Models;
using CatalogService.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly IBookRepository _bookRepository;

    public CatalogController(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    [HttpGet("books")]
    public async Task<IActionResult> GetBooks([FromQuery] BookQueryParameters parameters)
    {
        var (items, totalCount) = await _bookRepository.SearchAsync(parameters);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)parameters.Size);

        return Ok(new PagedResultDto<BookSummaryDto>
        {
            Content = items,
            Page = parameters.Page,
            Size = parameters.Size,
            TotalElements = totalCount,
            TotalPages = totalPages,
            Last = parameters.Page >= totalPages - 1
        });
    }

    [HttpGet("books/{bookId:guid}")]
    public async Task<IActionResult> GetBookById(Guid bookId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book is null)
        {
            return NotFound(new ErrorResponseDto
            {
                Error = "NOT_FOUND",
                Message = $"Book not found with ID: {bookId}"
            });
        }

        return Ok(book);
    }
    
    [HttpPut("books/{bookId:guid}/availability")]
    public async Task<IActionResult> UpdateAvailability(Guid bookId, UpdateAvailabilityRequestDto request)
    {
        try
        {
            var updated = await _bookRepository.UpdateAvailabilityAsync(bookId, request.Delta);
            if (updated is null)
                return NotFound(new ErrorResponseDto { Error = "NOT_FOUND", Message = $"Book not found with ID: {bookId}" });

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponseDto { Error = "VALIDATION_ERROR", Message = ex.Message });
        }
    }
}