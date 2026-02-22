namespace police_poll_service.models.respone
{
    public class OrgUnitEvaClientResModel
    {
        public int org_unit_total { get; set; }
        public int org_unit_evaluation_complete { get; set; }
        public int evaluators_total { get; set; }
        public int evaluators_amount { get; set; }
        public string evaluation_date { get; set; }
    }
}
