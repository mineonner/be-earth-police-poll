using Microsoft.EntityFrameworkCore;
using police_poll_service.DB;
using police_poll_service.DB.Tables;
using police_poll_service.models.request;

namespace police_poll_service.Repositories
{
    public class EvaluationRepository : IEvaluationRepository
    {
        private readonly PolicePollDbContext _db;

        public EvaluationRepository(PolicePollDbContext db)
        {
            _db = db;
        }

        public void ImportEvaluations(IEnumerable<ImportEvoluationReqModel> items, string createByUser)
        {
            const string codePrefix = "EV";
            EVALUATION? orgMax = _db.evaluation.AsNoTracking()
                .OrderByDescending(e => Convert.ToInt32(e.code.Substring(codePrefix.Length, e.code.Length - codePrefix.Length)))
                .FirstOrDefault();

            long nextNewCodeNum = orgMax == null
                ? 1
                : Int64.Parse(orgMax.code.Substring(codePrefix.Length, orgMax.code.Length - codePrefix.Length)) + 1;

            foreach (ImportEvoluationReqModel evReq in items)
            {
                EVALUATION? ev = _db.evaluation
                    .Where(o => o.org_unit_code == evReq.org_unit_code && o.evaluation_year == evReq.evaluation_year)
                    .FirstOrDefault();

                if (ev != null)
                {
                    ev.create_date = DateTime.Now.AddHours(7);
                    ev.create_by = createByUser;
                    ev.service_work_score = evReq.service_work_score;
                    ev.investigative_work_score = evReq.investigative_work_score;
                    ev.crime_prevention_work_score = evReq.crime_prevention_work_score;
                    ev.traffic_work_score = evReq.traffic_work_score;
                    ev.satisfaction_score = evReq.satisfaction_score;
                    ev.service_work_count = evReq.service_work_count;
                    ev.investigative_work_count = evReq.investigative_work_count;
                    ev.crime_prevention_work_count = evReq.crime_prevention_work_count;
                    ev.traffic_work_count = evReq.traffic_work_count;
                    ev.satisfaction_count = evReq.satisfaction_count;
                    ev.evaluators_amount = evReq.evaluators_amount;
                }
                else
                {
                    string code = codePrefix + nextNewCodeNum;
                    nextNewCodeNum++;

                    _db.evaluation.Add(new EVALUATION
                    {
                        code = code,
                        create_date = DateTime.Now.AddHours(7),
                        create_by = createByUser,
                        org_unit_code = evReq.org_unit_code,
                        service_work_score = evReq.service_work_score,
                        investigative_work_score = evReq.investigative_work_score,
                        crime_prevention_work_score = evReq.crime_prevention_work_score,
                        traffic_work_score = evReq.traffic_work_score,
                        satisfaction_score = evReq.satisfaction_score,
                        service_work_count = evReq.service_work_count,
                        investigative_work_count = evReq.investigative_work_count,
                        crime_prevention_work_count = evReq.crime_prevention_work_count,
                        traffic_work_count = evReq.traffic_work_count,
                        satisfaction_count = evReq.satisfaction_count,
                        evaluators_amount = evReq.evaluators_amount,
                        evaluation_year = evReq.evaluation_year
                    });
                }
            }

            _db.SaveChanges();
        }

        public void DeleteByCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return;

            EVALUATION ev = _db.evaluation.Where(o => o.code == code).First();
            _db.evaluation.Remove(ev);
            _db.SaveChanges();
        }
    }
}
