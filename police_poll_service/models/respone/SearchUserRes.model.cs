namespace police_poll_service.models.respone
{
    public class SearchUserResModel
    {
        public Int64 id { get; set; }
        public string user { get; set; }
        public string password { get; set; }
        public string role_code { get; set; }
        public string role_name { get; set; }
        public string org_unit_code { get; set; }
        public string org_unit_name { get; set; }
        public string head_org_unit { get; set; }
    }
}
