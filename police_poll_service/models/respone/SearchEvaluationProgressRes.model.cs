namespace police_poll_service.models.respone
{
    public class SearchEvaluationProgressResModel : OrgUnitEvaClientResModel
    {
        public int service_work_total { get; set; }
        public int investigative_work_total { get; set; }
        public int crime_prevention_work_total { get; set; }
        public int traffic_work_total { get; set; }
        public int satisfaction_total { get; set; }
        public int service_work_count { get; set; }
        public int investigative_work_count { get; set; }
        public int crime_prevention_work_count { get; set; }
        public int traffic_work_count { get; set; }
        public int satisfaction_count { get; set; }
    }
}
