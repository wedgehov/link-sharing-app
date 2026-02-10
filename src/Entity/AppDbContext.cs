using Microsoft.EntityFrameworkCore;

namespace Entity;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; } = null!;
    public virtual DbSet<UserProfile> UserProfiles { get; set; } = null!;
    public virtual DbSet<Link> Links { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Rely on conventions and data annotations defined on the entity classes
        base.OnModelCreating(modelBuilder);
    }
}


