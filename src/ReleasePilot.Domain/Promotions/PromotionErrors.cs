using ErrorOr;

namespace ReleasePilot.Domain.Promotions;

public static class PromotionErrors
{
    public static Error InvalidTransition(PromotionState current, string operation) =>
        Error.Conflict(
            code: "Promotion.InvalidTransition",
            description: $"Cannot perform '{operation}' when promotion is in state '{current}'.");

    public static readonly Error AlreadyCancelled =
        Error.Conflict(
            code: "Promotion.AlreadyCancelled",
            description: "Promotion has already been cancelled.");

    public static readonly Error AlreadyCompleted =
        Error.Conflict(
            code: "Promotion.AlreadyCompleted",
            description: "Promotion has already been completed.");

    public static readonly Error AlreadyRolledBack =
        Error.Conflict(
            code: "Promotion.AlreadyRolledBack",
            description: "Promotion has already been rolled back.");

    public static Error ReasonRequired(string operation) =>
        Error.Validation(
            code: "Promotion.ReasonRequired",
            description: $"A reason is required to perform '{operation}'.");

    public static Error ApproverRequired =>
        Error.Validation(
            code: "Promotion.ApproverRequired",
            description: "An approver identifier is required.");

    public static readonly Error EnvironmentNotReady =
        Error.Conflict(
            code: "Promotion.EnvironmentNotReady",
            description: "The predecessor environment has not been completed for this application.");
}
