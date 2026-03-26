using ReleasePilot.Domain.Promotions;
using ReleasePilot.Domain.Promotions.ValueObjects;
using ApplicationId = ReleasePilot.Domain.Promotions.ValueObjects.ApplicationId;

namespace ReleasePilot.Application.Tests;
public static class PromotionFactory
{
    public static readonly ApplicationId DefaultAppId = ApplicationId.Create(Guid.Parse("11111111-1111-1111-1111-111111111111")).Value;
    public static readonly ApplicationVersion DefaultVersion = ApplicationVersion.Create("1.0.0").Value;
    public static readonly EnvironmentName DefaultEnvironment = EnvironmentName.Create("staging").Value;
    public static readonly EnvironmentName DefaultSourceEnvironment = EnvironmentName.Create("dev").Value;

    public static Promotion Requested() => Promotion.Request(DefaultAppId, DefaultVersion, DefaultEnvironment, DefaultSourceEnvironment, "alice").Value;

    public static Promotion Approved()
    {
        var p = Requested();
        p.Approve("bob", Promotion.ApproverRole);
        p.ClearDomainEvents();
        return p;
    }

    public static Promotion InProgress()
    {
        var p = Approved();
        p.StartDeployment("system");
        p.ClearDomainEvents();
        return p;
    }
}
