using ReleasePilot.Application.Common;

namespace ReleasePilot.Application.Ports;

public interface IIssueTrackerPort
{
    Task<IReadOnlyList<WorkItemResponse>> GetWorkItemsAsync(
        IEnumerable<string> issueReferences,
        CancellationToken cancellationToken = default);
}
