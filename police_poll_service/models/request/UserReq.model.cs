using System.ComponentModel.DataAnnotations;

namespace police_poll_service.models.request
{
    public class UserReqModel
    {
        [Required]
        public string user { get; set; }
        [Required]
        public string password { get; set; }
        public string token { get; set; }
        public string create_date { get; set; }
        [Required]
        public string role_code { get; set; }
        public string org_unit_code { get; set; }
    }
}
