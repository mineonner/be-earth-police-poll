using police_poll_service.models.request;
using police_poll_service.models.respone;

namespace police_poll_service.Repositories
{
    public interface IDashboardRepository
    {
        List<DashboardResModel> GetDashboard(DashboardReqModel dash);
    }
}
