using Microsoft.AspNetCore.Mvc;
using police_poll_service.models.request;
using police_poll_service.models.respone;
using police_poll_service.Repositories;

namespace police_poll_service.Controllers
{
    [Route("api/police-poll")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly IOrgUnitRepository _orgUnits;
        private readonly IClientPollRepository _clientPoll;

        public ClientController(IOrgUnitRepository orgUnits, IClientPollRepository clientPoll)
        {
            _orgUnits = orgUnits;
            _clientPoll = clientPoll;
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
                List<OrgUnitDropdownResModel> result = _orgUnits.GetOrgUnitDropdown(req);

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
                result = await _clientPoll.GetOrgUnitEvaluationSummaryAsync(years);

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
            try
            {
                result = await _clientPoll.SearchEvaluationProgressAsync(req);

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
