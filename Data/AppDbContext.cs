using CatalogAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Game> Games => Set<Game>();
    public DbSet<UserLibraryItem> UserLibraryItems => Set<UserLibraryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(e =>
        {
            e.ToTable("games");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<UserLibraryItem>(e =>
        {
            e.ToTable("user_library");
            e.HasKey(x => x.Id);
        });
    }
}
