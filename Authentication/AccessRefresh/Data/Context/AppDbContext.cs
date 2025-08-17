using AccessRefresh.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessRefresh.Data.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Session> Sessions { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("Database"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("en_US.UTF-8");
        modelBuilder.UseIdentityColumns();
        
        modelBuilder.Entity<User>()
            .HasMany(e => e.Sessions)
            .WithOne(e => e.Owner)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(modelBuilder);
    }
}