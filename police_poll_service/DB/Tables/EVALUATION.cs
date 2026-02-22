namespace police_poll_service.DB.Tables
{
    public class EVALUATION
    {
        public int id { get; set; }
        public string code { get; set; }
        public DateTime create_date { get; set; }
        public string create_by { get; set; }
        public string org_unit_code { get; set; }
        public decimal service_work_score { get; set; }
        public int service_work_count { get; set; }
        public decimal investigative_work_score { get; set; }
        public int investigative_work_count { get; set; }
        public decimal crime_prevention_work_score { get; set; }
        public int crime_prevention_work_count { get; set; }
        public decimal traffic_work_score { get; set; }
        public int traffic_work_count { get; set; }
        public decimal satisfaction_score { get; set; }
        public int satisfaction_count { get; set; }
        public int evaluators_amount { get; set; }
        public string evaluation_year { get; set; }
    }
}
