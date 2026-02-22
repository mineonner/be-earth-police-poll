namespace police_poll_service.models.respone
{
    public class DashboardResModel
    {
        public string org_unit_code { get; set; }
        public string org_unit_name { get; set; }
        public int evaluators_total { get; set; }
        public int evaluators_count { get; set; }
        public int unevaluators_count { get; set; }
        public decimal score_total { get; set; }
        public int service_work_total { get; set; }
        public int service_work_count { get; set; }
        public int investigative_work_total { get; set; }
        public int investigative_work_count { get; set; }
        public int crime_prevention_work_total { get; set; }
        public int crime_prevention_work_count { get; set; }
        public int traffic_work_total { get; set; }
        public int traffic_work_count { get; set; }
        public int satisfaction_total { get; set; }
        public int satisfaction_count { get; set; }
        public string evaluation_date { get; set; }
        public DateTime org_unit_creat_date { get; set; }

    }
}
