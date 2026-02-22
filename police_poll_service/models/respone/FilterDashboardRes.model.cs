namespace police_poll_service.models.respone
{
    public class FilterDashboardResModel
    {
        public AvaluatorsItemResModel bch_org_unit { get; set; }
        public AvaluatorsItemResModel bk_org_unit { get; set; }
        public AvaluatorsItemResModel kk_org_unit { get; set; }
        public AvaluatorsItemResModel org_unit { get; set; }
        public List<OrgUnitMasterListResModel> org_unit_evoluation_item_list { get; set; }
    }
}
