using Microsoft.EntityFrameworkCore;

namespace Entity;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; } = null!;
    public virtual DbSet<UserProfile> UserProfiles { get; set; } = null!;
    public virtual DbSet<Link> Links { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>().HasIndex(p => p.UserId).IsUnique();

        modelBuilder
            .Entity<UserProfile>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<Link>()
            .HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(l => l.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(modelBuilder);
    }
}
