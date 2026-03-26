using ReleasePilot.Application.Common;
using ReleasePilot.Application.Ports;

namespace ReleasePilot.Infrastructure.Stubs;

public class StubIssueTrackerPort : IIssueTrackerPort
{
    private static readonly IReadOnlyList<WorkItemResponse> _hardcoded =
    [
        new WorkItemResponse
        {
            Id          = "PROJ-1042",
            Title       = "Add environment promotion workflow to release pipeline",
            Description = "Implement the full promotion lifecycle (Requested → Approved → InProgress → Completed) with approval gates and rollback support.",
            Status      = "In Review"
        },

        new WorkItemResponse
        {
            Id          = "PROJ-1078",
            Title       = "Integrate RabbitMQ for domain event publishing",
            Description = "Replace the in-process event dispatch with an outbox-style RabbitMQ publisher so downstream consumers can react to promotion state changes asynchronously.",
            Status      = "Done"
        },

        new WorkItemResponse
        {
            Id          = "PROJ-1095",
            Title       = "Audit log for all promotion state transitions",
            Description = "Persist an immutable audit record every time a promotion moves to a new state capturing the actor, timestamp, and event type for compliance reporting.",
            Status      = "In Progress"
        },
    ];

    public Task<IReadOnlyList<WorkItemResponse>> GetWorkItemsAsync(IEnumerable<string> issueReferences, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_hardcoded);
    }
}
