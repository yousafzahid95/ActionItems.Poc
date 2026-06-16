using ActionItems.Sdk.Sharding.Catalog.Entities;
using Microsoft.EntityFrameworkCore;

namespace ActionItems.Sdk.Sharding.Catalog;

public class ShardCatalogDbContext : DbContext
{
    public ShardCatalogDbContext(DbContextOptions<ShardCatalogDbContext> options)
        : base(options)
    {
    }

    public DbSet<WorkAreaClientMapping> WorkAreaClients => Set<WorkAreaClientMapping>();

    public DbSet<ClientShardMapping> ClientShards => Set<ClientShardMapping>();

    public DbSet<ShardDefinition> Shards => Set<ShardDefinition>();
    public DbSet<ShardReadReplica> ShardReadReplicas => Set<ShardReadReplica>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkAreaClientMapping>(entity =>
        {
            entity.HasKey(x => x.WorkAreaId);
            entity.Property(x => x.ClientId).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<ShardDefinition>(entity =>
        {
            entity.HasKey(x => x.ShardKey);
            entity.Property(x => x.ShardKey).HasMaxLength(50);
            entity.Property(x => x.MasterKeyVaultSecretName).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<ClientShardMapping>(entity =>
        {
            entity.HasKey(x => x.ClientId);
            entity.Property(x => x.ShardKey).HasMaxLength(50).IsRequired();

            entity.HasOne<ShardDefinition>()
                .WithMany()
                .HasForeignKey(x => x.ShardKey)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ShardReadReplica>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ShardKey).HasMaxLength(50).IsRequired();
            entity.Property(x => x.KeyVaultSecretName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Order).IsRequired();
            entity.HasIndex(x => new { x.ShardKey, x.Order });

            entity.HasOne<ShardDefinition>()
                .WithMany()
                .HasForeignKey(x => x.ShardKey)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
