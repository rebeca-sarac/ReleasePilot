using ErrorOr;

namespace ReleasePilot.Domain.Promotions.ValueObjects;

public sealed class ApplicationId : IEquatable<ApplicationId>
{
    private ApplicationId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static ErrorOr<ApplicationId> Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            return Error.Validation(code: "ApplicationId.Empty",
                                    description: "Application ID cannot be an empty GUID.");
        }

        return new ApplicationId(value);
    }

    public static ApplicationId NewId() => new(Guid.NewGuid());

    public bool Equals(ApplicationId? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is ApplicationId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
