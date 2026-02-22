using System.ComponentModel.DataAnnotations;

namespace police_poll_service.models.request
{
    public class LoginReqModel
    {
        [Required]
        public string user { get; set; }
        [Required]
        public string password { get; set; }
    }
}
