using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using police_poll_service.models.request;
using police_poll_service.models.respone;
using police_poll_service.Repositories;
using police_poll_service.services;
using report_meesuanruam_service.services;

namespace police_poll_service.Controllers
{
    [Route("api/police-pollAd")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly HashService _hashService;
        private readonly PolicePollService _pSer;
        private readonly IUserRepository _users;
        private readonly IOrgUnitRepository _orgUnits;
        private readonly IConfigRepository _config;
        private readonly IDashboardRepository _dashboard;
        private readonly IRoleRepository _roles;
        private readonly IEvaluationRepository _evaluations;
        private readonly IOrgUnitEvaluationReportRepository _evaluationReports;

        public AdminController(
            IConfiguration config,
            IUserRepository users,
            IOrgUnitRepository orgUnits,
            IConfigRepository configRepository,
            IDashboardRepository dashboard,
            IRoleRepository roles,
            IEvaluationRepository evaluations,
            IOrgUnitEvaluationReportRepository evaluationReports)
        {
            _hashService = new HashService(config);
            _pSer = new PolicePollService();
            _users = users;
            _orgUnits = orgUnits;
            _config = configRepository;
            _dashboard = dashboard;
            _roles = roles;
            _evaluations = evaluations;
            _evaluationReports = evaluationReports;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> login(LoginReqModel log)
        {
            DataResModel res = new DataResModel();
            try
            {
                UserResModel? result = _users.GetForLogin(log.user);

                if (result != null && _hashService.Verify(log.password, result.password))
                {
                    result.password = "";
                    if (!String.IsNullOrEmpty(result.org_unit_code))
                    {
                        result.org_unit_name = _orgUnits.GetOrgUnitName(result.org_unit_code) ?? "";
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
            try
            {
                List<DashboardResModel> dashResult = _dashboard.GetDashboard(dash);

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
                List<OrgUnitDropdownResModel> result = _orgUnits.GetOrgUnitDropdown(req);

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
            try
            {
                FilterDashboardResModel result = _evaluationReports.SearchFilterDashboard(req);

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

                List<OrgUnitMasterListResModel> result = _orgUnits.SearchOrgUnitMasterList(req);

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

                result = _roles.GetRoleDropdown(req);

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
            string head_org_unit = string.Join("_", req.head_role_orgs?.Select(p => p.org_unit_code) ?? Enumerable.Empty<string>());

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
                _orgUnits.SaveOrgUnit(req, head_org_unit, userData.user ?? "");
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
                    _orgUnits.DeleteOrgUnitCascade(code);
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

                List<OrgUnitMasterListResModel> result = _evaluationReports.SearchEvaluation(req);
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

                _evaluations.ImportEvaluations(req, userData.user ?? "");

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
                    _evaluations.DeleteByCode(code);
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

                result = _users.SearchUsers(org_units);

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



                string? passwordHash = null;
                if (req.id > 0)
                {
                    if (req.is_reset_password)
                        passwordHash = _hashService.Hash(req.password);
                }
                else
                {
                    passwordHash = _hashService.Hash(req.password);
                }

                _users.UpdateOrCreateUser(req, passwordHash);

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
                    _users.DeleteUser(user);
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

            try
            {
                List<OrgUnitMasterListResModel> orgEvo = _evaluationReports.SearchDashboardScoreCompareYears(req);

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
            try
            {
                List<OrgUnitMasterListResModel> result = _evaluationReports.GetOrgUnitRowsForExcelExport(req);

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

        [HttpPost]
        [Route("importOrgUnitMaster")]
        [Authorize]
        public async Task<IActionResult> importOrgUnitMaster(List<ImportOrgUnitReqModel> req)
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

                _orgUnits.ImportOrgUnitMasters(req, userData.user ?? "");

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
    }
}
