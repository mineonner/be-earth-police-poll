using police_poll_service.models.request;
using police_poll_service.models.respone;

namespace police_poll_service.Repositories
{
    public interface IUserRepository
    {
        UserResModel? GetForLogin(string username);
        List<SearchUserResModel> SearchUsers(IReadOnlyList<string> orgUnits);
        void UpdateOrCreateUser(UpdateUserReqModel req, string? passwordHashForNewOrReset);
        void DeleteUser(string user);
    }
}
