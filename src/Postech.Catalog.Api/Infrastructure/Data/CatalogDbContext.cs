using Microsoft.EntityFrameworkCore;
using Postech.Catalog.Api.Domain.Entities;

namespace Postech.Catalog.Api.Infrastructure.Data;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        MapGames(modelBuilder);

        MapOrders(modelBuilder);
    }

    private void MapOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(o => o.OrderId);
            entity.Property(o => o.OrderId)
                .ValueGeneratedOnAdd();
            entity.Property(o => o.UserId)
                .IsRequired();
            entity.Property(o => o.GameId)
                .IsRequired();
            entity.Property(o => o.TotalAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
            entity.Property(o => o.PlacedAt)
                .IsRequired();
        });
    }

    private void MapGames(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(entity =>
            {
                entity.ToTable("Games");
                entity.HasKey(g => g.Id);
                entity.Property(g => g.Id)
                    .ValueGeneratedOnAdd();
                entity.Property(g => g.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(g => g.Description)
                    .IsRequired()
                    .HasMaxLength(500);
                entity.Property(g => g.Genre)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(g => g.Price)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");
                entity.Property(g => g.ReleaseDate)
                    .IsRequired();
                entity.Property(g => g.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");
            }
        );
    }
}