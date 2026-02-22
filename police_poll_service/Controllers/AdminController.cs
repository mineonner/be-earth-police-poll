using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using police_poll_service.DB;
using police_poll_service.DB.Tables;
using police_poll_service.models.request;
using police_poll_service.models.respone;
using police_poll_service.services;
using report_meesuanruam_service.services;

namespace police_poll_service.Controllers
{
    [Route("api/police-pollAd")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly PolicePollDbContext _dbContext;
        private HashService _hashService;
        private PolicePollService _pSer;

        public AdminController(PolicePollDbContext _context, IConfiguration config)
        {
            _dbContext = _context;
            _hashService = new HashService(config);
            _pSer = new PolicePollService();
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> login(LoginReqModel log)
        {
            DataResModel res = new DataResModel();
            try
            {
                UserResModel result = (from u in _dbContext.user
                                       join ro in _dbContext.role on u.role_code equals ro.code
                                       where u.user == log.user
                                       select new UserResModel
                                       {
                                           user = u.user,
                                           password = u.password,
                                           role_code = u.role_code,
                                           role_name = ro.name,
                                           org_unit_code = u.org_unit_code,
                                           org_unit_name = "",
                                           token = ""
                                       }).SingleOrDefault();

                if (result != null && _hashService.Verify(log.password, result.password))
                {
                    result.password = "";
                    if (!String.IsNullOrEmpty(result.org_unit_code))
                    {
                        ORG_UNIT ork = _dbContext.org_unit.SingleOrDefault(b => b.code == result.org_unit_code);
                        result.org_unit_name = ork.name;
                        var tokenString = _hashService.createJwtToken(result);
                        result.token = tokenString;
                    }
                    res.result = result;
                }

                res.status = "success";
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
        [Route("getDashboard")]
        // [Authorize]
        public async Task<IActionResult> getDashboard(DashboardReqModel dash)
        {
            DataResModel res = new DataResModel();
            string evaluationDate = _dbContext.config.Where(o => o.code == "EVALUATION_DATE").Select(u => u.description).FirstOrDefault();
            try
            {
                List<DashboardResModel> dashResult = (
                    from head in _dbContext.org_unit
                    where head.role_code == "RO2" && (dash.head_org.Contains(head.code) || dash.head_org.Length == 0)
                    orderby head.create_date ascending

                    from org_ev in _dbContext.org_unit
                    .Where(orgEv => EF.Functions.Like(orgEv.head_org_unit, head.code + "[_]%") && orgEv.is_evaluation == true)
                    .DefaultIfEmpty()

                    join ev in _dbContext.evaluation
                       on new { OrgCode = org_ev.code, Year = dash.years } equals new { OrgCode = ev.org_unit_code, Year = ev.evaluation_year }
                       into evJoin
                    from ev in evJoin.DefaultIfEmpty()

                    group new { head, org_ev, ev } by new { head.code, head.name, head.create_date } into g

                    select new DashboardResModel
                    {
                        org_unit_code = g.Key.code,
                        org_unit_name = g.Key.name,
                        evaluators_total = g.Sum(x => x.org_ev != null ? x.org_ev.evaluators_total : 0),
                        evaluators_count = g.Sum(x => x.ev != null ? x.ev.evaluators_amount : 0),
                        unevaluators_count = Math.Max(
                           g.Sum(x => (x.org_ev != null ? x.org_ev.evaluators_total : 0) - (x.ev != null ? x.ev.evaluators_amount : 0)),
                           0),
                        score_total = g.Sum(x =>
                           x.org_ev.evaluation_type == "Service"
                               ? ((x.ev != null ? x.ev.service_work_score : 0) + (x.ev != null ? x.ev.investigative_work_score : 0)
                               + (x.ev != null ? x.ev.crime_prevention_work_score : 0) + (x.ev != null ? x.ev.traffic_work_score : 0)) / 4

                               : (x.ev != null ? x.ev.satisfaction_score : 0)) /
                            g.Count(),
                        service_work_total = g.Sum(x => x.org_ev != null ? x.org_ev.service_work_total : 0),
                        service_work_count = g.Sum(x => x.ev != null ? x.ev.service_work_count : 0),
                        investigative_work_total = g.Sum(x => x.org_ev != null ? x.org_ev.investigative_work_total : 0),
                        investigative_work_count = g.Sum(x => x.ev != null ? x.ev.investigative_work_count : 0),
                        crime_prevention_work_total = g.Sum(x => x.org_ev != null ? x.org_ev.crime_prevention_work_total : 0),
                        crime_prevention_work_count = g.Sum(x => x.ev != null ? x.ev.crime_prevention_work_count : 0),
                        traffic_work_total = g.Sum(x => x.org_ev != null ? x.org_ev.traffic_work_total : 0),
                        traffic_work_count = g.Sum(x => x.ev != null ? x.ev.traffic_work_count : 0),
                        satisfaction_total = g.Sum(x => x.org_ev != null ? x.org_ev.satisfaction_total : 0),
                        satisfaction_count = g.Sum(x => x.ev != null ? x.ev.satisfaction_count : 0),
                        evaluation_date = evaluationDate,
                        org_unit_creat_date = g.Key.create_date
                    }
                    ).OrderBy(x => x.org_unit_creat_date).ToList();

                dashResult = dashResult.Select(x =>
                {
                    x.score_total = Math.Round(x.score_total, 2);
                    return x;
                }).ToList();


                res.status = "success";
                res.result = dashResult;
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
        [Route("getOrgUnitDropdown")]
        [Authorize]
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

                //if (result.Count > 0) result.Insert(0, new OrgUnitDropdownResModel
                //{
                //    id = "",
                //    name = "ทั้งหมด"
                //});

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
        [Route("searchFilterDashboard")]
        [Authorize]
        public async Task<IActionResult> searchFilterDashboard(FilterDashboardReqModel req)
        {
            DataResModel res = new DataResModel();
            FilterDashboardResModel result = new FilterDashboardResModel()
            {
                bch_org_unit = null,
                bk_org_unit = null,
                kk_org_unit = null,
                org_unit = null,
                org_unit_evoluation_item_list = new List<OrgUnitMasterListResModel>(),
            }
            ;
            List<string> org_units = new List<string>();
            List<OrgUnitMasterListResModel> orgEvo = new List<OrgUnitMasterListResModel>();
            try
            {
                #region piedashboard
                if (!string.IsNullOrEmpty(req.bch_org_unit))
                {
                    org_units.Add(req.bch_org_unit);
                    result.bch_org_unit = (from h in _dbContext.org_unit
                                           join ev in _dbContext.evaluation
                                               on h.code equals ev.org_unit_code into evGroup
                                           from ev in evGroup.DefaultIfEmpty()
                                           where EF.Functions.Like(h.head_org_unit, "%" + req.bch_org_unit + "[_]%") &&
                                                 h.is_evaluation == true && ev.evaluation_year == req.evaluation_years
                                           group new { h, ev } by new { } into g

                                           select new AvaluatorsItemResModel
                                           {
                                               evaluators_count = g.Sum(x => x.ev != null ? x.ev.evaluators_amount : 0),
                                               unevaluators_count = Math.Max(
                                                 g.Sum(x => (x.h != null ? x.h.evaluators_total : 0) - (x.ev != null ? x.ev.evaluators_amount : 0)),
                                                 0)
                                           }).SingleOrDefault();
                }

                if (!string.IsNullOrEmpty(req.bk_org_unit))
                {
                    org_units.Add(req.bk_org_unit);
                    result.bk_org_unit = (from h in _dbContext.org_unit
                                          join ev in _dbContext.evaluation
                                              on h.code equals ev.org_unit_code into evGroup
                                          from ev in evGroup.DefaultIfEmpty()
                                          where EF.Functions.Like(h.head_org_unit, "%" + req.bk_org_unit + "[_]%") &&
                                                h.is_evaluation == true && ev.evaluation_year == req.evaluation_years
                                          group new { h, ev } by new { } into g

                                          select new AvaluatorsItemResModel
                                          {
                                              evaluators_count = g.Sum(x => x.ev != null ? x.ev.evaluators_amount : 0),
                                              unevaluators_count = Math.Max(
                                                g.Sum(x => (x.h != null ? x.h.evaluators_total : 0) - (x.ev != null ? x.ev.evaluators_amount : 0)),
                                                0)
                                          }).SingleOrDefault();
                }

                if (!string.IsNullOrEmpty(req.kk_org_unit))
                {
                    org_units.Add(req.kk_org_unit);
                    result.kk_org_unit = (from h in _dbContext.org_unit
                                          join ev in _dbContext.evaluation
                                              on h.code equals ev.org_unit_code into evGroup
                                          from ev in evGroup.DefaultIfEmpty()
                                          where (EF.Functions.Like(h.head_org_unit, "%" + req.kk_org_unit + "[_]%") || h.code == req.kk_org_unit) &&
                                                h.is_evaluation == true && ev.evaluation_year == req.evaluation_years
                                          group new { h, ev } by new { } into g

                                          select new AvaluatorsItemResModel
                                          {
                                              evaluators_count = g.Sum(x => x.ev != null ? x.ev.evaluators_amount : 0),
                                              unevaluators_count = Math.Max(
                                                g.Sum(x => (x.h != null ? x.h.evaluators_total : 0) - (x.ev != null ? x.ev.evaluators_amount : 0)),
                                                0)
                                          }).SingleOrDefault();
                }

                if (!string.IsNullOrEmpty(req.org_unit))
                {
                    org_units.Add(req.org_unit);
                    result.org_unit = (from h in _dbContext.org_unit
                                       join ev in _dbContext.evaluation
                                           on h.code equals ev.org_unit_code into evGroup
                                       from ev in evGroup.DefaultIfEmpty()
                                       where (EF.Functions.Like(h.head_org_unit, "%" + req.org_unit + "[_]%") || h.code == req.org_unit) &&
                                             h.is_evaluation == true && ev.evaluation_year == req.evaluation_years
                                       group new { h, ev } by new { } into g

                                       select new AvaluatorsItemResModel
                                       {
                                           evaluators_count = g.Sum(x => x.ev != null ? x.ev.evaluators_amount : 0),
                                           unevaluators_count = Math.Max(
                                             g.Sum(x => (x.h != null ? x.h.evaluators_total : 0) - (x.ev != null ? x.ev.evaluators_amount : 0)),
                                             0)
                                       }).SingleOrDefault();
                }
                #endregion

                List<OrgUnitItemResModel> orgItems = new List<OrgUnitItemResModel>();
                if (org_units.Count > 0)
                {
                    IQueryable<ORG_UNIT> orgQuery = _dbContext.org_unit
                        .Where(o => o.is_evaluation == true)
                        .OrderBy(o => o.head_org_unit);

                    if (org_units.Any())
                    {
                        orgQuery = orgQuery.Where(o =>
                            org_units.Contains(o.code) ||
                            (org_units.All(code => EF.Functions.Like(o.head_org_unit, "%" + code + "[_]%"))));
                    }

                    orgEvo = (from h in orgQuery
                              join ev in _dbContext.evaluation
                                on new { HevoCode = h.code, Year = req.evaluation_years }
                                equals new { HevoCode = ev.org_unit_code, Year = ev.evaluation_year }


                              select new OrgUnitMasterListResModel
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
                                                                                     ? Math.Round((((ev != null ? ev.service_work_score : 0) + (ev != null ? ev.investigative_work_score : 0)
                                                                                     + (ev != null ? ev.crime_prevention_work_score : 0) + (ev != null ? ev.traffic_work_score : 0)) / 4), 2)

                                                                                     : (ev != null ? ev.satisfaction_score : 0),
                                  evaluators_amount = ev.evaluators_amount,
                                  evaluation_year = ev.evaluation_year,
                                  is_evaluation = h.is_evaluation,
                                  head_org_unit = (h.head_org_unit.LastIndexOf("_") != -1) ? h.head_org_unit.Substring(0, h.head_org_unit.LastIndexOf("_")) : h.head_org_unit
                              }).ToList();

                    string head_org_unit = "";
                    int orgService = orgQuery.Where(o => o.evaluation_type == "Service").ToList().Count();
                    int orgSatisfaction = orgQuery.Where(o => o.evaluation_type == "Satisfaction").ToList().Count();

                    foreach (OrgUnitMasterListResModel evo in orgEvo)
                    {
                        if (head_org_unit != evo.head_org_unit)
                        {
                            head_org_unit = evo.head_org_unit;
                            string[] headOrgs = head_org_unit.Split("_");
                            foreach (string head in headOrgs)
                            {
                                if (result.org_unit_evoluation_item_list.FirstOrDefault(i => i.org_unit_code == head) == null)
                                {
                                    OrgUnitMasterListResModel headEvoluation = (from h in _dbContext.org_unit.Where(o => o.code == head)

                                                                                join hevo in _dbContext.org_unit on 1 equals 1 into hevoGroup
                                                                                from hevo in hevoGroup
                                                                                    .Where(hevo =>
                                                                                        (EF.Functions.Like(hevo.head_org_unit, "%" + h.code + "[_]%") || hevo.code == h.code)
                                                                                        && hevo.is_evaluation)
                                                                                    .DefaultIfEmpty()

                                                                                join ev in _dbContext.evaluation
                                                                                    on new { HevoCode = hevo.code, Year = req.evaluation_years }
                                                                                    equals new { HevoCode = ev.org_unit_code, Year = ev.evaluation_year }
                                                                                    into evGroup
                                                                                from ev in evGroup.DefaultIfEmpty()

                                                                                group new { h, ev, hevo } by new
                                                                                {
                                                                                    h.code,
                                                                                    h.name,
                                                                                    h.head_org_unit,
                                                                                    h.role_code,
                                                                                    h.is_evaluation,
                                                                                    evaluation_year = ev != null ? ev.evaluation_year : req.evaluation_years
                                                                                } into g

                                                                                orderby g.Key.head_org_unit, g.Key.role_code

                                                                                select new OrgUnitMasterListResModel
                                                                                {
                                                                                    org_unit_code = g.Key.code,
                                                                                    org_unit_name = g.Key.name,
                                                                                    org_unit_role = g.Key.role_code,
                                                                                    service_work_score = g.Count(o => o.hevo.evaluation_type == "Service") != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.service_work_score : 0) / g.Count(o => o.hevo.evaluation_type == "Service")), 2) : 0,
                                                                                    investigative_work_score = g.Count(o => o.hevo.evaluation_type == "Service") != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.investigative_work_score : 0) / g.Count(o => o.hevo.evaluation_type == "Service")), 2) : 0,
                                                                                    crime_prevention_work_score = g.Count(o => o.hevo.evaluation_type == "Service") != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.crime_prevention_work_score : 0) / g.Count(o => o.hevo.evaluation_type == "Service")), 2) : 0,
                                                                                    traffic_work_score = g.Count(o => o.hevo.evaluation_type == "Service") != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.traffic_work_score : 0) / g.Count(o => o.hevo.evaluation_type == "Service")), 2) : 0,
                                                                                    satisfaction_score = g.Count(o => o.hevo.evaluation_type == "Satisfaction") != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.satisfaction_score : 0) / g.Count(o => o.hevo.evaluation_type == "Satisfaction")), 2) : 0,

                                                                                    service_work_count = g.Sum(x => x.ev != null ? x.ev.service_work_count : 0),
                                                                                    investigative_work_count = g.Sum(x => x.ev != null ? x.ev.investigative_work_count : 0),
                                                                                    crime_prevention_work_count = g.Sum(x => x.ev != null ? x.ev.crime_prevention_work_count : 0),
                                                                                    traffic_work_count = g.Sum(x => x.ev != null ? x.ev.traffic_work_count : 0),
                                                                                    satisfaction_count = g.Sum(x => x.ev != null ? x.ev.satisfaction_count : 0),
                                                                                    evaluators_amount = g.Sum(x => x.ev != null ? x.ev.evaluators_amount : 0),

                                                                                    average_total_score = Math.Round((g.Sum(x => x.hevo.evaluation_type == "Service"
                                                                                    ? (((x.ev != null ? x.ev.service_work_score : 0) + (x.ev != null ? x.ev.investigative_work_score : 0)
                                                                                    + (x.ev != null ? x.ev.crime_prevention_work_score : 0) + (x.ev != null ? x.ev.traffic_work_score : 0)) / 4)

                                                                                    : (x.ev != null ? x.ev.satisfaction_score : 0)) / g.Count()), 2),

                                                                                    evaluation_year = g.Key.evaluation_year,
                                                                                    is_evaluation = g.Key.is_evaluation
                                                                                }).First();
                                    result.org_unit_evoluation_item_list.Add(headEvoluation);
                                }
                            }
                            result.org_unit_evoluation_item_list.Add(evo);
                        }
                        else
                        {
                            result.org_unit_evoluation_item_list.Add(evo);
                        }

                    }
                }

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
        [Route("searchOrgUnitMasterList")]
        [Authorize]
        public async Task<IActionResult> searchOrgUnitMasterList(OrgUnitMasterListReqModel req)
        {
            DataResModel res = new DataResModel();
            List<string> org_units = new List<string>();
            List<OrgUnitMasterListResModel> result = new List<OrgUnitMasterListResModel>();
            UserResModel userData = new UserResModel();
            try
            {
                if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                    authHeader.ToString().StartsWith("Bearer "))
                {
                    string token = authHeader.ToString().Substring("Bearer ".Length).Trim();
                    userData = _hashService.DecodingJwtToken(token);
                }

                if (string.IsNullOrEmpty(userData.role_code) || userData.role_code != "RO1")
                {
                    res.status = "error";
                    res.message = "ไม่พบสิทธิ์";
                    return BadRequest(res);
                }

                if (!string.IsNullOrEmpty(req.bch_org_unit)) org_units.Add(req.bch_org_unit);
                if (!string.IsNullOrEmpty(req.bk_org_unit)) org_units.Add(req.bk_org_unit);
                if (!string.IsNullOrEmpty(req.kk_org_unit)) org_units.Add(req.kk_org_unit);
                if (!string.IsNullOrEmpty(req.org_unit)) org_units.Add(req.org_unit);

                if (org_units.Count > 0)
                {
                    string[] headCodes = _dbContext.org_unit
                       .Where(ou => (org_units.Contains(ou.code)))
                       .Distinct()
                       .ToList().Select(ou => ou.head_org_unit.Split('_')).SelectMany(x => x).Distinct().ToArray();
                    //headCodes = orgHead.Select(ou => ou.head_org_unit.Split('_')).SelectMany(x => x).Distinct().ToArray();

                    foreach (string head in headCodes)
                    {
                        OrgUnitMasterListResModel headResult = _dbContext.org_unit.OrderBy(o => o.head_org_unit)
                            .ThenBy(o => o.role_code).Where(o => o.code == head).Select(o => new OrgUnitMasterListResModel
                            {
                                id = o.id,
                                org_unit_code = o.code,
                                org_unit_name = o.name,
                                org_unit_role = o.role_code,
                                service_work_total = o.service_work_total,
                                investigative_work_total = o.investigative_work_total,
                                crime_prevention_work_total = o.crime_prevention_work_total,
                                traffic_work_total = o.traffic_work_total,
                                satisfaction_total = o.satisfaction_total,
                                evaluation_type = o.evaluation_type,
                                is_evaluation = o.is_evaluation,
                                head_org_unit = o.head_org_unit,
                                head_role_orgs = new List<HeadOrgUnitItemResModel>()
                            }).First();

                        result.Add(headResult);
                    }

                    List<OrgUnitMasterListResModel> subHead = _dbContext.org_unit.OrderBy(o => o.head_org_unit)
                        .ThenBy(o => o.role_code)
                        .Where(o => headCodes.All(code => EF.Functions.Like(o.head_org_unit, "%" + code + "[_]%")))
                        .Select(o => new OrgUnitMasterListResModel
                        {
                            id = o.id,
                            org_unit_code = o.code,
                            org_unit_name = o.name,
                            org_unit_role = o.role_code,
                            service_work_total = o.service_work_total,
                            investigative_work_total = o.investigative_work_total,
                            crime_prevention_work_total = o.crime_prevention_work_total,
                            traffic_work_total = o.traffic_work_total,
                            satisfaction_total = o.satisfaction_total,
                            evaluation_type = o.evaluation_type,
                            is_evaluation = o.is_evaluation,
                            head_org_unit = o.head_org_unit,
                            head_role_orgs = new List<HeadOrgUnitItemResModel>()
                        }).ToList();

                    result.AddRange(subHead);
                }
                else
                {
                    result = _dbContext.org_unit.OrderBy(o => o.head_org_unit)
                        .ThenBy(o => o.role_code).Select(o => new OrgUnitMasterListResModel
                        {
                            id = o.id,
                            org_unit_code = o.code,
                            org_unit_name = o.name,
                            org_unit_role = o.role_code,
                            service_work_total = o.service_work_total,
                            investigative_work_total = o.investigative_work_total,
                            crime_prevention_work_total = o.crime_prevention_work_total,
                            traffic_work_total = o.traffic_work_total,
                            satisfaction_total = o.satisfaction_total,
                            evaluation_type = o.evaluation_type,
                            is_evaluation = o.is_evaluation,
                            head_org_unit = o.head_org_unit,
                            head_role_orgs = new List<HeadOrgUnitItemResModel>()
                        }).ToList();
                }

                foreach (OrgUnitMasterListResModel org in result)
                {
                    string[] headOrgs = org.head_org_unit.Split("_");

                    List<HeadOrgUnitItemResModel> head_role_orgs = _dbContext.org_unit.Where(o => headOrgs.Contains(o.code)).OrderBy(o => o.head_org_unit)
                   .ThenBy(o => o.role_code).Select(o => new HeadOrgUnitItemResModel
                   {
                       role_code = o.role_code,
                       org_unit_code = o.code
                   }).ToList();

                    org.head_role_orgs.AddRange(head_role_orgs);
                }


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
        [Route("getRoleDropdown")]
        [Authorize]
        public async Task<IActionResult> getRoleDropdown(BaseDropdownRequest req)
        {
            DataResModel res = new DataResModel();
            UserResModel userData = new UserResModel();
            List<OrgUnitDropdownResModel> result = new List<OrgUnitDropdownResModel>();
            try
            {
                if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                    authHeader.ToString().StartsWith("Bearer "))
                {
                    string token = authHeader.ToString().Substring("Bearer ".Length).Trim();
                    userData = _hashService.DecodingJwtToken(token);
                }

                if (string.IsNullOrEmpty(userData.role_code) || userData.role_code != "RO1")
                {
                    res.status = "error";
                    res.message = "ไม่พบสิทธิ์";
                    return BadRequest(res);
                }

                var query = _dbContext.role.Where(orgDD =>
                    (string.IsNullOrEmpty(req.search_text) || EF.Functions.Like(orgDD.name, "%" + req.search_text + "%"))
                );

                if (req.selected_code.Length > 0)
                {
                    var selectedCodes = _dbContext.role.Where(o => req.selected_code.Contains(o.code));
                    query = selectedCodes.Concat(query);
                }

                if (req.except_codes.Length > 0)
                {
                    query = query.Where(o => !req.except_codes.Contains(o.code));
                }

                result = query.Select(o => new OrgUnitDropdownResModel
                {
                    id = o.code,
                    name = o.name
                }).Take(req.max_length).ToList();

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
        [Route("saveOrgUnit")]
        [Authorize]
        public async Task<IActionResult> saveOrgUnit(OrgUnitDataReqModel req)
        {
            DataResModel res = new DataResModel();
            UserResModel userData = new UserResModel();
            string head_org_unit = string.Join("_", req.head_role_orgs.Select(p => p.org_unit_code));

            if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                authHeader.ToString().StartsWith("Bearer "))
            {
                string token = authHeader.ToString().Substring("Bearer ".Length).Trim();
                userData = _hashService.DecodingJwtToken(token);
            }

            if (string.IsNullOrEmpty(userData.role_code) || userData.role_code != "RO1")
            {
                res.status = "error";
                res.message = "ไม่พบสิทธิ์";
                return BadRequest(res);
            }

            try
            {
                if (req.id > 0)
                {


                    #region edit
                    ORG_UNIT org = _dbContext.org_unit.Single(o => o.code == req.org_unit_code && o.role_code == req.org_unit_role);
                    head_org_unit = string.IsNullOrEmpty(head_org_unit) ? org.code : $"{head_org_unit}_{org.code}";

                    org.name = req.org_unit_name;
                    org.evaluation_type = req.evaluation_type;
                    org.service_work_total = req.service_work_total;
                    org.investigative_work_total = req.investigative_work_total;
                    org.crime_prevention_work_total = req.crime_prevention_work_total;
                    org.traffic_work_total = req.traffic_work_total;
                    org.satisfaction_total = req.satisfaction_total;
                    org.evaluators_total = req.evaluators_total;
                    org.is_evaluation = req.is_evaluation;
                    org.head_org_unit = head_org_unit;
                    #endregion
                }
                else
                {
                    string code;
                    #region add
                    if (req.is_evaluation)
                    {
                        code = req.org_unit_code;
                    }
                    else
                    {
                        string codePrefix = "";
                        if (req.org_unit_role == "RO2") codePrefix = "BCH";
                        if (req.org_unit_role == "RO3") codePrefix = "BK";
                        if (req.org_unit_role == "RO4") codePrefix = "KK";
                        if (req.org_unit_role == "RO5") codePrefix = "ORG";
                        ORG_UNIT org = _dbContext.org_unit.Where(o => EF.Functions.Like(o.code, codePrefix + "%") && o.role_code == req.org_unit_role)
                                 .OrderByDescending(e => Convert.ToInt32(e.code.Substring(codePrefix.Length, e.code.Length - codePrefix.Length)))
                                 .FirstOrDefault();

                        if (org == null)
                        {
                            code = $"{codePrefix}1";
                        }
                        else
                        {
                            code = codePrefix + (Int64.Parse(org.code.Substring(codePrefix.Length, org.code.Length - codePrefix.Length)) + 1);
                        }
                    }


                    head_org_unit = string.IsNullOrEmpty(head_org_unit) ? code : $"{head_org_unit}_{code}";

                    _dbContext.org_unit.Add(new ORG_UNIT()
                    {
                        code = code,
                        name = req.org_unit_name,
                        create_date = DateTime.Now.AddHours(7),
                        create_by = userData.user,
                        role_code = req.org_unit_role,
                        evaluation_type = req.evaluation_type,
                        is_evaluation = req.is_evaluation,
                        head_org_unit = head_org_unit,
                        service_work_total = req.service_work_total,
                        investigative_work_total = req.investigative_work_total,
                        crime_prevention_work_total = req.crime_prevention_work_total,
                        traffic_work_total = req.traffic_work_total,
                        satisfaction_total = req.satisfaction_total,
                        evaluators_total = req.evaluators_total,
                    });

                    #endregion
                }
                _dbContext.SaveChanges();
                res.status = "success";
                //res.result = result;
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
        [Route("deleteOrgUnit")]
        [Authorize]
        public async Task<IActionResult> deleteOrgUnit(string code)
        {
            DataResModel res = new DataResModel();
            UserResModel userData = new UserResModel();

            if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                authHeader.ToString().StartsWith("Bearer "))
            {
                string token = authHeader.ToString().Substring("Bearer ".Length).Trim();
                userData = _hashService.DecodingJwtToken(token);
            }

            try
            {
                if (string.IsNullOrEmpty(userData.role_code) || userData.role_code != "RO1")
                {
                    res.status = "error";
                    res.message = "ไม่พบสิทธิ์";
                    return BadRequest(res);
                }

                if (!string.IsNullOrEmpty(code))
                {
                    List<ORG_UNIT> orgs = _dbContext.org_unit.Where(o => o.code == code || EF.Functions.Like(o.head_org_unit, "%" + code + "[_]%")).ToList();
                    _dbContext.org_unit.RemoveRange(orgs);
                    _dbContext.SaveChanges();
                    res.status = "success";
                    res.message = "ลบข้อมูลสำเร็จ";
                }

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
        [Route("searchEvaluation")]
        [Authorize]
        public async Task<IActionResult> searchEvaluation(FilterDashboardReqModel req)
        {
            DataResModel res = new DataResModel();
            List<string> org_units = new List<string>();
            List<OrgUnitMasterListResModel> result = new List<OrgUnitMasterListResModel>();
            UserResModel userData = new UserResModel();
            List<OrgUnitMasterListResModel> orgEvo = new List<OrgUnitMasterListResModel>();
            try
            {
                if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) && authHeader.ToString().StartsWith("Bearer "))
                {
                    string token = authHeader.ToString().Substring("Bearer ".Length).Trim();
                    userData = _hashService.DecodingJwtToken(token);
                }

                if (string.IsNullOrEmpty(userData.role_code) || userData.role_code != "RO1")
                {
                    res.status = "error";
                    res.message = "ไม่พบสิทธิ์";
                    return BadRequest(res);
                }

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

                orgEvo = (from h in orgQuery
                          join ev in _dbContext.evaluation
                            on new { HevoCode = h.code, Year = req.evaluation_years }
                            equals new { HevoCode = ev.org_unit_code, Year = ev.evaluation_year }


                          select new OrgUnitMasterListResModel
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
                              head_org_unit = (h.head_org_unit.LastIndexOf("_") != -1) ? h.head_org_unit.Substring(0, h.head_org_unit.LastIndexOf("_")) : h.head_org_unit
                          }).ToList();

                string head_org_unit = "";
                foreach (OrgUnitMasterListResModel evo in orgEvo)
                {
                    if (head_org_unit != evo.head_org_unit)
                    {
                        head_org_unit = evo.head_org_unit;
                        string[] headOrgs = head_org_unit.Split("_");
                        foreach (string head in headOrgs)
                        {
                            if (result.FirstOrDefault(i => i.org_unit_code == head) == null)
                            {
                                OrgUnitMasterListResModel headEvoluation = (from h in _dbContext.org_unit.Where(o => o.code == head)

                                                                            join hevo in _dbContext.org_unit on 1 equals 1 into hevoGroup
                                                                            from hevo in hevoGroup
                                                                                .Where(hevo =>
                                                                                    (EF.Functions.Like(hevo.head_org_unit, "%" + h.code + "[_]%") || hevo.code == h.code)
                                                                                    && hevo.is_evaluation)
                                                                                .DefaultIfEmpty()

                                                                            join ev in _dbContext.evaluation
                                                                                on new { HevoCode = hevo.code, Year = req.evaluation_years }
                                                                                equals new { HevoCode = ev.org_unit_code, Year = ev.evaluation_year }
                                                                                into evGroup
                                                                            from ev in evGroup.DefaultIfEmpty()

                                                                            group new { h, ev, hevo } by new
                                                                            {
                                                                                h.code,
                                                                                h.name,
                                                                                h.head_org_unit,
                                                                                h.role_code,
                                                                                h.is_evaluation,
                                                                                evaluation_year = ev != null ? ev.evaluation_year : req.evaluation_years
                                                                            } into g

                                                                            orderby g.Key.head_org_unit, g.Key.role_code

                                                                            select new OrgUnitMasterListResModel
                                                                            {
                                                                                org_unit_code = g.Key.code,
                                                                                org_unit_name = g.Key.name,
                                                                                org_unit_role = g.Key.role_code,

                                                                                service_work_score = g.Count(o => o.hevo.evaluation_type == "Service") != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.service_work_score : 0) / g.Count(o => o.hevo.evaluation_type == "Service")), 2) : 0,
                                                                                investigative_work_score = g.Count(o => o.hevo.evaluation_type == "Service") != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.investigative_work_score : 0) / g.Count(o => o.hevo.evaluation_type == "Service")), 2) : 0,
                                                                                crime_prevention_work_score = g.Count(o => o.hevo.evaluation_type == "Service") != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.crime_prevention_work_score : 0) / g.Count(o => o.hevo.evaluation_type == "Service")), 2) : 0,
                                                                                traffic_work_score = g.Count(o => o.hevo.evaluation_type == "Service") != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.traffic_work_score : 0) / g.Count(o => o.hevo.evaluation_type == "Service")), 2) : 0,
                                                                                satisfaction_score = g.Count(o => o.hevo.evaluation_type == "Satisfaction") != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.satisfaction_score : 0) / g.Count(o => o.hevo.evaluation_type == "Satisfaction")), 2) : 0,
                                                                                service_work_count = g.Sum(x => x.ev != null ? x.ev.service_work_count : 0),
                                                                                investigative_work_count = g.Sum(x => x.ev != null ? x.ev.investigative_work_count : 0),
                                                                                crime_prevention_work_count = g.Sum(x => x.ev != null ? x.ev.crime_prevention_work_count : 0),
                                                                                traffic_work_count = g.Sum(x => x.ev != null ? x.ev.traffic_work_count : 0),
                                                                                satisfaction_count = g.Sum(x => x.ev != null ? x.ev.satisfaction_count : 0),
                                                                                evaluators_amount = g.Sum(x => x.ev != null ? x.ev.evaluators_amount : 0),
                                                                                evaluation_year = g.Key.evaluation_year,
                                                                                is_evaluation = g.Key.is_evaluation
                                                                            }).First();

                                result.Add(headEvoluation);
                            }

                        }
                        result.Add(evo);
                    }
                    else
                    {
                        result.Add(evo);
                    }
                }
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
        [Route("importEvaluation")]
        [Authorize]
        public async Task<IActionResult> importEvaluation(List<ImportEvoluationReqModel> req)
        {
            DataResModel res = new DataResModel();
            UserResModel userData = new UserResModel();

            try
            {
                if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) && authHeader.ToString().StartsWith("Bearer "))
                {
                    string token = authHeader.ToString().Substring("Bearer ".Length).Trim();
                    userData = _hashService.DecodingJwtToken(token);
                }

                if (string.IsNullOrEmpty(userData.role_code) || userData.role_code != "RO1")
                {
                    res.status = "error";
                    res.message = "ไม่พบสิทธิ์";
                    return BadRequest(res);
                }

                foreach (ImportEvoluationReqModel evReq in req)
                {

                    EVALUATION ev = _dbContext.evaluation.Where(o => o.org_unit_code == evReq.org_unit_code && o.evaluation_year == evReq.evaluation_year)
                    .FirstOrDefault();

                    if (ev != null)
                    {
                        ev.create_date = DateTime.Now.AddHours(7);
                        ev.create_by = userData.user;
                        ev.service_work_score = evReq.service_work_score;
                        ev.investigative_work_score = evReq.investigative_work_score;
                        ev.crime_prevention_work_score = evReq.crime_prevention_work_score;
                        ev.traffic_work_score = evReq.traffic_work_score;
                        ev.satisfaction_score = evReq.satisfaction_score;
                        ev.service_work_count = evReq.service_work_count;
                        ev.investigative_work_count = evReq.investigative_work_count;
                        ev.crime_prevention_work_count = evReq.crime_prevention_work_count;
                        ev.traffic_work_count = evReq.traffic_work_count;
                        ev.satisfaction_count = evReq.satisfaction_count;
                        ev.evaluators_amount = evReq.evaluators_amount;


                    }
                    else
                    {
                        string codePrefix = "EV";
                        string code;
                        EVALUATION org = _dbContext.evaluation
                                     .OrderByDescending(e => Convert.ToInt32(e.code.Substring(codePrefix.Length, e.code.Length - codePrefix.Length)))
                                     .FirstOrDefault();

                        if (org == null)
                        {
                            code = $"{codePrefix}1";
                        }
                        else
                        {
                            code = codePrefix + (Int64.Parse(org.code.Substring(codePrefix.Length, org.code.Length - codePrefix.Length)) + 1);
                        }

                        _dbContext.evaluation.Add(new EVALUATION
                        {
                            code = code,
                            create_date = DateTime.Now.AddHours(7),
                            create_by = userData.user,
                            org_unit_code = evReq.org_unit_code,
                            service_work_score = evReq.service_work_score,
                            investigative_work_score = evReq.investigative_work_score,
                            crime_prevention_work_score = evReq.crime_prevention_work_score,
                            traffic_work_score = evReq.traffic_work_score,
                            satisfaction_score = evReq.satisfaction_score,
                            service_work_count = evReq.service_work_count,
                            investigative_work_count = evReq.investigative_work_count,
                            crime_prevention_work_count = evReq.crime_prevention_work_count,
                            traffic_work_count = evReq.traffic_work_count,
                            satisfaction_count = evReq.satisfaction_count,
                            evaluators_amount = evReq.evaluators_amount,
                            evaluation_year = evReq.evaluation_year
                        });
                    }

                    _dbContext.SaveChanges();
                }

                res.status = "success";
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
        [Route("deleteEvoluation")]
        [Authorize]
        public async Task<IActionResult> deleteEvoluation(string code)
        {
            DataResModel res = new DataResModel();
            UserResModel userData = new UserResModel();

            if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                authHeader.ToString().StartsWith("Bearer "))
            {
                string token = authHeader.ToString().Substring("Bearer ".Length).Trim();
                userData = _hashService.DecodingJwtToken(token);
            }

            try
            {
                if (string.IsNullOrEmpty(userData.role_code) || userData.role_code != "RO1")
                {
                    res.status = "error";
                    res.message = "ไม่พบสิทธิ์";
                    return BadRequest(res);
                }

                if (!string.IsNullOrEmpty(code))
                {
                    EVALUATION ev = _dbContext.evaluation.Where(o => o.code == code).First();
                    _dbContext.evaluation.Remove(ev);
                    _dbContext.SaveChanges();
                    res.status = "success";
                    res.message = "ลบข้อมูลสำเร็จ";
                }

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
        [Route("searchUser")]
        [Authorize]
        public async Task<IActionResult> searchUser(FilterDashboardReqModel req)
        {
            DataResModel res = new DataResModel();
            UserResModel userData = new UserResModel();
            List<string> org_units = new List<string>();
            List<SearchUserResModel> result = new List<SearchUserResModel>();

            if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                authHeader.ToString().StartsWith("Bearer "))
            {
                string token = authHeader.ToString().Substring("Bearer ".Length).Trim();
                userData = _hashService.DecodingJwtToken(token);
            }

            try
            {

                if (string.IsNullOrEmpty(userData.role_code) || userData.role_code != "RO1")
                {
                    res.status = "error";
                    res.message = "ไม่พบสิทธิ์";
                    return BadRequest(res);
                }

                if (!string.IsNullOrEmpty(req.bch_org_unit)) org_units.Add(req.bch_org_unit);
                if (!string.IsNullOrEmpty(req.bk_org_unit)) org_units.Add(req.bk_org_unit);
                if (!string.IsNullOrEmpty(req.kk_org_unit)) org_units.Add(req.kk_org_unit);
                if (!string.IsNullOrEmpty(req.org_unit)) org_units.Add(req.org_unit);

                var query = from u in _dbContext.user

                            join o in _dbContext.org_unit
                            on new { headCode = u.org_unit_code }
                              equals new { headCode = o.code }

                            join r in _dbContext.role
                            on new { roleCode = u.role_code }
                              equals new { roleCode = r.code }

                            select new SearchUserResModel
                            {
                                id = u.id,
                                user = u.user,
                                password = u.password,
                                role_code = u.role_code,
                                role_name = r.name,
                                org_unit_code = u.org_unit_code,
                                org_unit_name = o.name,
                                head_org_unit = o.head_org_unit,
                            };

                if (org_units.Any())
                {
                    query = query.Where(o =>
                        org_units.Contains(o.org_unit_code) ||
                        (org_units.All(code => EF.Functions.Like(o.head_org_unit, "%" + code + "[_]%"))));
                }

                result = query.ToList();

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
        [Route("updateUser")]
        [Authorize]
        public async Task<IActionResult> updateUser(UpdateUserReqModel req)
        {
            DataResModel res = new DataResModel();
            UserResModel userData = new UserResModel();
            if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                  authHeader.ToString().StartsWith("Bearer "))
            {
                string token = authHeader.ToString().Substring("Bearer ".Length).Trim();
                userData = _hashService.DecodingJwtToken(token);
            }

            try
            {

                if (string.IsNullOrEmpty(userData.role_code) || userData.role_code != "RO1")
                {
                    res.status = "error";
                    res.message = "ไม่พบสิทธิ์";
                    return BadRequest(res);
                }



                if (req.id > 0)
                {
                    USER user = _dbContext.user.Where(o => o.user == req.user).First();
                    user.role_code = req.role_code;
                    user.org_unit_code = req.org_unit_code;
                    if (req.is_reset_password) user.password = _hashService.Hash(req.password);
                }
                else
                {
                    string pass = _hashService.Hash(req.password);

                    _dbContext.user.Add(new USER()
                    {
                        user = req.user,
                        password = pass,
                        create_date = DateTime.Now,
                        role_code = req.role_code,
                        org_unit_code = req.org_unit_code
                    });
                }

                _dbContext.SaveChanges();

                res.status = "success";
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
        [Route("deleteUser")]
        [Authorize]
        public async Task<IActionResult> deleteUser(string user)
        {
            DataResModel res = new DataResModel();
            UserResModel userData = new UserResModel();

            if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                authHeader.ToString().StartsWith("Bearer "))
            {
                string token = authHeader.ToString().Substring("Bearer ".Length).Trim();
                userData = _hashService.DecodingJwtToken(token);
            }

            try
            {
                if (string.IsNullOrEmpty(userData.role_code) || userData.role_code != "RO1")
                {
                    res.status = "error";
                    res.message = "ไม่พบสิทธิ์";
                    return BadRequest(res);
                }

                if (!string.IsNullOrEmpty(user))
                {
                    USER userRes = _dbContext.user.Where(o => o.user == user).First();
                    _dbContext.user.Remove(userRes);
                    _dbContext.SaveChanges();
                    res.status = "success";
                    res.message = "ลบข้อมูลสำเร็จ";
                }

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
        [Route("searchDashboardScoreCompareYears")]
        [Authorize]
        public async Task<IActionResult> searchDashboardScoreCompareYears(FilterDashboardReqModel req)
        {
            DataResModel res = new DataResModel();
            UserResModel userData = new UserResModel();
            List<string> org_units = new List<string>();
            List<OrgUnitMasterListResModel> orgEvo = new List<OrgUnitMasterListResModel>();
            if (HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
             authHeader.ToString().StartsWith("Bearer "))
            {
                string token = authHeader.ToString().Substring("Bearer ".Length).Trim();
                userData = _hashService.DecodingJwtToken(token);
            }

            try
            {
                //if (string.IsNullOrEmpty(userData.role_code) || userData.role_code != "RO1")
                //{
                //    res.status = "error";
                //    res.message = "ไม่พบสิทธิ์";
                //    return BadRequest(res);
                //}

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

                int orgService = orgQuery.Where(o => o.evaluation_type == "Service").ToList().Count();
                int orgSatisfaction = orgQuery.Where(o => o.evaluation_type == "Satisfaction").ToList().Count();

                int[] years = { int.Parse(req.evaluation_years), int.Parse(req.evaluation_years) - 1 };

                foreach (int year in years)
                {
                    OrgUnitMasterListResModel score = (from h in orgQuery
                                                       join ev in _dbContext.evaluation
                                                         on new { HevoCode = h.code, Year = year.ToString() }
                                                         equals new { HevoCode = ev.org_unit_code, Year = ev.evaluation_year } into evGroup
                                                       from ev in evGroup.DefaultIfEmpty()
                                                       group new { h, ev } by new
                                                       {

                                                       } into g
                                                       select new OrgUnitMasterListResModel
                                                       {
                                                           service_work_total = g.Sum(x => x.h != null ? x.h.service_work_total : 0),
                                                           investigative_work_total = g.Sum(x => x.h != null ? x.h.investigative_work_total : 0),
                                                           crime_prevention_work_total = g.Sum(x => x.h != null ? x.h.crime_prevention_work_total : 0),
                                                           traffic_work_total = g.Sum(x => x.h != null ? x.h.traffic_work_total : 0),
                                                           satisfaction_total = g.Sum(x => x.h != null ? x.h.satisfaction_total : 0),
                                                           service_work_score = orgService != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.service_work_score : 0) / orgService), 2) : 0,
                                                           investigative_work_score = orgService != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.investigative_work_score : 0) / orgService), 2) : 0,
                                                           crime_prevention_work_score = orgService != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.crime_prevention_work_score : 0) / orgService), 2) : 0,
                                                           traffic_work_score = orgService != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.traffic_work_score : 0) / orgService), 2) : 0,
                                                           satisfaction_score = orgSatisfaction != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.satisfaction_score : 0) / orgSatisfaction), 2) : 0,
                                                           average_total_score = Math.Round((g.Sum(x => x.h.evaluation_type == "Service"
                                                                                               ? (((x.ev != null ? x.ev.service_work_score : 0) + (x.ev != null ? x.ev.investigative_work_score : 0)
                                                                                               + (x.ev != null ? x.ev.crime_prevention_work_score : 0) + (x.ev != null ? x.ev.traffic_work_score : 0)) / 4)

                                                                                               : (x.ev != null ? x.ev.satisfaction_score : 0)) / g.Count()), 2),
                                                           org_unit_count = g.Count(),
                                                           evaluation_year = year.ToString()
                                                       }).FirstOrDefault();
                    orgEvo.Add(score);
                }



                res.status = "success";
                res.result = orgEvo;
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
        [Route("exportExcel")]
        [Authorize]
        public async Task<IActionResult> exportExcel(FilterDashboardReqModel req)
        {
            DataResModel res = new DataResModel();
            List<string> org_units = new List<string>();
            List<OrgUnitMasterListResModel> result = new List<OrgUnitMasterListResModel>();
            try
            {
                #region select Org Unit Evaluation
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

                List<OrgUnitMasterListResModel> orgEvo = (from h in orgQuery
                                                          join ev in _dbContext.evaluation
                                                            on new { HevoCode = h.code, Year = req.evaluation_years }
                                                            equals new { HevoCode = ev.org_unit_code, Year = ev.evaluation_year }


                                                          select new OrgUnitMasterListResModel
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
                                                                                                                 ? Math.Round((((ev != null ? ev.service_work_score : 0) + (ev != null ? ev.investigative_work_score : 0)
                                                                                                                 + (ev != null ? ev.crime_prevention_work_score : 0) + (ev != null ? ev.traffic_work_score : 0)) / 4), 2)

                                                                                                                 : (ev != null ? ev.satisfaction_score : 0),
                                                              evaluators_amount = ev.evaluators_amount,
                                                              evaluation_year = ev.evaluation_year,
                                                              is_evaluation = h.is_evaluation,
                                                              head_org_unit = (h.head_org_unit.LastIndexOf("_") != -1) ? h.head_org_unit.Substring(0, h.head_org_unit.LastIndexOf("_")) : h.head_org_unit
                                                          }).ToList();

                string head_org_unit = "";
                foreach (OrgUnitMasterListResModel evo in orgEvo)
                {
                    if (head_org_unit != evo.head_org_unit)
                    {
                        head_org_unit = evo.head_org_unit;
                        string[] headOrgs = head_org_unit.Split("_");
                        foreach (string head in headOrgs)
                        {
                            if (result.FirstOrDefault(i => i.org_unit_code == head) == null)
                            {
                                OrgUnitMasterListResModel headEvoluation = (from h in _dbContext.org_unit.Where(o => o.code == head)

                                                                            join hevo in _dbContext.org_unit on 1 equals 1 into hevoGroup
                                                                            from hevo in hevoGroup
                                                                                .Where(hevo =>
                                                                                    (EF.Functions.Like(hevo.head_org_unit, "%" + h.code + "[_]%") || hevo.code == h.code)
                                                                                    && hevo.is_evaluation)
                                                                                .DefaultIfEmpty()

                                                                            join ev in _dbContext.evaluation
                                                                                on new { HevoCode = hevo.code, Year = req.evaluation_years }
                                                                                equals new { HevoCode = ev.org_unit_code, Year = ev.evaluation_year }
                                                                                into evGroup
                                                                            from ev in evGroup.DefaultIfEmpty()

                                                                            group new { h, ev, hevo } by new
                                                                            {
                                                                                h.code,
                                                                                h.name,
                                                                                h.head_org_unit,
                                                                                h.role_code,
                                                                                h.is_evaluation,
                                                                                evaluation_year = ev != null ? ev.evaluation_year : req.evaluation_years
                                                                            } into g

                                                                            orderby g.Key.head_org_unit, g.Key.role_code

                                                                            select new OrgUnitMasterListResModel
                                                                            {
                                                                                org_unit_code = g.Key.code,
                                                                                org_unit_name = g.Key.name,
                                                                                org_unit_role = g.Key.role_code,
                                                                                service_work_score = g.Count(o => o.hevo.evaluation_type == "Service") != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.service_work_score : 0) / g.Count(o => o.hevo.evaluation_type == "Service")), 2) : 0,
                                                                                investigative_work_score = g.Count(o => o.hevo.evaluation_type == "Service") != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.investigative_work_score : 0) / g.Count(o => o.hevo.evaluation_type == "Service")), 2) : 0,
                                                                                crime_prevention_work_score = g.Count(o => o.hevo.evaluation_type == "Service") != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.crime_prevention_work_score : 0) / g.Count(o => o.hevo.evaluation_type == "Service")), 2) : 0,
                                                                                traffic_work_score = g.Count(o => o.hevo.evaluation_type == "Service") != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.traffic_work_score : 0) / g.Count(o => o.hevo.evaluation_type == "Service")), 2) : 0,
                                                                                satisfaction_score = g.Count(o => o.hevo.evaluation_type == "Satisfaction") != 0 ? Math.Round((g.Sum(x => x.ev != null ? x.ev.satisfaction_score : 0) / g.Count(o => o.hevo.evaluation_type == "Satisfaction")), 2) : 0,

                                                                                service_work_count = g.Sum(x => x.ev != null ? x.ev.service_work_count : 0),
                                                                                investigative_work_count = g.Sum(x => x.ev != null ? x.ev.investigative_work_count : 0),
                                                                                crime_prevention_work_count = g.Sum(x => x.ev != null ? x.ev.crime_prevention_work_count : 0),
                                                                                traffic_work_count = g.Sum(x => x.ev != null ? x.ev.traffic_work_count : 0),
                                                                                satisfaction_count = g.Sum(x => x.ev != null ? x.ev.satisfaction_count : 0),
                                                                                evaluators_amount = g.Sum(x => x.ev != null ? x.ev.evaluators_amount : 0),

                                                                                average_total_score = Math.Round((g.Sum(x => x.hevo.evaluation_type == "Service"
                                                                                               ? (((x.ev != null ? x.ev.service_work_score : 0) + (x.ev != null ? x.ev.investigative_work_score : 0)
                                                                                               + (x.ev != null ? x.ev.crime_prevention_work_score : 0) + (x.ev != null ? x.ev.traffic_work_score : 0)) / 4)

                                                                                               : (x.ev != null ? x.ev.satisfaction_score : 0)) / g.Count()), 2),
                                                                                evaluation_year = g.Key.evaluation_year,
                                                                                is_evaluation = g.Key.is_evaluation
                                                                            }).First();

                                result.Add(headEvoluation);
                            }

                        }
                        result.Add(evo);
                    }
                    else
                    {
                        result.Add(evo);
                    }
                }
                #endregion

                #region create excel

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Report");


                // เพิ่ม Header
                worksheet.Column("A").Width = 15;
                worksheet.Column("B").Width = 25;
                worksheet.Column("C").Width = 25;
                worksheet.Column("D").Width = 25;
                worksheet.Column("E").Width = 25;
                worksheet.Column("F").Width = 25;
                worksheet.Column("G").Width = 25;
                worksheet.Column("H").Width = 35;
                worksheet.Column("I").Width = 15;
                worksheet.Column("J").Width = 20;
                worksheet.Column("K").Width = 30;

                worksheet.Cell("A1").Value = "รหัสหน่วยงาน";
                worksheet.Cell("B1").Value = "บช.";
                worksheet.Cell("C1").Value = "บก.";
                worksheet.Cell("D1").Value = "กก.";
                worksheet.Cell("E1").Value = "หน่วยงาน";

                worksheet.Cell("F1").Value = "คะแนนความพึงพอใจ สภ./สน.";
                worksheet.Range("F1", "I1").Merge();

                worksheet.Cell("J1").Value = "คะแนนความพึงพอใจ\r\nโดยรวม";
                worksheet.Cell("K1").Value = "ผลประเมิน";

                //// Sub-headers under "คะแนนความพึงพอใจเฉลี่ย"
                worksheet.Cell("F2").Value = "งานบริการสถานีตำรวจ";
                worksheet.Cell("G2").Value = "งานสืบสวนสอบสวน";
                worksheet.Cell("H2").Value = "งานป้องกันปราบปรามอาชญากรรม";
                worksheet.Cell("I2").Value = "งานจราจร";

                //// Merge vertically for non-subheading columns
                worksheet.Range("A1", "A2").Merge();
                worksheet.Range("B1", "B2").Merge();
                worksheet.Range("C1", "C2").Merge();
                worksheet.Range("D1", "D2").Merge();
                worksheet.Range("E1", "E2").Merge();
                worksheet.Range("J1", "J2").Merge();
                worksheet.Range("K1", "K2").Merge();

                // Styling (optional but recommended)
                worksheet.Range("A1:L2").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                worksheet.Range("A1:L2").Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                worksheet.Range("A1:L2").Style.Font.SetBold();


                // Style header
                var headerRange = worksheet.Range(1, 1, 2, 12);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.Gainsboro;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                string BCHText = "";
                string BKText = "";
                string KKText = "";
                string ORGText = "";

                for (int i = 0; i < result.Count; i++)
                {
                    var row = i + 3;

                    if (result[i].is_evaluation) worksheet.Cell(row, 1).Value = result[i].org_unit_code;


                    if (result[i].org_unit_role == "RO2")
                    {
                        BCHText = result[i].org_unit_name;
                        BKText = "";
                        KKText = "";
                        ORGText = "";
                    }

                    if (result[i].org_unit_role == "RO3")
                    {
                        BKText = result[i].org_unit_name;
                        KKText = "";
                        ORGText = "";
                    }

                    if (result[i].org_unit_role == "RO4")
                    {
                        KKText = result[i].org_unit_name;
                        ORGText = "";
                    }

                    if (result[i].org_unit_role == "RO5") ORGText = result[i].org_unit_name;

                    worksheet.Cell(row, 2).Value = BCHText;
                    worksheet.Cell(row, 3).Value = BKText;
                    worksheet.Cell(row, 4).Value = KKText;
                    worksheet.Cell(row, 5).Value = ORGText;

                    worksheet.Cell(row, 6).Value = result[i].service_work_score;
                    worksheet.Cell(row, 7).Value = result[i].investigative_work_score;
                    worksheet.Cell(row, 8).Value = result[i].crime_prevention_work_score;
                    worksheet.Cell(row, 9).Value = result[i].traffic_work_score;
                    //worksheet.Cell(row, 10).Value = result[i].satisfaction_score;
                    worksheet.Cell(row, 10).Value = result[i].average_total_score;

                    if (result[i].average_total_score <= 2.33m)
                    {
                        worksheet.Cell(row, 11).Value = "ผลประเมินอยู่ในระดับปรับปรุง";
                        worksheet.Cell(row, 11).Style.Fill.BackgroundColor = XLColor.FromHtml("#cf4547");
                    }

                    if (2.33m <= result[i].average_total_score && result[i].average_total_score <= 3.66m)
                    {
                        worksheet.Cell(row, 11).Value = "ผลประเมินอยู่ในระดับพอใช้";
                        worksheet.Cell(row, 11).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffb000");
                    }

                    if (3.67m <= result[i].average_total_score && result[i].average_total_score <= 5)
                    {
                        worksheet.Cell(row, 11).Value = "ผลประเมินอยู่ในระดับดี";
                        worksheet.Cell(row, 11).Style.Fill.BackgroundColor = XLColor.FromHtml("#66dd66");
                    }

                    if (result[i].org_unit_role == "RO2" && !result[i].is_evaluation) worksheet.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#cf772e");
                    if (result[i].org_unit_role == "RO3" && !result[i].is_evaluation) worksheet.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#bde451");
                    if (result[i].org_unit_role == "RO4" && !result[i].is_evaluation) worksheet.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#3eca9b");

                }

                // Auto adjust column width
                //worksheet.Columns().AdjustToContents();
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;
                string excelName = $"Export-{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                #endregion

                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName
        );
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
