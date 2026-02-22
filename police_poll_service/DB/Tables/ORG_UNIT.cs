namespace police_poll_service.DB.Tables
{
    public class ORG_UNIT
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public DateTime create_date { get; set; }
        public string create_by { get; set; }
        public string role_code { get; set; }
        public string? evaluation_type { get; set; }
        public bool is_evaluation { get; set; }
        public string head_org_unit { get; set; }
        public int service_work_total { get; set; }
        public int investigative_work_total { get; set; }
        public int crime_prevention_work_total { get; set; }
        public int traffic_work_total { get; set; }
        public int satisfaction_total { get; set; }
        public int evaluators_total { get; set; }

    }
}
