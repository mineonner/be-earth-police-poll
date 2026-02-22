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

        public DbSet<USER> user { get; set; }
        public DbSet<ROLE> role { get; set; }
        public DbSet<ORG_UNIT> org_unit { get; set; }
        public DbSet<EVALUATION> evaluation { get; set; }
        public DbSet<CONFIG> config { get; set; }
    }
}
