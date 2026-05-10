using Microsoft.EntityFrameworkCore;
using police_poll_service.DB;
using police_poll_service.DB.Tables;
using police_poll_service.models.request;
using police_poll_service.models.respone;

namespace police_poll_service.Repositories
{
    public class ClientPollRepository : IClientPollRepository
    {
        private readonly PolicePollDbContext _db;
        private readonly IConfigRepository _config;

        public ClientPollRepository(PolicePollDbContext db, IConfigRepository config)
        {
            _db = db;
            _config = config;
        }

        public async Task<OrgUnitEvaClientResModel> GetOrgUnitEvaluationSummaryAsync(string years)
        {
            OrgUnitEvaClientResModel result = new OrgUnitEvaClientResModel();
            string evaluatorTotal = _config.GetDescriptionByCode("EVALUATOR_TOTAL") ?? string.Empty;
            var orgUnits = await _db.org_unit.AsNoTracking()
                .Where(orgEv => orgEv.is_evaluation == true)
                .Select(orgEv => new
                {
                    orgEv.code,
                    orgEv.evaluation_type,
                    orgEv.evaluators_total,
                    orgEv.service_work_total,
                    orgEv.investigative_work_total,
                    orgEv.crime_prevention_work_total,
                    orgEv.traffic_work_total,
                    orgEv.satisfaction_total
                })
                .ToListAsync();

            var evaluationsByOrgCode = await _db.evaluation.AsNoTracking()
                .Where(ev => ev.evaluation_year == years)
                .ToDictionaryAsync(ev => ev.org_unit_code, ev => ev);

            List<OrgUnitMasterListResModel> org = orgUnits
                .Select(orgEv =>
                {
                    evaluationsByOrgCode.TryGetValue(orgEv.code, out var ev);
                    return new OrgUnitMasterListResModel
                    {
                        org_unit_code = orgEv.code,
                        evaluation_type = orgEv.evaluation_type ?? string.Empty,
                        evaluators_total = orgEv.evaluators_total,
                        evaluators_amount = ev?.evaluators_amount ?? 0,
                        service_work_total = orgEv.service_work_total,
                        investigative_work_total = orgEv.investigative_work_total,
                        crime_prevention_work_total = orgEv.crime_prevention_work_total,
                        traffic_work_total = orgEv.traffic_work_total,
                        satisfaction_total = orgEv.satisfaction_total,
                        service_work_count = ev?.service_work_count ?? 0,
                        investigative_work_count = ev?.investigative_work_count ?? 0,
                        crime_prevention_work_count = ev?.crime_prevention_work_count ?? 0,
                        traffic_work_count = ev?.traffic_work_count ?? 0,
                        satisfaction_count = ev?.satisfaction_count ?? 0
                    };
                })
                .ToList();

            result.org_unit_total = org.Count;
            result.org_unit_evaluation_complete = org.Where(o =>
                                            (o.evaluation_type == "Service" && o.service_work_total <= o.service_work_count && o.investigative_work_total <= o.investigative_work_count
                                            && o.crime_prevention_work_total <= o.crime_prevention_work_count && o.traffic_work_total <= o.traffic_work_count) ||
                                            (o.evaluation_type == "Satisfaction" && o.satisfaction_total <= o.satisfaction_count)
                                            ).Count();
            result.evaluators_total = string.IsNullOrEmpty(evaluatorTotal) ? 0 : int.Parse(evaluatorTotal);
            result.evaluators_amount = org.Aggregate(0, (acc, x) => acc + x.evaluators_amount);
            result.evaluation_date = _config.GetDescriptionByCode("EVALUATION_DATE") ?? string.Empty;

            return result;
        }

        public async Task<SearchEvaluationProgressResModel> SearchEvaluationProgressAsync(FilterDashboardReqModel req)
        {
            SearchEvaluationProgressResModel result = new SearchEvaluationProgressResModel();
            List<string> org_units = new List<string>();

            if (!string.IsNullOrEmpty(req.bch_org_unit)) org_units.Add(req.bch_org_unit);
            if (!string.IsNullOrEmpty(req.bk_org_unit)) org_units.Add(req.bk_org_unit);
            if (!string.IsNullOrEmpty(req.kk_org_unit)) org_units.Add(req.kk_org_unit);
            if (!string.IsNullOrEmpty(req.org_unit)) org_units.Add(req.org_unit);

            IQueryable<ORG_UNIT> orgQuery = _db.org_unit.AsNoTracking()
                .Where(o => o.is_evaluation == true)
                .OrderBy(o => o.head_org_unit);

            if (org_units.Any())
            {
                orgQuery = orgQuery.Where(o =>
                    org_units.Contains(o.code) ||
                    (org_units.All(code => EF.Functions.Like(o.head_org_unit, "%" + code + "[_]%"))));
            }

            var orgUnits = await orgQuery
                .Select(org_ev => new
                {
                    org_ev.code,
                    org_ev.evaluators_total,
                    org_ev.service_work_total,
                    org_ev.investigative_work_total,
                    org_ev.crime_prevention_work_total,
                    org_ev.traffic_work_total,
                    org_ev.satisfaction_total
                })
                .ToListAsync();

            var evaluationsByOrgCode = await _db.evaluation.AsNoTracking()
                .Where(ev => ev.evaluation_year == req.evaluation_years)
                .ToDictionaryAsync(ev => ev.org_unit_code, ev => ev);

            List<OrgUnitMasterListResModel> org = orgUnits
                .Select(org_ev =>
                {
                    evaluationsByOrgCode.TryGetValue(org_ev.code, out var ev);
                    return new OrgUnitMasterListResModel
                    {
                        org_unit_code = org_ev.code,
                        evaluators_total = org_ev.evaluators_total,
                        evaluators_amount = ev?.evaluators_amount ?? 0,
                        service_work_total = org_ev.service_work_total,
                        investigative_work_total = org_ev.investigative_work_total,
                        crime_prevention_work_total = org_ev.crime_prevention_work_total,
                        traffic_work_total = org_ev.traffic_work_total,
                        satisfaction_total = org_ev.satisfaction_total,
                        service_work_count = ev?.service_work_count ?? 0,
                        investigative_work_count = ev?.investigative_work_count ?? 0,
                        crime_prevention_work_count = ev?.crime_prevention_work_count ?? 0,
                        traffic_work_count = ev?.traffic_work_count ?? 0,
                        satisfaction_count = ev?.satisfaction_count ?? 0,
                    };
                })
                .ToList();

            result.evaluators_total = org.Aggregate(0, (acc, x) => acc + x.evaluators_total);
            result.evaluators_amount = org.Aggregate(0, (acc, x) => acc + x.evaluators_amount);
            result.service_work_total = org.Aggregate(0, (acc, x) => acc + x.service_work_total);
            result.investigative_work_total = org.Aggregate(0, (acc, x) => acc + x.investigative_work_total);
            result.crime_prevention_work_total = org.Aggregate(0, (acc, x) => acc + x.crime_prevention_work_total);
            result.traffic_work_total = org.Aggregate(0, (acc, x) => acc + x.traffic_work_total);
            result.satisfaction_total = org.Aggregate(0, (acc, x) => acc + x.satisfaction_total);
            result.service_work_count = org.Aggregate(0, (acc, x) => acc + x.service_work_count);
            result.investigative_work_count = org.Aggregate(0, (acc, x) => acc + x.investigative_work_count);
            result.crime_prevention_work_count = org.Aggregate(0, (acc, x) => acc + x.crime_prevention_work_count);
            result.traffic_work_count = org.Aggregate(0, (acc, x) => acc + x.traffic_work_count);
            result.satisfaction_count = org.Aggregate(0, (acc, x) => acc + x.satisfaction_count);

            return result;
        }
    }
}
