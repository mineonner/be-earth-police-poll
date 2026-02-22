namespace police_poll_service.models.respone
{
    public class UserResModel
    {
        public string? user { get; set; }
        public string? password { get; set; }
        public string? role_code { get; set; }
        public string? role_name { get; set; }
        public string? org_unit_code { get; set; }
        public string? org_unit_name { get; set; }
        public string? token { get; set; }
    }
}
