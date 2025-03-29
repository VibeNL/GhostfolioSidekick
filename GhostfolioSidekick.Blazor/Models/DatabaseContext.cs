using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Blazor.Models
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options)
            : base(options)
        {
        }

        public DbSet<Platform> Platforms { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<SymbolProfile> SymbolProfiles { get; set; }
        public DbSet<Activity> Activities { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=ghostfoliosidekick.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
