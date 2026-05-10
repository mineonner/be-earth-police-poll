using police_poll_service.models.request;

namespace police_poll_service.Repositories
{
    public interface IEvaluationRepository
    {
        void ImportEvaluations(IEnumerable<ImportEvoluationReqModel> items, string createByUser);
        void DeleteByCode(string code);
    }
}
