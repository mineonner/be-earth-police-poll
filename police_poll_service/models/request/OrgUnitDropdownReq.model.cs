using System.ComponentModel.DataAnnotations;

namespace police_poll_service.models.request
{
    public class OrgUnitDropdownReqModel : BaseDropdownRequest
    {
        [Required]
        public string[] org_units { get; set; }
        [Required]
        public string role_code { get; set; }
        public Boolean is_head_org { get; set; }
    }
}
