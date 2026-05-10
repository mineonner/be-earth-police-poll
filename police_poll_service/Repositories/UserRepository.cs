using Microsoft.EntityFrameworkCore;
using police_poll_service.DB;
using police_poll_service.DB.Tables;
using police_poll_service.models.request;
using police_poll_service.models.respone;

namespace police_poll_service.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly PolicePollDbContext _db;

        public UserRepository(PolicePollDbContext db)
        {
            _db = db;
        }

        public UserResModel? GetForLogin(string username)
        {
            return (from u in _db.user.AsNoTracking()
                    join ro in _db.role.AsNoTracking() on u.role_code equals ro.code
                    where u.user == username
                    select new UserResModel
                    {
                        user = u.user,
                        password = u.password,
                        role_code = u.role_code,
                        role_name = ro.name,
                        org_unit_code = u.org_unit_code,
                        org_unit_name = "",
                        token = ""
                    }).SingleOrDefault();
        }

        public List<SearchUserResModel> SearchUsers(IReadOnlyList<string> orgUnits)
        {
            var query = from u in _db.user.AsNoTracking()
                        join o in _db.org_unit.AsNoTracking() on new { headCode = u.org_unit_code } equals new { headCode = o.code }
                        join r in _db.role.AsNoTracking() on new { roleCode = u.role_code } equals new { roleCode = r.code }
                        select new SearchUserResModel
                        {
                            id = u.id,
                            user = u.user,
                            password = u.password,
                            role_code = u.role_code,
                            role_name = r.name,
                            org_unit_code = u.org_unit_code,
                            org_unit_name = o.name,
                            head_org_unit = o.head_org_unit,
                        };

            if (orgUnits.Any())
            {
                query = query.Where(o =>
                    orgUnits.Contains(o.org_unit_code) ||
                    orgUnits.All(code => EF.Functions.Like(o.head_org_unit, "%" + code + "[_]%")));
            }

            return query.ToList();
        }

        public void UpdateOrCreateUser(UpdateUserReqModel req, string? passwordHashForNewOrReset)
        {
            if (req.id > 0)
            {
                USER user = _db.user.Where(o => o.user == req.user).First();
                user.role_code = req.role_code;
                user.org_unit_code = req.org_unit_code;
                if (req.is_reset_password && passwordHashForNewOrReset != null)
                    user.password = passwordHashForNewOrReset;
            }
            else
            {
                if (passwordHashForNewOrReset == null)
                    throw new InvalidOperationException("Password hash required for new user.");

                _db.user.Add(new USER
                {
                    user = req.user,
                    password = passwordHashForNewOrReset,
                    create_date = DateTime.Now,
                    role_code = req.role_code,
                    org_unit_code = req.org_unit_code
                });
            }

            _db.SaveChanges();
        }

        public void DeleteUser(string user)
        {
            USER userRes = _db.user.Where(o => o.user == user).First();
            _db.user.Remove(userRes);
            _db.SaveChanges();
        }
    }
}
