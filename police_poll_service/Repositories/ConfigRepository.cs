using Microsoft.EntityFrameworkCore;
using police_poll_service.DB;

namespace police_poll_service.Repositories
{
    public class ConfigRepository : IConfigRepository
    {
        private readonly PolicePollDbContext _db;

        public ConfigRepository(PolicePollDbContext db)
        {
            _db = db;
        }

        public string? GetDescriptionByCode(string code) =>
            _db.config.AsNoTracking().Where(o => o.code == code).Select(u => u.description).FirstOrDefault();
    }
}
