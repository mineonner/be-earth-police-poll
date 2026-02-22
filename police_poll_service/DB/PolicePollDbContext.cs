using Microsoft.EntityFrameworkCore;
using police_poll_service.DB.Tables;

namespace police_poll_service.DB
{
    public class PolicePollDbContext : DbContext
    {
        public PolicePollDbContext(DbContextOptions<PolicePollDbContext> options)
        : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EVALUATION>()
                .Property(e => e.service_work_score)
                .HasPrecision(3, 2);   // precision=10, scale=4

            modelBuilder.Entity<EVALUATION>()
                .Property(e => e.investigative_work_score)
                .HasPrecision(3, 2);   // precision=10, scale=4

            modelBuilder.Entity<EVALUATION>()
                .Property(e => e.crime_prevention_work_score)
                .HasPrecision(3, 2);   // precision=10, scale=4

            modelBuilder.Entity<EVALUATION>()
                .Property(e => e.traffic_work_score)
                .HasPrecision(3, 2);   // precision=10, scale=4

            modelBuilder.Entity<EVALUATION>()
                .Property(e => e.satisfaction_score)
                .HasPrecision(3, 2);   // precision=10, scale=4
        }

        public DbSet<USER> user { get; set; }
        public DbSet<ROLE> role { get; set; }
        public DbSet<ORG_UNIT> org_unit { get; set; }
        public DbSet<EVALUATION> evaluation { get; set; }
        public DbSet<CONFIG> config { get; set; }
    }
}
