using ClipboardManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClipboardManager.Data.Context;

/// <summary>
/// EF Core SQLite database context for clipboard history storage.
/// </summary>
public class ClipboardDbContext : DbContext
{
    public DbSet<ClipboardItemEntity> ClipboardItems => Set<ClipboardItemEntity>();

    public ClipboardDbContext(DbContextOptions<ClipboardDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ClipboardItemEntity>(entity =>
        {
            entity.ToTable("ClipboardItems");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ContentHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.CopiedAt)
                .IsRequired();

            entity.Property(e => e.IsPinned)
                .IsRequired();

            // Indexes to ensure lightning-fast deduplication and list sorting
            entity.HasIndex(e => e.ContentHash);
            entity.HasIndex(e => new { e.IsPinned, e.CopiedAt });
        });
    }
}
