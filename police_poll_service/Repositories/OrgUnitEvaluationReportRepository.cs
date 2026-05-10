using police_poll_service.DB;

namespace police_poll_service.Repositories
{
    public partial class OrgUnitEvaluationReportRepository : IOrgUnitEvaluationReportRepository
    {
        private readonly PolicePollDbContext _db;

        public OrgUnitEvaluationReportRepository(PolicePollDbContext db)
        {
            _db = db;
        }
    }
}
