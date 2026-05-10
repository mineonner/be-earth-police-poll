using police_poll_service.models.request;
using police_poll_service.models.respone;

namespace police_poll_service.Repositories
{
    public interface IOrgUnitRepository
    {
        List<OrgUnitDropdownResModel> GetOrgUnitDropdown(OrgUnitDropdownReqModel req);
        string? GetOrgUnitName(string code);
        List<OrgUnitMasterListResModel> SearchOrgUnitMasterList(OrgUnitMasterListReqModel req);
        void SaveOrgUnit(OrgUnitDataReqModel req, string headOrgUnitJoined, string currentUser);
        void DeleteOrgUnitCascade(string code);
        void ImportOrgUnitMasters(IEnumerable<ImportOrgUnitReqModel> items, string createByUser);
    }
}
