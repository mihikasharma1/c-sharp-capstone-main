using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;

namespace UserService.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid userId);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}

public class UserRepository : IUserRepository
{
    private readonly UserServiceContext _context;

    public UserRepository(UserServiceContext context)
    {
        _context = context;
    }

    public Task<User?> GetByEmailAsync(string email) =>
        _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByIdAsync(Guid userId) =>
        _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

    public async Task AddAsync(User user) => await _context.Users.AddAsync(user);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}