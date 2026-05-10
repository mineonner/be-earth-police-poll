using police_poll_service.models.request;
using police_poll_service.models.respone;

namespace police_poll_service.Repositories
{
    public interface IOrgUnitEvaluationReportRepository
    {
        FilterDashboardResModel SearchFilterDashboard(FilterDashboardReqModel req);
        List<OrgUnitMasterListResModel> SearchEvaluation(FilterDashboardReqModel req);
        List<OrgUnitMasterListResModel> SearchDashboardScoreCompareYears(FilterDashboardReqModel req);
        List<OrgUnitMasterListResModel> GetOrgUnitRowsForExcelExport(FilterDashboardReqModel req);
    }
}
