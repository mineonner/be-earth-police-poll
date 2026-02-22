namespace police_poll_service.DB.Tables
{
    public class USER
    {
        public Int64 id { get; set; }
        public string user { get; set; }
        public string password { get; set; }
        public string? token { get; set; }
        public DateTime create_date { get; set; }
        public string role_code { get; set; }
        public string? org_unit_code { get; set; }
    }
}
