using ErrorOr;

namespace ReleasePilot.Domain.Promotions.ValueObjects;

public sealed class EnvironmentName : IEquatable<EnvironmentName>
{
    private static readonly Dictionary<string, int> _promotionOrder = new()
    {
        ["dev"]        = 0,
        ["staging"]    = 1,
        ["production"] = 2,
    };

    public static readonly IReadOnlyList<string> KnownEnvironments = [.. _promotionOrder.Keys];

    private EnvironmentName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ErrorOr<EnvironmentName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Error.Validation(code: "EnvironmentName.Empty",
                                    description: "Environment name cannot be empty.");
        }

        var normalised = value.Trim().ToLowerInvariant();

        if (!_promotionOrder.ContainsKey(normalised))
        {
            return Error.Validation(code: "EnvironmentName.Unknown",
                                    description: $"'{value}' is not a recognised environment. Known environments: {string.Join(", ", KnownEnvironments)}.");
        }

        return new EnvironmentName(normalised);
    }

    public EnvironmentName? PreviousEnvironment()
    {
        var order = _promotionOrder[Value];
        if (order == 0)
        {
            return null;
        }

        var previous = _promotionOrder.Single(kv => kv.Value == order - 1).Key;
        return new EnvironmentName(previous);
    }

    public EnvironmentName? NextEnvironment()
    {
        var order = _promotionOrder[Value];
        if (order == _promotionOrder.Count - 1)
        {
            return null;
        }

        var next = _promotionOrder.Single(kv => kv.Value == order + 1).Key;
        return new EnvironmentName(next);
    }

    public bool CanPromoteTo(EnvironmentName other) => _promotionOrder[other.Value] == _promotionOrder[Value] + 1;

    public bool Equals(EnvironmentName? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is EnvironmentName other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}
