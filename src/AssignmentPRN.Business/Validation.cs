using AssignmentPRN.DataAccess.Common;
using System.Net.Mail;

namespace AssignmentPRN.Business;

internal static class BusinessValidation
{
    public static string RequiredText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessValidationException($"{fieldName} is required.");
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
        if (!IsValidEmail(normalized))
        {
            throw new BusinessValidationException("Email must be a valid email address.");
        }

        return normalized;
    }

    public static int PositiveId(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new BusinessValidationException($"{fieldName} must be positive.");
        }

        return value;
    }

    public static int NonNegative(int value, string fieldName)
    {
        if (value < 0)
        {
            throw new BusinessValidationException($"{fieldName} cannot be negative.");
        }

        return value;
    }

    public static int PositiveCount(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new BusinessValidationException($"{fieldName} must be positive.");
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

    public static DateTime ToUtcDate(DateTime value)
    {
        return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    }

    public static void EnsureEnum<TEnum>(TEnum value, string fieldName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
        {
            throw new BusinessValidationException($"{fieldName} is not supported.");
        }
    }

    public static void EnsureLength(string value, string fieldName, int maxLength)
    {
        if (value.Length > maxLength)
        {
            throw new BusinessValidationException($"{fieldName} cannot exceed {maxLength} characters.");
        }
    }

    private static bool IsValidEmail(string value)
    {
        if (value.Any(char.IsWhiteSpace) || value.Any(char.IsControl))
        {
            return false;
        }

        var parts = value.Split('@', StringSplitOptions.None);
        if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
        {
            return false;
        }

        if (parts[0].Length > 64 || parts[1].Length > 255 || parts[1].StartsWith('.') || parts[1].EndsWith('.'))
        {
            return false;
        }

        if (parts[1].Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var address = new MailAddress(value);
            return address.DisplayName.Length == 0
                && string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

internal static class ServiceExecutor
{
    public static async Task<ServiceResponse> RunAsync(
        Func<Task> operation,
        string failureMessage)
    {
        try
        {
            await operation();
            return ServiceResponse.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (BusinessValidationException exception)
        {
            return ServiceResponse.Fail(exception.Errors);
        }
        catch (DuplicateEntityException)
        {
            return ServiceResponse.Fail("A record with the same unique value already exists.");
        }
        catch (KeyNotFoundException)
        {
            return ServiceResponse.Fail("The requested record was not found.");
        }
        catch (ArgumentException)
        {
            return ServiceResponse.Fail("The supplied values are invalid.");
        }
        catch (InvalidOperationException)
        {
            return ServiceResponse.Fail(failureMessage);
        }
        catch (Exception)
        {
            return ServiceResponse.Fail(failureMessage);
        }
    }

    public static async Task<ServiceResponse<T>> RunAsync<T>(
        Func<Task<T>> operation,
        string failureMessage)
    {
        try
        {
            var data = await operation();
            return ServiceResponse<T>.Ok(data);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (BusinessValidationException exception)
        {
            return ServiceResponse<T>.Fail(exception.Errors);
        }
        catch (DuplicateEntityException)
        {
            return ServiceResponse<T>.Fail("A record with the same unique value already exists.");
        }
        catch (KeyNotFoundException)
        {
            return ServiceResponse<T>.Fail("The requested record was not found.");
        }
        catch (ArgumentException)
        {
            return ServiceResponse<T>.Fail("The supplied values are invalid.");
        }
        catch (InvalidOperationException)
        {
            return ServiceResponse<T>.Fail(failureMessage);
        }
        catch (Exception)
        {
            return ServiceResponse<T>.Fail(failureMessage);
        }
    }
}
