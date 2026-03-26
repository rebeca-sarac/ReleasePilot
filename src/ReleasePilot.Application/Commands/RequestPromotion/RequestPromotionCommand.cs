using ErrorOr;
using MediatR;

namespace ReleasePilot.Application.Commands.RequestPromotion;

public record RequestPromotionCommand : IRequest<ErrorOr<Guid>>
{
    public Guid ApplicationId { get; init; }
    public string Version { get; init; } = string.Empty;
    public string TargetEnvironment { get; init; } = string.Empty;
    public string? SourceEnvironment { get; init; }
    public string RequestedBy { get; init; } = string.Empty;
    public IReadOnlyList<string>? IssueReferences { get; init; }
}
