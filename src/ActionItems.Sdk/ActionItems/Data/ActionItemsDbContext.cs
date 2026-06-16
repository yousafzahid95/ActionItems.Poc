using ActionItems.Sdk.ActionItems.Entities;
using Microsoft.EntityFrameworkCore;

namespace ActionItems.Sdk.ActionItems.Data;

public class ActionItemsDbContext : DbContext
{
    public ActionItemsDbContext(DbContextOptions<ActionItemsDbContext> options)
        : base(options)
    {
    }

    public DbSet<ActionItem> ActionItems => Set<ActionItem>();

    public DbSet<Entity> Entities => Set<Entity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<ActionItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.WorkAreaId);
            entity.HasIndex(x => x.EntityId);
            entity.HasOne<Entity>()
                .WithMany()
                .HasForeignKey(x => x.EntityId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
