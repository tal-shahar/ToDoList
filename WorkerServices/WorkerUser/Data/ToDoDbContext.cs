using Microsoft.EntityFrameworkCore;
using SharedLibreries.Infrastructure.Database;
using SharedLibreries.Infrastructure.Resilience;
using SharedLibreries.Models;

namespace WorkerUser.Data
{
    public class ToDoDbContext : BaseDbContext
    {
        public ToDoDbContext(
            DbContextOptions<ToDoDbContext> options,
            ILogger<ToDoDbContext>? logger = null,
            ICircuitBreaker? circuitBreaker = null,
            IRetryPolicy? retryPolicy = null) 
            : base(options, logger, circuitBreaker, retryPolicy) { }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(320);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            });
        }

        protected override void UpdateTimestamps()
        {
            UpdateTimestampsForEntity<User>(
                u => u.UpdatedAt,
                (u, time) => u.UpdatedAt = time,
                u => u.CreatedAt,
                (u, time) => u.CreatedAt = time
            );
        }
    }
}
