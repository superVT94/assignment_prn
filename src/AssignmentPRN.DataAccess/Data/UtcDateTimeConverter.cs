using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AssignmentPRN.DataAccess.Data;

public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => ConvertToUtc(value),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }

    internal static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static DateTime ConvertToUtc(DateTime value)
    {
        return NormalizeToUtc(value);
    }
}

public sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(
            value => ConvertToUtc(value),
            value => value.HasValue
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                : null)
    {
    }

    private static DateTime? ConvertToUtc(DateTime? value)
    {
        return value.HasValue ? UtcDateTimeConverter.NormalizeToUtc(value.Value) : null;
    }
}
