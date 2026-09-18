using CatalogService.Controllers;
using CatalogService.DTOs;
using CatalogService.Models;
using CatalogService.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CatalogService.Tests.Controllers;

public class CatalogControllerTests
{
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly CatalogController _controller;

    public CatalogControllerTests() => _controller = new CatalogController(_bookRepository.Object);

    [Fact]
    public async Task GetBooks_ReturnsPagedResult_WithCorrectMetadata()
    {
        _bookRepository.Setup(r => r.SearchAsync(It.IsAny<BookQueryParameters>()))
            .ReturnsAsync((new List<BookSummaryDto> { new() { Title = "Clean Code" } }, 45));

        var paged = Assert.IsType<PagedResultDto<BookSummaryDto>>(
            Assert.IsType<OkObjectResult>(await _controller.GetBooks(new BookQueryParameters { Page = 0, Size = 20 })).Value);

        Assert.Equal(45, paged.TotalElements);
        Assert.Equal(3, paged.TotalPages);
        Assert.False(paged.Last);
    }

    [Fact]
    public async Task GetBookById_ReturnsNotFound_WhenBookDoesNotExist()
    {
        _bookRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BookDetailDto?)null);
        var notFound = Assert.IsType<NotFoundObjectResult>(await _controller.GetBookById(Guid.NewGuid()));
        Assert.Equal("NOT_FOUND", Assert.IsType<ErrorResponseDto>(notFound.Value).Error);
    }

    [Fact]
    public async Task GetBookById_ReturnsBook_WhenFound()
    {
        var book = new BookDetailDto { BookId = Guid.NewGuid(), Title = "1984" };
        _bookRepository.Setup(r => r.GetByIdAsync(book.BookId)).ReturnsAsync(book);
        Assert.Same(book, Assert.IsType<OkObjectResult>(await _controller.GetBookById(book.BookId)).Value);
    }
}