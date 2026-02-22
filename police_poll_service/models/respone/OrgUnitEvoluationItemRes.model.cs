namespace police_poll_service.models.respone
{
    public class OrgUnitEvoluationItemResModel
    {
        public string org_unit_code { get; set; }
        public string org_unit_name { get; set; }
        public string org_role { get; set; }
        public decimal service_work_score { get; set; }
        public decimal investigative_work_score { get; set; }
        public decimal crime_prevention_work_score { get; set; }
        public decimal traffic_work_score { get; set; }
        public decimal satisfaction_score { get; set; }
        public decimal average_total_score { get; set; }
        public int service_work_count { get; set; }
        public int investigative_work_count { get; set; }
        public int crime_prevention_work_count { get; set; }
        public int traffic_work_count { get; set; }
        public int satisfaction_count { get; set; }
        public Boolean is_head { get; set; }
        public string evaluation_type { get; set; }
    }
}
