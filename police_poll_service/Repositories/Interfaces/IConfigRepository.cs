namespace police_poll_service.Repositories
{
    public interface IConfigRepository
    {
        string? GetDescriptionByCode(string code);
    }
}
