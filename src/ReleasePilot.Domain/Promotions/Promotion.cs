using ErrorOr;
using ReleasePilot.Domain.Common;
using ReleasePilot.Domain.Promotions.Events;
using ReleasePilot.Domain.Promotions.ValueObjects;
using ApplicationId = ReleasePilot.Domain.Promotions.ValueObjects.ApplicationId;

namespace ReleasePilot.Domain.Promotions;

public sealed class Promotion : AggregateRoot<Guid>
{
#pragma warning disable CS8618
    private Promotion() { }
#pragma warning restore CS8618

    private Promotion(Guid id, ApplicationId applicationId, ApplicationVersion version, EnvironmentName targetEnvironment, EnvironmentName? sourceEnvironment, string requestedBy, DateTime requestedAt, IReadOnlyList<string> issueReferences) : base(id)
    {
        ApplicationId = applicationId;
        Version = version;
        TargetEnvironment = targetEnvironment;
        SourceEnvironment = sourceEnvironment;
        RequestedBy = requestedBy;
        RequestedAt = requestedAt;
        State = PromotionState.Requested;
        IssueReferences = issueReferences;
    }

    public ApplicationId ApplicationId { get; }
    public ApplicationVersion Version { get; }
    public EnvironmentName TargetEnvironment { get; }
    public EnvironmentName? SourceEnvironment { get; }
    public PromotionState State { get; private set; }
    public string RequestedBy { get; }
    public DateTime RequestedAt { get; }
    public IReadOnlyList<string> IssueReferences { get; } = [];
    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? RollbackReason { get; private set; }
    public string? CancellationReason { get; private set; }

    public static ErrorOr<Promotion> Request(ApplicationId applicationId, ApplicationVersion version, EnvironmentName targetEnvironment, EnvironmentName? sourceEnvironment, string requestedBy, IEnumerable<string>? issueReferences = null)
    {
        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            return Error.Validation(code: "Promotion.RequestedByRequired",
                                    description: "The requestor identifier cannot be empty.");
        }

        var promotion = new Promotion(id: Guid.NewGuid(),
                                      applicationId: applicationId,
                                      version: version,
                                      targetEnvironment: targetEnvironment,
                                      sourceEnvironment: sourceEnvironment,
                                      requestedBy: requestedBy.Trim(),
                                      requestedAt: DateTime.UtcNow,
                                      issueReferences: issueReferences?.ToList() ?? []);

        promotion.RaiseDomainEvent(new PromotionRequested
        {
            EventId = Guid.NewGuid(),
            OccurredOn = promotion.RequestedAt,
            PromotionId = promotion.Id,
            RequestedBy = promotion.RequestedBy,
            ApplicationId = applicationId,
            Version = version,
            TargetEnvironment = targetEnvironment
        });

        return promotion;
    }

    public const string ApproverRole = "Approver";

    public ErrorOr<Success> Approve(string approvedBy, string approverRole)
    {
        if (string.IsNullOrWhiteSpace(approvedBy))
        {
            return PromotionErrors.ApproverRequired;
        }

        if (approverRole != ApproverRole)
        {
            return Error.Unauthorized(code: "Promotion.Unauthorized",
                                      description: $"Only users with the '{ApproverRole}' role can approve promotions.");
        }

        if (State != PromotionState.Requested)
        {
            return PromotionErrors.InvalidTransition(State, nameof(Approve));
        }

        State = PromotionState.Approved;
        ApprovedBy = approvedBy.Trim();
        ApprovedAt = DateTime.UtcNow;

        RaiseDomainEvent(new PromotionApproved
        {
            EventId = Guid.NewGuid(),
            OccurredOn = ApprovedAt.Value,
            PromotionId = Id,
            ApprovedBy = ApprovedBy
        });

        return Result.Success;
    }

    public ErrorOr<Success> StartDeployment(string startedBy)
    {
        if (State != PromotionState.Approved)
        {
            return PromotionErrors.InvalidTransition(State, nameof(StartDeployment));
        }

        State = PromotionState.InProgress;
        StartedAt = DateTime.UtcNow;

        RaiseDomainEvent(new DeploymentStarted
        {
            EventId = Guid.NewGuid(),
            OccurredOn = StartedAt.Value,
            PromotionId = Id,
            StartedBy = startedBy
        });

        return Result.Success;
    }

    public ErrorOr<Success> Complete(string completedBy)
    {
        if (State != PromotionState.InProgress)
        {
            return PromotionErrors.InvalidTransition(State, nameof(Complete));
        }

        State = PromotionState.Completed;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new PromotionCompleted
        {
            EventId = Guid.NewGuid(),
            OccurredOn = CompletedAt.Value,
            PromotionId = Id,
            CompletedBy = completedBy,
            CompletedAt = CompletedAt.Value
        });

        return Result.Success;
    }

    public ErrorOr<Success> Rollback(string reason, string rolledBackBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return PromotionErrors.ReasonRequired(nameof(Rollback));
        }

        if (State != PromotionState.InProgress)
        {
            return PromotionErrors.InvalidTransition(State, nameof(Rollback));
        }

        State = PromotionState.RolledBack;
        RollbackReason = reason.Trim();

        RaiseDomainEvent(new PromotionRolledBack
        {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            PromotionId = Id,
            RolledBackBy = rolledBackBy,
            Reason = RollbackReason
        });

        return Result.Success;
    }

    public ErrorOr<Success> Cancel(string reason, string cancelledBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return PromotionErrors.ReasonRequired(nameof(Cancel));
        }

        if (State is not PromotionState.Requested)
        {
            return PromotionErrors.InvalidTransition(State, nameof(Cancel));
        }

        State = PromotionState.Cancelled;
        CancellationReason = reason.Trim();

        RaiseDomainEvent(new PromotionCancelled
        {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            PromotionId = Id,
            CancelledBy = cancelledBy,
            Reason = CancellationReason
        });

        return Result.Success;
    }
}
