namespace ReleasePilot.Api.Auth;

public record UserRecord(Guid UserId, string Username, string Role);

public class StubUserService
{
    private static readonly IReadOnlyList<UserRecord> _users =
    [
        new UserRecord(UserId:   Guid.Parse("11111111-1111-1111-1111-111111111111"),
                       Username: "approver",
                       Role:     "Approver"),

        new UserRecord(UserId:   Guid.Parse("22222222-2222-2222-2222-222222222222"),
                       Username: "developer",
                       Role:     "Developer"),
    ];

    public UserRecord? FindByUsername(string username)
    {
        return _users.FirstOrDefault(u => string.Equals(u.Username, 
                                                        username, 
                                                        StringComparison.OrdinalIgnoreCase));
    }
}
