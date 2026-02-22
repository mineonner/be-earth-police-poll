using System.ComponentModel.DataAnnotations;

namespace police_poll_service.models.request
{
    public class UpdateUserReqModel
    {
        [Required]
        public Int64 id { get; set; }
        [Required]
        public string user { get; set; }
        [Required]
        public string password { get; set; }
        [Required]
        public string role_code { get; set; }
        [Required]
        public string org_unit_code { get; set; }
        [Required]
        public Boolean is_reset_password { get; set; }
    }
}
