namespace police_poll_service.models.respone
{
    public class OrgUnitMasterListResModel
    {
        public int id { get; set; }
        public string org_unit_code { get; set; }
        public string org_unit_name { get; set; }
        public string org_unit_role { get; set; }
        public string evaluation_code { get; set; }
        public int service_work_total { get; set; }
        public int investigative_work_total { get; set; }
        public int crime_prevention_work_total { get; set; }
        public int traffic_work_total { get; set; }
        public int satisfaction_total { get; set; }
        public int evaluators_total { get; set; }
        public string evaluation_type { get; set; }
        public bool is_evaluation { get; set; }
        public string head_org_unit { get; set; }
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
        public decimal average_total_score { get; set; }
        public int evaluators_amount { get; set; }
        public string evaluation_year { get; set; }
        public int org_unit_count { get; set; }
        public List<HeadOrgUnitItemResModel> head_role_orgs { get; set; }
    }
}
