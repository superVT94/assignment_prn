namespace AssignmentPRN.DataAccess.Common;

internal static class RepositoryInput
{
    public static string RequiredText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        }

        var normalized = value.Trim();
        EnsureLength(normalized, fieldName, maxLength);
        return normalized;
    }

    public static string? OptionalText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        EnsureLength(normalized, fieldName, maxLength);
        return normalized;
    }

    public static string NormalizeCode(string? value, string fieldName, int maxLength)
    {
        return RequiredText(value, fieldName, maxLength).ToUpperInvariant();
    }

    public static string NormalizeEmail(string? value)
    {
        var normalized = RequiredText(value, "Email", 320).ToLowerInvariant();

        if (!normalized.Contains('@', StringComparison.Ordinal) || normalized.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("Email must be a valid email address.", "Email");
        }

        return normalized;
    }

    public static int PositiveId(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(fieldName, "The identifier must be positive.");
        }

        return value;
    }

    public static int NonNegative(int value, string fieldName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(fieldName, "The value cannot be negative.");
        }

        return value;
    }

    public static int PositiveCount(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(fieldName, "The value must be positive.");
        }

        return value;
    }

    public static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public static DateTime? ToUtc(DateTime? value)
    {
        return value.HasValue ? ToUtc(value.Value) : null;
    }

    public static void EnsureEnum<TEnum>(TEnum value, string fieldName) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
        {
            throw new ArgumentOutOfRangeException(fieldName, "The value is not supported.");
        }
    }

    public static void EnsureLength(string value, string fieldName, int maxLength)
    {
        if (value.Length > maxLength)
        {
            throw new ArgumentException($"{fieldName} cannot exceed {maxLength} characters.", fieldName);
        }
    }
}
