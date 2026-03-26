namespace ReleasePilot.Domain.Promotions;

public enum PromotionState
{
    Requested,
    Approved,
    InProgress,
    Completed,
    RolledBack,
    Cancelled
}
