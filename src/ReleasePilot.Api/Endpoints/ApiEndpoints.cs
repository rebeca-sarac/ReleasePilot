namespace ReleasePilot.Api.Endpoints;

public static class ApiEndpoints
{
    public static class Promotions
    {
        public const string Base = "/api/promotions";
        public const string GetById = $"{Base}/{{id}}";
        public const string Approve = $"{Base}/{{id}}/approve";
        public const string Start = $"{Base}/{{id}}/start";
        public const string Complete = $"{Base}/{{id}}/complete";
        public const string Rollback = $"{Base}/{{id}}/rollback";
        public const string Cancel = $"{Base}/{{id}}/cancel";
        public const string EnvironmentStatus = $"{Base}/{{id}}/environment-status";
    }

    public static class Auth
    {
        public const string Token = "/auth/token";
    }
}
