using ClientProManager.Models;

namespace ClientProManager.Services;

public interface ISessionService
{
    User? CurrentUser { get; set; }
    bool IsAuthenticated { get; }
}

public class SessionService : ISessionService
{
    public User? CurrentUser { get; set; }
    public bool IsAuthenticated => CurrentUser is not null;
}
