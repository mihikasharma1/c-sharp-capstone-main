using Microsoft.EntityFrameworkCore;
using UserService.Models;
using UserService.Services;

namespace UserService.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(UserServiceContext context, IPasswordHasher passwordHasher)
    {
        if (await context.Users.AnyAsync()) return;

        var librarian = new User
        {
            Email = "librarian@library.com",
            PasswordHash = passwordHasher.Hash("Librarian123!"),
            FirstName = "Lib",
            LastName = "Rarian",
            PhoneNumber = "+1-555-0100",
            Role = Role.Librarian,
            MembershipStatus = MembershipStatus.Active,
            MemberSince = DateTime.UtcNow
        };

        var patron = new User
        {
            Email = "patron@library.com",
            PasswordHash = passwordHasher.Hash("Patron123!"),
            FirstName = "Pat",
            LastName = "Ron",
            PhoneNumber = "+1-555-0101",
            Role = Role.Patron,
            MembershipStatus = MembershipStatus.Active,
            MemberSince = DateTime.UtcNow
        };

        await context.Users.AddRangeAsync(librarian, patron);
        await context.SaveChangesAsync();
    }
}