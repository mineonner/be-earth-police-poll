using Microsoft.EntityFrameworkCore;
using police_poll_service.DB;
using police_poll_service.DB.Tables;
using police_poll_service.models.request;
using police_poll_service.models.respone;

namespace police_poll_service.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly PolicePollDbContext _db;
        private readonly IConfigRepository _config;

        public DashboardRepository(PolicePollDbContext db, IConfigRepository config)
        {
            _db = db;
            _config = config;
        }

        private static bool IsChildOrgUnderHead(string? headOrgUnit, string headCode) =>
            !string.IsNullOrEmpty(headOrgUnit)
            && headOrgUnit.StartsWith(headCode + "_", StringComparison.Ordinal);

        public List<DashboardResModel> GetDashboard(DashboardReqModel dash)
        {
            string? evaluationDate = _config.GetDescriptionByCode("EVALUATION_DATE");
            string evaluationDateStr = evaluationDate ?? string.Empty;

            IQueryable<ORG_UNIT> headsQuery = _db.org_unit.AsNoTracking()
                .Where(h => h.role_code == "RO2");

            if (dash.head_org != null && dash.head_org.Length > 0)
                headsQuery = headsQuery.Where(h => dash.head_org.Contains(h.code));

            var heads = headsQuery
                .OrderBy(h => h.create_date)
                .Select(h => new { h.code, h.name, h.create_date })
                .ToList();

            if (heads.Count == 0)
                return new List<DashboardResModel>();

            var headCodes = heads.Select(h => h.code).ToList();

            var orgEvs = _db.org_unit.AsNoTracking()
                .Where(o => o.is_evaluation
                    && headCodes.Any(hc => EF.Functions.Like(o.head_org_unit, hc + "[_]%")))
                .ToList();

            var orgCodes = orgEvs.Select(o => o.code).Distinct().ToList();

            Dictionary<string, EVALUATION> evalsByOrg = new Dictionary<string, EVALUATION>();
            if (orgCodes.Count > 0)
            {
                evalsByOrg = _db.evaluation.AsNoTracking()
                    .Where(e => e.evaluation_year == dash.years && orgCodes.Contains(e.org_unit_code))
                    .GroupBy(e => e.org_unit_code)
                    .ToDictionary(g => g.Key, g => g.First());
            }

            var dashResult = new List<DashboardResModel>(heads.Count);
            foreach (var head in heads)
            {
                var children = orgEvs
                    .Where(o => IsChildOrgUnderHead(o.head_org_unit, head.code))
                    .ToList();

                if (children.Count == 0)
                {
                    dashResult.Add(new DashboardResModel
                    {
                        org_unit_code = head.code,
                        org_unit_name = head.name ?? string.Empty,
                        evaluators_total = 0,
                        evaluators_count = 0,
                        unevaluators_count = 0,
                        score_total = 0,
                        service_work_total = 0,
                        service_work_count = 0,
                        investigative_work_total = 0,
                        investigative_work_count = 0,
                        crime_prevention_work_total = 0,
                        crime_prevention_work_count = 0,
                        traffic_work_total = 0,
                        traffic_work_count = 0,
                        satisfaction_total = 0,
                        satisfaction_count = 0,
                        evaluation_date = evaluationDateStr,
                        org_unit_creat_date = head.create_date
                    });
                    continue;
                }

                int evaluatorsTotal = 0;
                int evaluatorsCount = 0;
                int serviceWorkTotal = 0, serviceWorkCount = 0;
                int investigativeWorkTotal = 0, investigativeWorkCount = 0;
                int crimePreventionWorkTotal = 0, crimePreventionWorkCount = 0;
                int trafficWorkTotal = 0, trafficWorkCount = 0;
                int satisfactionTotal = 0, satisfactionCount = 0;
                decimal scoreSum = 0;

                foreach (var o in children)
                {
                    evaluatorsTotal += o.evaluators_total;
                    evalsByOrg.TryGetValue(o.code, out var ev);
                    evaluatorsCount += ev?.evaluators_amount ?? 0;
                    serviceWorkTotal += o.service_work_total;
                    serviceWorkCount += ev?.service_work_count ?? 0;
                    investigativeWorkTotal += o.investigative_work_total;
                    investigativeWorkCount += ev?.investigative_work_count ?? 0;
                    crimePreventionWorkTotal += o.crime_prevention_work_total;
                    crimePreventionWorkCount += ev?.crime_prevention_work_count ?? 0;
                    trafficWorkTotal += o.traffic_work_total;
                    trafficWorkCount += ev?.traffic_work_count ?? 0;
                    satisfactionTotal += o.satisfaction_total;
                    satisfactionCount += ev?.satisfaction_count ?? 0;

                    if (o.evaluation_type == "Service")
                    {
                        scoreSum += ((ev?.service_work_score ?? 0) + (ev?.investigative_work_score ?? 0)
                            + (ev?.crime_prevention_work_score ?? 0) + (ev?.traffic_work_score ?? 0)) / 4m;
                    }
                    else
                    {
                        scoreSum += ev?.satisfaction_score ?? 0;
                    }
                }

                int rowCount = children.Count;
                int unevaluatorsRaw = children.Sum(o =>
                {
                    evalsByOrg.TryGetValue(o.code, out var ev);
                    return o.evaluators_total - (ev?.evaluators_amount ?? 0);
                });

                dashResult.Add(new DashboardResModel
                {
                    org_unit_code = head.code,
                    org_unit_name = head.name ?? string.Empty,
                    evaluators_total = evaluatorsTotal,
                    evaluators_count = evaluatorsCount,
                    unevaluators_count = Math.Max(unevaluatorsRaw, 0),
                    score_total = rowCount > 0 ? scoreSum / rowCount : 0,
                    service_work_total = serviceWorkTotal,
                    service_work_count = serviceWorkCount,
                    investigative_work_total = investigativeWorkTotal,
                    investigative_work_count = investigativeWorkCount,
                    crime_prevention_work_total = crimePreventionWorkTotal,
                    crime_prevention_work_count = crimePreventionWorkCount,
                    traffic_work_total = trafficWorkTotal,
                    traffic_work_count = trafficWorkCount,
                    satisfaction_total = satisfactionTotal,
                    satisfaction_count = satisfactionCount,
                    evaluation_date = evaluationDateStr,
                    org_unit_creat_date = head.create_date
                });
            }

            return dashResult
                .OrderBy(x => x.org_unit_creat_date)
                .Select(x =>
                {
                    x.score_total = Math.Round(x.score_total, 2);
                    return x;
                })
                .ToList();
        }
    }
}
