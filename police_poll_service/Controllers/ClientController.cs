using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using police_poll_service.DB;
using police_poll_service.DB.Tables;
using police_poll_service.models.request;
using police_poll_service.models.respone;
using report_meesuanruam_service.services;

namespace police_poll_service.Controllers
{
    [Route("api/police-poll")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly PolicePollDbContext _dbContext;
        private HashService _hashService;

        public ClientController(PolicePollDbContext _context, IConfiguration config)
        {
            _dbContext = _context;
            _hashService = new HashService(config);
        }

        [HttpGet]
        [Route("getPolicePoll")]
        public async Task<IActionResult> getPolicePoll()
        {
            DataResModel res = new DataResModel();
            res.status = "success";
            res.result = "getPolicePolls";
            return Ok(res);
        }

        [HttpPost]
        [Route("getOrgUnitDropdown")]
        public async Task<IActionResult> getOrgUnitDropdown(OrgUnitDropdownReqModel req)
        {
            DataResModel res = new DataResModel();
            try
            {
                List<OrgUnitDropdownResModel> result = new List<OrgUnitDropdownResModel>();

                var query = _dbContext.org_unit.AsQueryable();

                query = query.Where(orgDD =>
                        req.role_code == orgDD.role_code &&
                        (
                            (req.is_head_org
                                ? (req.org_units.Any(code => EF.Functions.Like(orgDD.head_org_unit, "%" + code + "[_]%")) || req.org_units.Contains(orgDD.code))
                                : (req.org_units.All(code => EF.Functions.Like(orgDD.head_org_unit, "%" + code + "[_]%")) || req.org_units.Contains(orgDD.code))
                             ) || req.org_units.Length == 0
                        )
                    ).OrderBy(o => o.id);


                query = query.Where(orgDD =>
                    (string.IsNullOrEmpty(req.search_text) || EF.Functions.Like(orgDD.name, "%" + req.search_text + "%"))
                );

                //query = query.OrderByDescending(orgDD => req.selected_code.Contains(orgDD.code));

                if (req.selected_code.Length > 0)
                {
                    var selectedCodes = _dbContext.org_unit.Where(o => req.selected_code.Contains(o.code));
                    query = selectedCodes.Concat(query);
                }

                result = query
                        .Select(orgDD => new OrgUnitDropdownResModel
                        {
                            id = orgDD.code,
                            name = orgDD.name
                        })
                        .Take(req.max_length)
                        .ToList();

                if (result.Count > 0) result.Insert(0, new OrgUnitDropdownResModel
                {
                    id = "",
                    name = "ทั้งหมด"
                });

                res.status = "success";
                res.result = result;
                return Ok(res);
            }
            catch (Exception ex)
            {
                res.status = "error";
                res.message = "ระบบขัดข้องชั่วคราว อยู่ระหว่างดำเนินการแก้ไข";
                res.result = ex.Message;
                return BadRequest(res);
            }
        }

        [HttpGet]
        [Route("getOrgUnitEvaluation")]
        public async Task<IActionResult> getOrgUnitEvaluation(string years)
        {
            DataResModel res = new DataResModel();
            OrgUnitEvaClientResModel result = new OrgUnitEvaClientResModel();
            try
            {
                string evaluatorTotal = _dbContext.config.Where(o => o.code == "EVALUATOR_TOTAL").Select(u => u.description).FirstOrDefault() ?? string.Empty;
                var orgUnits = await _dbContext.org_unit
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

                var evaluationsByOrgCode = await _dbContext.evaluation
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
                            // evaluators_total = string.IsNullOrEmpty(evaluatorTotal) ? 0 : int.Parse(evaluatorTotal),
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
                // result.evaluators_total = org.Aggregate(0, (acc, x) => acc + x.evaluators_total);
                result.evaluators_total = string.IsNullOrEmpty(evaluatorTotal) ? 0 : int.Parse(evaluatorTotal);
                result.evaluators_amount = org.Aggregate(0, (acc, x) => acc + x.evaluators_amount);
                result.evaluation_date = _dbContext.config.Where(o => o.code == "EVALUATION_DATE").Select(u => u.description).FirstOrDefault() ?? string.Empty;

                res.status = "success";
                res.result = result;
                return Ok(res);
            }
            catch (Exception ex)
            {
                res.status = "error";
                res.message = "ระบบขัดข้องชั่วคราว อยู่ระหว่างดำเนินการแก้ไข";
                res.result = ex.Message;
                return BadRequest(res);
            }
        }

        [HttpPost]
        [Route("searchEvaluationProgress")]
        public async Task<IActionResult> searchEvaluationProgress(FilterDashboardReqModel req)
        {
            DataResModel res = new DataResModel();
            SearchEvaluationProgressResModel result = new SearchEvaluationProgressResModel();
            List<string> org_units = new List<string>();
            try
            {

                if (!string.IsNullOrEmpty(req.bch_org_unit)) org_units.Add(req.bch_org_unit);
                if (!string.IsNullOrEmpty(req.bk_org_unit)) org_units.Add(req.bk_org_unit);
                if (!string.IsNullOrEmpty(req.kk_org_unit)) org_units.Add(req.kk_org_unit);
                if (!string.IsNullOrEmpty(req.org_unit)) org_units.Add(req.org_unit);


                IQueryable<ORG_UNIT> orgQuery = _dbContext.org_unit
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

                var evaluationsByOrgCode = await _dbContext.evaluation
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

                res.status = "success";
                res.result = result;
                return Ok(res);
            }
            catch (Exception ex)
            {
                res.status = "error";
                res.message = "ระบบขัดข้องชั่วคราว อยู่ระหว่างดำเนินการแก้ไข";
                res.result = ex.Message;
                return BadRequest(res);
            }
        }


    }
}
