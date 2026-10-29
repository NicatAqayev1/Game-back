using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Data
{
    public class AppDbContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Slug sütununun unikal (təkrarsız) olmasını təmin edirik
            modelBuilder.Entity<Casino>()
                .HasIndex(c => c.Slug)
                .IsUnique();
            modelBuilder.Entity<PostbackLog>()
                .Property(p => p.Payout)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<PostbackLog>()
                .HasIndex(p => new { p.CasinoId, p.TransactionId })
                .IsUnique()
                .HasFilter("[TransactionId] IS NOT NULL");
            modelBuilder.Entity<PostbackLog>()
                .HasIndex(p => new { p.TransactionId, p.CasinoId })
                .IsUnique()
                .HasFilter("[IsSuccessful] = 1 AND [TransactionId] IS NOT NULL");
            modelBuilder.Entity<PostbackLog>()
                .Property(p => p.RawData)
                .HasColumnType("nvarchar(max)");
        }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}

        public DbSet<Casino> Casinos { get; set; }
        public DbSet<ClickLog> ClickLogs { get; set; }
        public DbSet<PostbackLog> PostbackLogs { get; set; }
    }
}
