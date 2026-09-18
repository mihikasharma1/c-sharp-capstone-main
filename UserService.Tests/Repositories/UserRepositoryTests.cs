using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;
using UserService.Repositories;
using Xunit;

namespace UserService.Tests.Repositories;

public class UserRepositoryTests
{
    private static UserServiceContext CreateContext() =>
        new(new DbContextOptionsBuilder<UserServiceContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetByEmailAsync_ReturnsUser_WhenFound()
    {
        var context = CreateContext();
        context.Users.Add(new User { Email = "test@example.com", FirstName = "A", LastName = "B", PhoneNumber = "123", PasswordHash = "x" });
        await context.SaveChangesAsync();

        var result = await new UserRepository(context).GetByEmailAsync("test@example.com");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenNotFound()
    {
        var context = CreateContext();
        Assert.Null(await new UserRepository(context).GetByEmailAsync("nobody@example.com"));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenFound()
    {
        var context = CreateContext();
        var user = new User { Email = "test@example.com", FirstName = "A", LastName = "B", PhoneNumber = "123", PasswordHash = "x" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await new UserRepository(context).GetByIdAsync(user.UserId);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task AddAsync_PersistsUser()
    {
        var context = CreateContext();
        var repo = new UserRepository(context);
        var user = new User { Email = "new@example.com", FirstName = "A", LastName = "B", PhoneNumber = "123", PasswordHash = "x" };

        await repo.AddAsync(user);
        await repo.SaveChangesAsync();

        Assert.Equal(1, await context.Users.CountAsync());
    }
}