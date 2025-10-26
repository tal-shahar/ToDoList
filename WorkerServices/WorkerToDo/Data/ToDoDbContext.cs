using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedLibreries.Infrastructure.Database;
using SharedLibreries.Infrastructure.Resilience;
using SharedLibreries.Models;

namespace WorkerServices.WorkerToDo.Data
{
    public class ToDoDbContext : BaseDbContext
    {
        public ToDoDbContext(
            DbContextOptions<ToDoDbContext> options,
            ILogger<ToDoDbContext>? logger = null,
            ICircuitBreaker? circuitBreaker = null,
            IRetryPolicy? retryPolicy = null) 
            : base(options, logger, circuitBreaker, retryPolicy) { }

        public DbSet<Item> Items { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Item>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.IsCompleted).HasDefaultValue(false);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");

                // Soft delete filter
                entity.HasQueryFilter(e => !e.IsDeleted);
            });
        }

        protected override void UpdateTimestamps()
        {
            UpdateTimestampsForEntity<Item>(
                i => i.UpdatedAt,
                (i, time) => i.UpdatedAt = time,
                i => i.CreatedAt,
                (i, time) => i.CreatedAt = time
            );
        }
    }
}
