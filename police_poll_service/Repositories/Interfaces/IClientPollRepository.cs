using police_poll_service.models.request;
using police_poll_service.models.respone;

namespace police_poll_service.Repositories
{
    public interface IClientPollRepository
    {
        Task<OrgUnitEvaClientResModel> GetOrgUnitEvaluationSummaryAsync(string years);
        Task<SearchEvaluationProgressResModel> SearchEvaluationProgressAsync(FilterDashboardReqModel req);
    }
}
