using Microsoft.EntityFrameworkCore;
using police_poll_service.DB;
using police_poll_service.DB.Tables;
using police_poll_service.models.request;
using police_poll_service.models.respone;

namespace police_poll_service.Repositories
{
    public partial class OrgUnitEvaluationReportRepository
    {
        public FilterDashboardResModel SearchFilterDashboard(FilterDashboardReqModel req)
        {
            FilterDashboardResModel result = new FilterDashboardResModel()
            {
                bch_org_unit = null,
                bk_org_unit = null,
                kk_org_unit = null,
                org_unit = null,
                org_unit_evoluation_item_list = new List<OrgUnitMasterListResModel>(),
            };

            List<string> org_units = new List<string>();

            if (!string.IsNullOrEmpty(req.bch_org_unit))
            {
                org_units.Add(req.bch_org_unit);
                result.bch_org_unit = ComputeFilterDashboardPie(req.bch_org_unit, includeSelfCode: false, req.evaluation_years);
            }

            if (!string.IsNullOrEmpty(req.bk_org_unit))
            {
                org_units.Add(req.bk_org_unit);
                result.bk_org_unit = ComputeFilterDashboardPie(req.bk_org_unit, includeSelfCode: false, req.evaluation_years);
            }

            if (!string.IsNullOrEmpty(req.kk_org_unit))
            {
                org_units.Add(req.kk_org_unit);
                result.kk_org_unit = ComputeFilterDashboardPie(req.kk_org_unit, includeSelfCode: true, req.evaluation_years);
            }

            if (!string.IsNullOrEmpty(req.org_unit))
            {
                org_units.Add(req.org_unit);
                result.org_unit = ComputeFilterDashboardPie(req.org_unit, includeSelfCode: true, req.evaluation_years);
            }

            if (org_units.Count > 0)
                result.org_unit_evoluation_item_list = BuildDashboardOrgEvoListWithHeadRollups(req);

            return result;
        }

        private AvaluatorsItemResModel? ComputeFilterDashboardPie(string pathCode, bool includeSelfCode, string? evalYear)
        {
            if (string.IsNullOrEmpty(evalYear))
                return null;

            var headsQuery = _db.org_unit.AsNoTracking()
                .Where(h => h.is_evaluation
                    && (EF.Functions.Like(h.head_org_unit, "%" + pathCode + "[_]%")
                        || (includeSelfCode && h.code == pathCode)));

            var pairs = (from h in headsQuery
                         join ev in _db.evaluation.AsNoTracking().Where(e => e.evaluation_year == evalYear)
                             on h.code equals ev.org_unit_code
                         select new { h, ev }).ToList();

            if (pairs.Count == 0)
                return null;

            return new AvaluatorsItemResModel
            {
                evaluators_count = pairs.Sum(x => x.ev.evaluators_amount),
                unevaluators_count = Math.Max(
                    pairs.Sum(x => x.h.evaluators_total - x.ev.evaluators_amount),
                    0)
            };
        }

        private List<OrgUnitMasterListResModel> BuildDashboardOrgEvoListWithHeadRollups(FilterDashboardReqModel req)
        {
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

            var evalOrgList = orgQuery.ToList();
            var orgCodes = evalOrgList.Select(o => o.code).Distinct().ToList();

            Dictionary<string, EVALUATION> evalByOrg = new Dictionary<string, EVALUATION>();
            if (orgCodes.Count > 0)
            {
                evalByOrg = _db.evaluation.AsNoTracking()
                    .Where(e => e.evaluation_year == req.evaluation_years && orgCodes.Contains(e.org_unit_code))
                    .GroupBy(e => e.org_unit_code)
                    .ToDictionary(g => g.Key, g => g.First());
            }

            var orgEvo = evalOrgList
                .Where(h => evalByOrg.ContainsKey(h.code))
                .Select(h =>
                {
                    var ev = evalByOrg[h.code];
                    return new OrgUnitMasterListResModel
                    {
                        org_unit_code = h.code,
                        org_unit_name = h.name,
                        org_unit_role = h.role_code,
                        evaluation_code = ev.code,
                        service_work_score = Math.Round(ev.service_work_score, 2),
                        service_work_count = ev.service_work_count,
                        service_work_total = h.service_work_total,
                        investigative_work_score = Math.Round(ev.investigative_work_score, 2),
                        investigative_work_count = ev.investigative_work_count,
                        investigative_work_total = h.investigative_work_total,
                        crime_prevention_work_score = Math.Round(ev.crime_prevention_work_score, 2),
                        crime_prevention_work_count = ev.crime_prevention_work_count,
                        crime_prevention_work_total = h.crime_prevention_work_total,
                        traffic_work_score = Math.Round(ev.traffic_work_score, 2),
                        traffic_work_count = ev.traffic_work_count,
                        traffic_work_total = h.traffic_work_total,
                        satisfaction_score = Math.Round(ev.satisfaction_score, 2),
                        satisfaction_count = ev.satisfaction_count,
                        satisfaction_total = h.satisfaction_total,
                        average_total_score = h.evaluation_type == "Service"
                            ? Math.Round(
                                ((ev.service_work_score + ev.investigative_work_score + ev.crime_prevention_work_score + ev.traffic_work_score) / 4m),
                                2)
                            : ev.satisfaction_score,
                        evaluators_amount = ev.evaluators_amount,
                        evaluation_year = ev.evaluation_year,
                        is_evaluation = h.is_evaluation,
                        head_org_unit = (h.head_org_unit.LastIndexOf("_") != -1)
                            ? h.head_org_unit.Substring(0, h.head_org_unit.LastIndexOf("_"))
                            : h.head_org_unit
                    };
                })
                .ToList();

            var codesForHeadLookup = new HashSet<string>(StringComparer.Ordinal);
            foreach (var evo in orgEvo)
            {
                foreach (var part in evo.head_org_unit.Split("_"))
                {
                    if (!string.IsNullOrEmpty(part))
                        codesForHeadLookup.Add(part);
                }
            }

            var orgByCode = codesForHeadLookup.Count == 0
                ? new Dictionary<string, ORG_UNIT>(StringComparer.Ordinal)
                : _db.org_unit.AsNoTracking()
                    .Where(o => codesForHeadLookup.Contains(o.code))
                    .ToDictionary(o => o.code, StringComparer.Ordinal);

            var result = new List<OrgUnitMasterListResModel>();
            var addedHeadRollupCodes = new HashSet<string>(StringComparer.Ordinal);

            string head_org_unit = "";
            foreach (OrgUnitMasterListResModel evo in orgEvo)
            {
                if (head_org_unit != evo.head_org_unit)
                {
                    head_org_unit = evo.head_org_unit;
                    foreach (string head in head_org_unit.Split("_"))
                    {
                        if (string.IsNullOrEmpty(head))
                            continue;
                        if (addedHeadRollupCodes.Add(head) && orgByCode.TryGetValue(head, out var hEnt))
                        {
                            result.Add(BuildSearchEvaluationHeadRollup(hEnt, evalOrgList, evalByOrg, req.evaluation_years, includeAverageTotalScore: true));
                        }
                    }
                    result.Add(evo);
                }
                else
                {
                    result.Add(evo);
                }
            }

            return result;
        }

        public List<OrgUnitMasterListResModel> SearchEvaluation(FilterDashboardReqModel req)
        {
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

            var evalOrgList = orgQuery.ToList();
            var orgCodes = evalOrgList.Select(o => o.code).Distinct().ToList();

            Dictionary<string, EVALUATION> evalByOrg = new Dictionary<string, EVALUATION>();
            if (orgCodes.Count > 0)
            {
                evalByOrg = _db.evaluation.AsNoTracking()
                    .Where(e => e.evaluation_year == req.evaluation_years && orgCodes.Contains(e.org_unit_code))
                    .GroupBy(e => e.org_unit_code)
                    .ToDictionary(g => g.Key, g => g.First());
            }

            var orgEvo = evalOrgList
                .Where(h => evalByOrg.ContainsKey(h.code))
                .Select(h =>
                {
                    var ev = evalByOrg[h.code];
                    return new OrgUnitMasterListResModel
                    {
                        org_unit_code = h.code,
                        org_unit_name = h.name,
                        org_unit_role = h.role_code,
                        evaluation_code = ev.code,
                        service_work_score = Math.Round(ev.service_work_score, 2),
                        service_work_count = ev.service_work_count,
                        investigative_work_score = Math.Round(ev.investigative_work_score, 2),
                        investigative_work_count = ev.investigative_work_count,
                        crime_prevention_work_score = Math.Round(ev.crime_prevention_work_score, 2),
                        crime_prevention_work_count = ev.crime_prevention_work_count,
                        traffic_work_score = Math.Round(ev.traffic_work_score, 2),
                        traffic_work_count = ev.traffic_work_count,
                        satisfaction_score = Math.Round(ev.satisfaction_score, 2),
                        satisfaction_count = ev.satisfaction_count,
                        evaluators_amount = ev.evaluators_amount,
                        evaluation_year = ev.evaluation_year,
                        is_evaluation = h.is_evaluation,
                        head_org_unit = (h.head_org_unit.LastIndexOf("_") != -1)
                            ? h.head_org_unit.Substring(0, h.head_org_unit.LastIndexOf("_"))
                            : h.head_org_unit
                    };
                })
                .ToList();

            var codesForHeadLookup = new HashSet<string>(StringComparer.Ordinal);
            foreach (var evo in orgEvo)
            {
                foreach (var part in evo.head_org_unit.Split("_"))
                {
                    if (!string.IsNullOrEmpty(part))
                        codesForHeadLookup.Add(part);
                }
            }

            var orgByCode = codesForHeadLookup.Count == 0
                ? new Dictionary<string, ORG_UNIT>(StringComparer.Ordinal)
                : _db.org_unit.AsNoTracking()
                    .Where(o => codesForHeadLookup.Contains(o.code))
                    .ToDictionary(o => o.code, StringComparer.Ordinal);

            var result = new List<OrgUnitMasterListResModel>();
            var addedHeadRollupCodes = new HashSet<string>(StringComparer.Ordinal);

            string head_org_unit = "";
            foreach (OrgUnitMasterListResModel evo in orgEvo)
            {
                if (head_org_unit != evo.head_org_unit)
                {
                    head_org_unit = evo.head_org_unit;
                    foreach (string head in head_org_unit.Split("_"))
                    {
                        if (string.IsNullOrEmpty(head))
                            continue;
                        if (addedHeadRollupCodes.Add(head) && orgByCode.TryGetValue(head, out var hEnt))
                        {
                            result.Add(BuildSearchEvaluationHeadRollup(hEnt, evalOrgList, evalByOrg, req.evaluation_years, includeAverageTotalScore: false));
                        }
                    }
                    result.Add(evo);
                }
                else
                {
                    result.Add(evo);
                }
            }

            return result;
        }

        private static bool HevoMatchesHeadForRollup(ORG_UNIT hevo, string headCode) =>
            hevo.is_evaluation
            && (hevo.code == headCode
                || (!string.IsNullOrEmpty(hevo.head_org_unit)
                    && hevo.head_org_unit.Contains(headCode + "_", StringComparison.Ordinal)));

        private static OrgUnitMasterListResModel BuildSearchEvaluationHeadRollup(
            ORG_UNIT h,
            List<ORG_UNIT> evalOrgs,
            Dictionary<string, EVALUATION> evalByOrg,
            string evaluationYears,
            bool includeAverageTotalScore)
        {
            var hevos = evalOrgs
                .Where(hevo => HevoMatchesHeadForRollup(hevo, h.code))
                .OrderBy(x => x.head_org_unit)
                .ThenBy(x => x.role_code)
                .ToList();

            int cntService = hevos.Count(o => o.evaluation_type == "Service");
            int cntSatisfaction = hevos.Count(o => o.evaluation_type == "Satisfaction");

            decimal SumEvDecimal(Func<EVALUATION, decimal> pick) =>
                hevos.Sum(hevo => evalByOrg.TryGetValue(hevo.code, out var e) ? pick(e) : 0m);

            int SumEvInt(Func<EVALUATION, int> pick) =>
                hevos.Sum(hevo => evalByOrg.TryGetValue(hevo.code, out var e) ? pick(e) : 0);

            string evalYear = evaluationYears;
            foreach (var hevo in hevos)
            {
                if (evalByOrg.TryGetValue(hevo.code, out var e) && e != null)
                {
                    evalYear = e.evaluation_year;
                    break;
                }
            }

            var row = new OrgUnitMasterListResModel
            {
                org_unit_code = h.code,
                org_unit_name = h.name,
                org_unit_role = h.role_code,
                service_work_score = cntService != 0 ? Math.Round(SumEvDecimal(e => e.service_work_score) / cntService, 2) : 0,
                investigative_work_score = cntService != 0 ? Math.Round(SumEvDecimal(e => e.investigative_work_score) / cntService, 2) : 0,
                crime_prevention_work_score = cntService != 0 ? Math.Round(SumEvDecimal(e => e.crime_prevention_work_score) / cntService, 2) : 0,
                traffic_work_score = cntService != 0 ? Math.Round(SumEvDecimal(e => e.traffic_work_score) / cntService, 2) : 0,
                satisfaction_score = cntSatisfaction != 0 ? Math.Round(SumEvDecimal(e => e.satisfaction_score) / cntSatisfaction, 2) : 0,
                service_work_count = SumEvInt(e => e.service_work_count),
                investigative_work_count = SumEvInt(e => e.investigative_work_count),
                crime_prevention_work_count = SumEvInt(e => e.crime_prevention_work_count),
                traffic_work_count = SumEvInt(e => e.traffic_work_count),
                satisfaction_count = SumEvInt(e => e.satisfaction_count),
                evaluators_amount = SumEvInt(e => e.evaluators_amount),
                evaluation_year = evalYear,
                is_evaluation = h.is_evaluation
            };

            if (includeAverageTotalScore && hevos.Count > 0)
            {
                decimal sumScores = hevos.Sum(hevo =>
                {
                    evalByOrg.TryGetValue(hevo.code, out var e);
                    if (hevo.evaluation_type == "Service")
                    {
                        return (((e?.service_work_score ?? 0) + (e?.investigative_work_score ?? 0)
                            + (e?.crime_prevention_work_score ?? 0) + (e?.traffic_work_score ?? 0)) / 4m);
                    }
                    return e?.satisfaction_score ?? 0m;
                });
                row.average_total_score = Math.Round(sumScores / hevos.Count, 2);
            }

            return row;
        }

        public List<OrgUnitMasterListResModel> SearchDashboardScoreCompareYears(FilterDashboardReqModel req)
        {
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

            var rows = orgQuery.ToList();
            int orgService = rows.Count(o => o.evaluation_type == "Service");
            int orgSatisfaction = rows.Count(o => o.evaluation_type == "Satisfaction");
            var orgCodes = rows.Select(r => r.code).Distinct().ToList();

            List<int> years = new List<int>();
            if (!string.IsNullOrEmpty(req.evaluation_years)) years.Add(int.Parse(req.evaluation_years));
            if (!string.IsNullOrEmpty(req.compare_evaluation_years)) years.Add(int.Parse(req.compare_evaluation_years));

            List<OrgUnitMasterListResModel> orgEvo = new List<OrgUnitMasterListResModel>();

            if (rows.Count == 0)
                return orgEvo;

            foreach (int year in years)
            {
                string yearStr = year.ToString();
                Dictionary<string, EVALUATION> evalByOrg = new Dictionary<string, EVALUATION>();
                if (orgCodes.Count > 0)
                {
                    evalByOrg = _db.evaluation.AsNoTracking()
                        .Where(e => e.evaluation_year == yearStr && orgCodes.Contains(e.org_unit_code))
                        .GroupBy(e => e.org_unit_code)
                        .ToDictionary(g => g.Key, g => g.First());
                }

                int gCount = rows.Count;

                decimal SumEvDecimal(Func<EVALUATION, decimal> pick) =>
                    rows.Sum(h => evalByOrg.TryGetValue(h.code, out var e) ? pick(e) : 0m);

                OrgUnitMasterListResModel score = new OrgUnitMasterListResModel
                {
                    service_work_total = rows.Sum(h => h.service_work_total),
                    investigative_work_total = rows.Sum(h => h.investigative_work_total),
                    crime_prevention_work_total = rows.Sum(h => h.crime_prevention_work_total),
                    traffic_work_total = rows.Sum(h => h.traffic_work_total),
                    satisfaction_total = rows.Sum(h => h.satisfaction_total),
                    service_work_score = orgService != 0 ? Math.Round(SumEvDecimal(e => e.service_work_score) / orgService, 2) : 0,
                    investigative_work_score = orgService != 0 ? Math.Round(SumEvDecimal(e => e.investigative_work_score) / orgService, 2) : 0,
                    crime_prevention_work_score = orgService != 0 ? Math.Round(SumEvDecimal(e => e.crime_prevention_work_score) / orgService, 2) : 0,
                    traffic_work_score = orgService != 0 ? Math.Round(SumEvDecimal(e => e.traffic_work_score) / orgService, 2) : 0,
                    satisfaction_score = orgSatisfaction != 0 ? Math.Round(SumEvDecimal(e => e.satisfaction_score) / orgSatisfaction, 2) : 0,
                    average_total_score = Math.Round(
                        rows.Sum(h =>
                        {
                            evalByOrg.TryGetValue(h.code, out var ev);
                            return h.evaluation_type == "Service"
                                ? (((ev?.service_work_score ?? 0) + (ev?.investigative_work_score ?? 0)
                                    + (ev?.crime_prevention_work_score ?? 0) + (ev?.traffic_work_score ?? 0)) / 4m)
                                : (ev?.satisfaction_score ?? 0m);
                        }) / gCount,
                        2),
                    org_unit_count = gCount,
                    evaluation_year = yearStr
                };

                orgEvo.Add(score);
            }

            return orgEvo;
        }

        public List<OrgUnitMasterListResModel> GetOrgUnitRowsForExcelExport(FilterDashboardReqModel req) =>
            BuildDashboardOrgEvoListWithHeadRollups(req);

    }
}
