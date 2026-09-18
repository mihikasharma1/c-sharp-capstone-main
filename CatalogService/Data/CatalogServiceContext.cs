using Microsoft.EntityFrameworkCore;
using CatalogService.Models;

namespace CatalogService.Data;

public class CatalogServiceContext : DbContext
{
    public CatalogServiceContext(DbContextOptions<CatalogServiceContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasIndex(b => b.Isbn).IsUnique();
        });
    }
}