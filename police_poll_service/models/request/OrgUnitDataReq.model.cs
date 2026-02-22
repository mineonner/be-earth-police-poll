using police_poll_service.models.respone;
using System.ComponentModel.DataAnnotations;

namespace police_poll_service.models.request
{
    public class OrgUnitDataReqModel
    {
        [Required]
        public int id { get; set; }
        public string? org_unit_code { get; set; }
        [Required]
        public string org_unit_name { get; set; }
        [Required]
        public string org_unit_role { get; set; }
        [Required]
        public int service_work_total { get; set; }
        [Required]
        public int investigative_work_total { get; set; }
        [Required]
        public int crime_prevention_work_total { get; set; }
        [Required]
        public int traffic_work_total { get; set; }
        [Required]
        public int satisfaction_total { get; set; }
        [Required]
        public int evaluators_total { get; set; }
        public string? evaluation_type { get; set; }
        [Required]
        public bool is_evaluation { get; set; }
        public string? head_org_unit { get; set; }
        public List<HeadOrgUnitItemResModel>? head_role_orgs { get; set; }
    }
}
