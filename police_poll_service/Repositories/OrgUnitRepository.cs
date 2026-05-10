using Microsoft.EntityFrameworkCore;
using police_poll_service.DB;
using police_poll_service.DB.Tables;
using police_poll_service.models.request;
using police_poll_service.models.respone;

namespace police_poll_service.Repositories
{
    public class OrgUnitRepository : IOrgUnitRepository
    {
        private readonly PolicePollDbContext _db;

        public OrgUnitRepository(PolicePollDbContext db)
        {
            _db = db;
        }

        public List<OrgUnitDropdownResModel> GetOrgUnitDropdown(OrgUnitDropdownReqModel req)
        {
            var query = _db.org_unit.AsNoTracking();

            query = query.Where(orgDD =>
                    req.role_code == orgDD.role_code &&
                    (
                        (req.is_head_org
                            ? (req.org_units.Any(code => EF.Functions.Like(orgDD.head_org_unit, "%" + code + "[_]%")) || req.org_units.Contains(orgDD.code))
                            : (req.org_units.All(code => EF.Functions.Like(orgDD.head_org_unit, "%" + code + "[_]%")) || req.org_units.Contains(orgDD.code))
                         ) || req.org_units.Length == 0
                    )
                ).OrderBy(o => o.id);

            query = query.Where(orgDD =>
                (string.IsNullOrEmpty(req.search_text) || EF.Functions.Like(orgDD.name, "%" + req.search_text + "%"))
            );

            if (req.selected_code.Length > 0)
            {
                var selectedCodes = _db.org_unit.AsNoTracking().Where(o => req.selected_code.Contains(o.code));
                query = selectedCodes.Concat(query);
            }

            return query
                .Select(orgDD => new OrgUnitDropdownResModel
                {
                    id = orgDD.code,
                    name = orgDD.name
                })
                .Take(req.max_length)
                .ToList();
        }

        public string? GetOrgUnitName(string code) =>
            _db.org_unit.AsNoTracking().Where(b => b.code == code).Select(o => o.name).FirstOrDefault();

        public List<OrgUnitMasterListResModel> SearchOrgUnitMasterList(OrgUnitMasterListReqModel req)
        {
            List<string> org_units = new List<string>();
            if (!string.IsNullOrEmpty(req.bch_org_unit)) org_units.Add(req.bch_org_unit);
            if (!string.IsNullOrEmpty(req.bk_org_unit)) org_units.Add(req.bk_org_unit);
            if (!string.IsNullOrEmpty(req.kk_org_unit)) org_units.Add(req.kk_org_unit);
            if (!string.IsNullOrEmpty(req.org_unit)) org_units.Add(req.org_unit);

            List<OrgUnitMasterListResModel> result = new List<OrgUnitMasterListResModel>();

            if (org_units.Count > 0)
            {
                var anchorPaths = _db.org_unit.AsNoTracking()
                    .Where(ou => org_units.Contains(ou.code))
                    .Select(ou => ou.head_org_unit)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                var headCodesOrdered = new List<string>();
                var headCodeSeen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var path in anchorPaths)
                {
                    foreach (var part in path.Split('_', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (headCodeSeen.Add(part))
                            headCodesOrdered.Add(part);
                    }
                }

                if (headCodesOrdered.Count == 0)
                    return result;

                var headEntities = _db.org_unit.AsNoTracking()
                    .Where(o => headCodesOrdered.Contains(o.code))
                    .ToDictionary(o => o.code, StringComparer.Ordinal);

                foreach (string head in headCodesOrdered)
                {
                    if (!headEntities.TryGetValue(head, out var ent))
                        throw new InvalidOperationException($"ORG_UNIT not found for head code: {head}");
                    result.Add(MapToMasterListRes(ent));
                }

                var subHeadRows = _db.org_unit.AsNoTracking()
                    .Where(o => headCodesOrdered.All(code => EF.Functions.Like(o.head_org_unit, "%" + code + "[_]%")))
                    .OrderBy(o => o.head_org_unit)
                    .ThenBy(o => o.role_code)
                    .ToList();

                foreach (var o in subHeadRows)
                    result.Add(MapToMasterListRes(o));
            }
            else
            {
                var allRows = _db.org_unit.AsNoTracking()
                    .OrderBy(o => o.head_org_unit)
                    .ThenBy(o => o.role_code)
                    .ToList();

                foreach (var o in allRows)
                    result.Add(MapToMasterListRes(o));

                FillHeadRoleOrgs(result, allRows.ToDictionary(o => o.code, StringComparer.Ordinal));
                return result;
            }

            FillHeadRoleOrgs(result);
            return result;
        }

        private static OrgUnitMasterListResModel MapToMasterListRes(ORG_UNIT o) => new OrgUnitMasterListResModel
        {
            id = o.id,
            org_unit_code = o.code,
            org_unit_name = o.name,
            org_unit_role = o.role_code,
            service_work_total = o.service_work_total,
            investigative_work_total = o.investigative_work_total,
            crime_prevention_work_total = o.crime_prevention_work_total,
            traffic_work_total = o.traffic_work_total,
            satisfaction_total = o.satisfaction_total,
            evaluation_type = o.evaluation_type,
            is_evaluation = o.is_evaluation,
            head_org_unit = o.head_org_unit,
            head_role_orgs = new List<HeadOrgUnitItemResModel>()
        };

        private void FillHeadRoleOrgs(List<OrgUnitMasterListResModel> result, Dictionary<string, ORG_UNIT>? preloadedByCode = null)
        {
            var codesNeeded = new HashSet<string>(StringComparer.Ordinal);
            foreach (var org in result)
            {
                foreach (var part in org.head_org_unit.Split('_', StringSplitOptions.RemoveEmptyEntries))
                    codesNeeded.Add(part);
            }

            if (codesNeeded.Count == 0)
                return;

            Dictionary<string, ORG_UNIT> byCode;
            if (preloadedByCode != null)
            {
                byCode = new Dictionary<string, ORG_UNIT>(StringComparer.Ordinal);
                foreach (var c in codesNeeded)
                {
                    if (preloadedByCode.TryGetValue(c, out var row))
                        byCode[c] = row;
                }
            }
            else
            {
                byCode = _db.org_unit.AsNoTracking()
                    .Where(o => codesNeeded.Contains(o.code))
                    .ToDictionary(o => o.code, StringComparer.Ordinal);
            }

            foreach (var org in result)
            {
                var chain = org.head_org_unit.Split('_', StringSplitOptions.RemoveEmptyEntries)
                    .Where(byCode.ContainsKey)
                    .Select(c => byCode[c])
                    .OrderBy(x => x.head_org_unit)
                    .ThenBy(x => x.role_code)
                    .Select(x => new HeadOrgUnitItemResModel
                    {
                        role_code = x.role_code,
                        org_unit_code = x.code
                    })
                    .ToList();

                org.head_role_orgs.AddRange(chain);
            }
        }

        public void SaveOrgUnit(OrgUnitDataReqModel req, string headOrgUnitJoined, string currentUser)
        {
            string head_org_unit = headOrgUnitJoined;

            if (req.id > 0)
            {
                ORG_UNIT org = _db.org_unit.Single(o => o.code == req.org_unit_code && o.role_code == req.org_unit_role);
                head_org_unit = string.IsNullOrEmpty(head_org_unit) ? org.code : $"{head_org_unit}_{org.code}";

                org.name = req.org_unit_name;
                org.evaluation_type = req.evaluation_type;
                org.service_work_total = req.service_work_total;
                org.investigative_work_total = req.investigative_work_total;
                org.crime_prevention_work_total = req.crime_prevention_work_total;
                org.traffic_work_total = req.traffic_work_total;
                org.satisfaction_total = req.satisfaction_total;
                org.evaluators_total = req.evaluators_total;
                org.is_evaluation = req.is_evaluation;
                org.head_org_unit = head_org_unit;
            }
            else
            {
                string code;
                if (req.is_evaluation)
                {
                    code = req.org_unit_code!;
                }
                else
                {
                    string codePrefix = "";
                    if (req.org_unit_role == "RO2") codePrefix = "BCH";
                    if (req.org_unit_role == "RO3") codePrefix = "BK";
                    if (req.org_unit_role == "RO4") codePrefix = "KK";
                    if (req.org_unit_role == "RO5") codePrefix = "ORG";
                    ORG_UNIT? org = _db.org_unit.Where(o => EF.Functions.Like(o.code, codePrefix + "%") && o.role_code == req.org_unit_role)
                             .OrderByDescending(e => Convert.ToInt32(e.code.Substring(codePrefix.Length, e.code.Length - codePrefix.Length)))
                             .FirstOrDefault();

                    if (org == null)
                    {
                        code = $"{codePrefix}1";
                    }
                    else
                    {
                        code = codePrefix + (Int64.Parse(org.code.Substring(codePrefix.Length, org.code.Length - codePrefix.Length)) + 1);
                    }
                }

                head_org_unit = string.IsNullOrEmpty(head_org_unit) ? code : $"{head_org_unit}_{code}";

                _db.org_unit.Add(new ORG_UNIT()
                {
                    code = code,
                    name = req.org_unit_name,
                    create_date = DateTime.Now.AddHours(7),
                    create_by = currentUser,
                    role_code = req.org_unit_role,
                    evaluation_type = req.evaluation_type,
                    is_evaluation = req.is_evaluation,
                    head_org_unit = head_org_unit,
                    service_work_total = req.service_work_total,
                    investigative_work_total = req.investigative_work_total,
                    crime_prevention_work_total = req.crime_prevention_work_total,
                    traffic_work_total = req.traffic_work_total,
                    satisfaction_total = req.satisfaction_total,
                    evaluators_total = req.evaluators_total,
                });
            }

            _db.SaveChanges();
        }

        public void DeleteOrgUnitCascade(string code)
        {
            if (string.IsNullOrEmpty(code)) return;

            List<ORG_UNIT> orgs = _db.org_unit.Where(o => o.code == code || EF.Functions.Like(o.head_org_unit, "%" + code + "[_]%")).ToList();
            _db.org_unit.RemoveRange(orgs);
            _db.SaveChanges();
        }

        public void ImportOrgUnitMasters(IEnumerable<ImportOrgUnitReqModel> items, string createByUser)
        {
            foreach (var item in items)
            {
                ORG_UNIT? org = _db.org_unit.Where(o => o.code == item.org_unit_code && o.is_evaluation == true).FirstOrDefault();
                if (org != null)
                {
                    org.service_work_total = item.service_work_total;
                    org.investigative_work_total = item.investigative_work_total;
                    org.crime_prevention_work_total = item.crime_prevention_work_total;
                    org.traffic_work_total = item.traffic_work_total;
                    org.satisfaction_total = item.satisfaction_total;
                    org.evaluators_total = item.evaluators_total;
                }
            }
            
            _db.SaveChanges();
        }
    }
}
