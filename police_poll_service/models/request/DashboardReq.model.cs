using System.ComponentModel.DataAnnotations;

namespace police_poll_service.models.request
{
    public class DashboardReqModel
    {
        public string[] head_org { get; set; }
        [Required]
        public string years { get; set; }
    }
}
