using System.Text.RegularExpressions;
using ErrorOr;

namespace ReleasePilot.Domain.Promotions.ValueObjects;

public sealed partial class ApplicationVersion : IEquatable<ApplicationVersion>
{
    // Accepts SemVer: 1.2.3 or 1.2.3-preview.4
    [GeneratedRegex(@"^\d+\.\d+\.\d+(-[\w\.\-]+)?$", RegexOptions.Compiled)]
    private static partial Regex SemVerPattern();

    private ApplicationVersion(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ErrorOr<ApplicationVersion> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Error.Validation(code: "ApplicationVersion.Empty",
                                    description: "Application version cannot be empty.");
        }

        var trimmed = value.Trim();

        if (!SemVerPattern().IsMatch(trimmed))
        {
            return Error.Validation(code: "ApplicationVersion.InvalidFormat",
                                    description: $"'{value}' is not a valid semantic version (e.g. 1.2.3 or 1.2.3-preview.1).");
        }

        return new ApplicationVersion(trimmed);
    }

    public bool Equals(ApplicationVersion? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is ApplicationVersion other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}
