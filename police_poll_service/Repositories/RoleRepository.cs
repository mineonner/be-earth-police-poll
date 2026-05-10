using Microsoft.EntityFrameworkCore;
using police_poll_service.DB;
using police_poll_service.models.request;
using police_poll_service.models.respone;

namespace police_poll_service.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly PolicePollDbContext _db;

        public RoleRepository(PolicePollDbContext db)
        {
            _db = db;
        }

        public List<OrgUnitDropdownResModel> GetRoleDropdown(BaseDropdownRequest req)
        {
            var query = _db.role.AsNoTracking().Where(orgDD =>
                (string.IsNullOrEmpty(req.search_text) || EF.Functions.Like(orgDD.name, "%" + req.search_text + "%"))
            );

            if (req.selected_code.Length > 0)
            {
                var selectedCodes = _db.role.AsNoTracking().Where(o => req.selected_code.Contains(o.code));
                query = selectedCodes.Concat(query);
            }

            if (req.except_codes.Length > 0)
            {
                query = query.Where(o => !req.except_codes.Contains(o.code));
            }

            return query.Select(o => new OrgUnitDropdownResModel
            {
                id = o.code,
                name = o.name
            }).Take(req.max_length).ToList();
        }
    }
}
